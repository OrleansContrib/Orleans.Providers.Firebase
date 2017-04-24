namespace Orleans.Providers.Firebase.Membership
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Orleans.Providers.Firebase.Authentication;
    using Orleans.Runtime;
    using Orleans.Runtime.Configuration;

    public class FirebaseMembershipTable : IMembershipTable
    {
        private const string OrleansMembershipPath = "Orleans/Membership";

        private readonly TableVersion tableVersion = new TableVersion(0, "0");
        private string deploymentId;
        private FirebaseClient firebaseClient;
        private Logger logger;

        public async Task DeleteMembershipTableEntries(string deploymentId)
        {
            await this.firebaseClient.DeleteAsync(this.ConstructDeploymentPath(deploymentId));
        }

        public async Task InitializeMembershipTable(GlobalConfiguration globalConfiguration, bool tryInitTableVersion, Logger logger)
        {
            this.logger = logger;
            this.deploymentId = string.IsNullOrEmpty(globalConfiguration.DeploymentId) ? "Default" : globalConfiguration.DeploymentId;
            this.firebaseClient = new FirebaseClient();
            this.logger.Info("Initializing Firebase Membership Table");
            var connectionString = globalConfiguration.DataConnectionString.Split("|".ToCharArray());
            this.firebaseClient.BasePath = connectionString[0];
            if (connectionString.Length > 1)
            {
                this.firebaseClient.Key = FirebaseServiceKey.FromBase64(connectionString[1]);
            }

            await this.firebaseClient.Initialize();
        }

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            try
            {
                var tableEntry = this.ConvertEntry(entry);
                var key = this.CreateSiloKey(tableEntry.DeploymentId, tableEntry.SiloIdentity);
                await this.firebaseClient.PutAsync(this.ConstructMembershipPath(key), tableEntry);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<MembershipTableData> ReadAll()
        {
            var allEntries = await this.firebaseClient.GetAsync<Dictionary<string, SiloInstanceRecord>>(this.ConstructMembershipPath());
            var entries = new List<SiloInstanceRecord>();

            if (allEntries != null)
            {
                foreach (var entry in allEntries)
                {
                    entries.Add(entry.Value);
                }
            }

            MembershipTableData data = this.ConvertEntries(entries);
            if (this.logger.IsVerbose2)
            {
                this.logger.Verbose2("ReadAll Table=" + Environment.NewLine + "{0}", data.ToString());
            }

            return data;
        }

        public Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            // TODO: Implement read.
            return Task.FromResult(new MembershipTableData(this.tableVersion));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            var key = this.CreateSiloKey(this.deploymentId, entry.SiloAddress.ToString());
            await this.firebaseClient.PutAsync(this.ConstructMembershipPath($"{key}/{SiloInstanceRecord.IAmAliveTimePropertName}"), entry.IAmAliveTime);
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            try
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("UpdateRow entry = {0}, etag = {1}", entry.SiloAddress, etag);
                }

                var siloEntry = this.ConvertEntry(entry);
                if (!int.TryParse(etag, out int currentEtag))
                {
                    this.logger.Warn(0, $"Update failed. Invalid ETag value. Will retry. Entry {entry.SiloAddress}, eTag {etag}");
                    return false;
                }

                siloEntry.ETag = currentEtag + 1;

                bool result;

                try
                {
                    var key = this.CreateSiloKey(this.deploymentId, entry.SiloAddress.ToString());
                    var path = this.ConstructMembershipPath(key);
                    var state = await this.firebaseClient.GetAsync<SiloInstanceRecord>(path);

                    // TODO: Handle partial deltas.
                    state.ETag = siloEntry.ETag;
                    await this.firebaseClient.PutAsync(path, state);

                    result = true;
                }
                catch (Exception e)
                {
                    result = false;
                    this.logger.Warn(0, $"Update failed due to contention on the table. Will retry. Entry {entry.SiloAddress}, eTag {etag}", e);
                }

                return result;
            }
            catch (Exception e)
            {
                this.logger.Warn(0, $"Intermediate error updating entry {entry.SiloAddress} to the membership table.", e);
                throw;
            }
        }

        private string CreateSiloKey(string deploymentId, string siloIdentity)
        {
            siloIdentity = siloIdentity.Replace("-", ":").TrimStart('S');
            return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{deploymentId}_{siloIdentity}"));
        }

        private string ConstructDeploymentPath(string deploymentId)
        {
            return $"{OrleansMembershipPath}/{deploymentId}";
        }

        private string ConstructMembershipPath(string subPath = null)
        {
            return $"{OrleansMembershipPath}/{this.deploymentId}{(subPath == null ? string.Empty : "/" + subPath)}";
        }

        private MembershipTableData ConvertEntries(List<SiloInstanceRecord> entries)
        {
            try
            {
                var memEntries = new List<Tuple<MembershipEntry, string>>();

                if (entries != null)
                {
                    foreach (var tableEntry in entries)
                    {
                        try
                        {
                            MembershipEntry membershipEntry = this.Parse(tableEntry);
                            memEntries.Add(new Tuple<MembershipEntry, string>(membershipEntry, tableEntry.ETag.ToString()));
                        }
                        catch (Exception e)
                        {
                            this.logger.Error(
                                0,
                                $"Intermediate error parsing SiloInstanceTableEntry to MembershipTableData: {tableEntry}. Ignoring this entry.",
                                e);
                        }
                    }
                }

                var data = new MembershipTableData(memEntries, this.tableVersion);
                return data;
            }
            catch (Exception e)
            {
                this.logger.Error(
                    0,
                    $"Intermediate error parsing SiloInstanceTableEntry to MembershipTableData: {Utils.EnumerableToString(entries, x => x.ToString())}.",
                    e);
                throw;
            }
        }

        private SiloInstanceRecord ConvertEntry(MembershipEntry memEntry)
        {
            var tableEntry = new SiloInstanceRecord
            {
                DeploymentId = this.deploymentId,
                Address = memEntry.SiloAddress.Endpoint.Address.ToString(),
                Port = memEntry.SiloAddress.Endpoint.Port,
                Generation = memEntry.SiloAddress.Generation,
                HostName = memEntry.HostName,
                Status = (int)memEntry.Status,
                ProxyPort = memEntry.ProxyPort,
                SiloName = memEntry.SiloName,
                StartTime = memEntry.StartTime,
                IAmAliveTime = memEntry.IAmAliveTime,
                SiloIdentity = SiloInstanceRecord.ConstructSiloIdentity(memEntry.SiloAddress)
            };

            if (memEntry.SuspectTimes != null)
            {
                var siloList = new StringBuilder();
                var timeList = new StringBuilder();
                bool first = true;
                foreach (var tuple in memEntry.SuspectTimes)
                {
                    if (!first)
                    {
                        siloList.Append('|');
                        timeList.Append('|');
                    }

                    siloList.Append(tuple.Item1.ToParsableString());
                    timeList.Append(LogFormatter.PrintDate(tuple.Item2));
                    first = false;
                }

                tableEntry.SuspectingSilos = siloList.ToString();
                tableEntry.SuspectingTimes = timeList.ToString();
            }
            else
            {
                tableEntry.SuspectingSilos = string.Empty;
                tableEntry.SuspectingTimes = string.Empty;
            }

            return tableEntry;
        }

        private MembershipEntry Parse(SiloInstanceRecord tableEntry)
        {
            var parse = new MembershipEntry
            {
                HostName = tableEntry.HostName,
                Status = (SiloStatus)tableEntry.Status
            };

            parse.ProxyPort = tableEntry.ProxyPort;

            parse.SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Parse(tableEntry.Address), tableEntry.Port), tableEntry.Generation);

            if (!string.IsNullOrEmpty(tableEntry.SiloName))
            {
                parse.SiloName = tableEntry.SiloName;
            }

            parse.StartTime = tableEntry.StartTime;

            parse.IAmAliveTime = tableEntry.IAmAliveTime;

            var suspectingSilos = new List<SiloAddress>();
            var suspectingTimes = new List<DateTime>();

            if (!string.IsNullOrEmpty(tableEntry.SuspectingSilos))
            {
                string[] silos = tableEntry.SuspectingSilos.Split('|');
                foreach (string silo in silos)
                {
                    suspectingSilos.Add(SiloAddress.FromParsableString(silo));
                }
            }

            if (!string.IsNullOrEmpty(tableEntry.SuspectingTimes))
            {
                string[] times = tableEntry.SuspectingTimes.Split('|');
                foreach (string time in times)
                {
                    suspectingTimes.Add(LogFormatter.ParseDate(time));
                }
            }

            if (suspectingSilos.Count != suspectingTimes.Count)
            {
                throw new OrleansException(string.Format("SuspectingSilos.Length of {0} as read from Firebase table is not equal to SuspectingTimes.Length of {1}", suspectingSilos.Count, suspectingTimes.Count));
            }

            for (int i = 0; i < suspectingSilos.Count; i++)
            {
                parse.AddSuspector(suspectingSilos[i], suspectingTimes[i]);
            }

            return parse;
        }

        private SiloInstanceRecord ConvertPartial(MembershipEntry memEntry)
        {
            return new SiloInstanceRecord
            {
                DeploymentId = this.deploymentId,
                IAmAliveTime = memEntry.IAmAliveTime,
                SiloIdentity = SiloInstanceRecord.ConstructSiloIdentity(memEntry.SiloAddress)
            };
        }
    }
}
