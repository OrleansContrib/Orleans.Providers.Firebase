using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using System.Text;
using System.Collections.Generic;
using System.Net;

namespace Orleans.Providers.Firebase.Membership
{
    public class FirebaseMembershipTable : IMembershipTable
    {
        private const string OrleansMembershipPath = "Orleans/Membership";
        private readonly TableVersion _tableVersion = new TableVersion(0, "0");

        private string _deploymentId;
        private FirebaseClient _firebaseClient;
        private Logger _logger;

        public async Task DeleteMembershipTableEntries(string deploymentId)
        {
            await _firebaseClient.DeleteAsync(ConstructDeploymentPath(deploymentId));
        }

        public Task InitializeMembershipTable(GlobalConfiguration globalConfiguration, bool tryInitTableVersion, Logger logger)
        {
            _logger = logger;
            _deploymentId = string.IsNullOrEmpty(globalConfiguration.DeploymentId) ? "Default" : globalConfiguration.DeploymentId;
            _firebaseClient = new FirebaseClient();
            _logger.Info("Initializing Firebase Membership Table");
            var connectionString = globalConfiguration.DataConnectionString.Split("|".ToCharArray());
            _firebaseClient.BasePath = connectionString[0];
            if (connectionString.Length > 1)
                _firebaseClient.Auth = connectionString[1];
            return TaskDone.Done;
        }

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            try
            {
                var tableEntry = ConvertEntry(entry);
                var key = CreateSiloKey(tableEntry.DeploymentId, tableEntry.SiloIdentity);
                await _firebaseClient.PutAsync(ConstructMembershipPath(key), tableEntry);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<MembershipTableData> ReadAll()
        {
            var allEntries = await _firebaseClient.GetAsync<Dictionary<string, SiloInstanceRecord>>(ConstructMembershipPath());
            var entries = new List<SiloInstanceRecord>();

            if (allEntries != null)
            { 
                foreach (var entry in allEntries)
                {
                    entries.Add(entry.Value);
                }
            }

            MembershipTableData data = ConvertEntries(entries);
            if (_logger.IsVerbose2) _logger.Verbose2("ReadAll Table=" + Environment.NewLine + "{0}", data.ToString());

            return data;
        }

        public Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            // TODO: Implement read.
            return Task.FromResult(new MembershipTableData(_tableVersion));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            var key = CreateSiloKey(_deploymentId, entry.SiloAddress.ToString());
            await _firebaseClient.PutAsync(ConstructMembershipPath($"{key}/{SiloInstanceRecord.I_AM_ALIVE_TIME_PROPERTY_NAME}"), entry.IAmAliveTime);
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            try
            {
                if (_logger.IsVerbose) _logger.Verbose("UpdateRow entry = {0}, etag = {1}", entry.SiloAddress, etag);
                var siloEntry = ConvertEntry(entry);
                int currentEtag = 0;
                if (!int.TryParse(etag, out currentEtag))
                {
                    _logger.Warn(0, $"Update failed. Invalid ETag value. Will retry. Entry {entry.SiloAddress}, eTag {etag}");
                    return false;
                }

                siloEntry.ETag = currentEtag + 1;

                bool result;

                try
                {
                    var key = CreateSiloKey(_deploymentId, entry.SiloAddress.ToString());
                    var path = ConstructMembershipPath(key);
                    var state = await _firebaseClient.GetAsync<SiloInstanceRecord>(path);
                    // TODO: Handle partial deltas.
                    state.ETag = siloEntry.ETag;
                    await _firebaseClient.PutAsync(path, state);

                    result = true;
                }
                catch (Exception e)
                {
                    result = false;
                    _logger.Warn(0, $"Update failed due to contention on the table. Will retry. Entry {entry.SiloAddress}, eTag {etag}", e);
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.Warn(0, $"Intermediate error updating entry {entry.SiloAddress} to the membership table.", e);
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
            return $"{OrleansMembershipPath}/{_deploymentId}{(subPath == null ? "" : "/" + subPath)}";
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
                            MembershipEntry membershipEntry = Parse(tableEntry);
                            memEntries.Add(new Tuple<MembershipEntry, string>(membershipEntry, tableEntry.ETag.ToString()));
                        }
                        catch (Exception exc)
                        {
                            _logger.Error(0,
                                $"Intermediate error parsing SiloInstanceTableEntry to MembershipTableData: {tableEntry}. Ignoring this entry.", exc);
                        }
                    }
                }
                var data = new MembershipTableData(memEntries, _tableVersion);
                return data;
            }
            catch (Exception exc)
            {
                _logger.Error(0,
                    $"Intermediate error parsing SiloInstanceTableEntry to MembershipTableData: {Utils.EnumerableToString(entries, e => e.ToString())}.", exc);
                throw;
            }
        }

        private SiloInstanceRecord ConvertEntry(MembershipEntry memEntry)
        {
            var tableEntry = new SiloInstanceRecord
            {
                DeploymentId = _deploymentId,
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
                    suspectingTimes.Add(LogFormatter.ParseDate(time));
            }

            if (suspectingSilos.Count != suspectingTimes.Count)
                throw new OrleansException(String.Format("SuspectingSilos.Length of {0} as read from Firebase table is not equal to SuspectingTimes.Length of {1}", suspectingSilos.Count, suspectingTimes.Count));

            for (int i = 0; i < suspectingSilos.Count; i++)
                parse.AddSuspector(suspectingSilos[i], suspectingTimes[i]);

            return parse;
        }

        private SiloInstanceRecord ConvertPartial(MembershipEntry memEntry)
        {
            return new SiloInstanceRecord
            {
                DeploymentId = _deploymentId,
                IAmAliveTime = memEntry.IAmAliveTime,
                SiloIdentity = SiloInstanceRecord.ConstructSiloIdentity(memEntry.SiloAddress)
            };
        }
    }
}
