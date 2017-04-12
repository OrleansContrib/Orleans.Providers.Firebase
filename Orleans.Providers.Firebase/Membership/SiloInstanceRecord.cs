namespace Orleans.Providers.Firebase.Membership
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using Orleans.Runtime;

    public class SiloInstanceRecord
    {
        public const string DeploymentIdPropertyName = "DeploymentId";
        public const string SiloIdentifyPropertyName = "SiloIdentity";
        public const string ETagPropertyName = "ETag";
        public const string AddressPropertyName = "Address";
        public const string PortPropertName = "Port";
        public const string GenerationPropertName = "Generation";
        public const string HostnamePropertName = "HostName";
        public const string StatusPropertName = "SiloStatus";
        public const string ProxyPortPropertName = "ProxyPort";
        public const string SiloNamePropertName = "SiloName";
        public const string InstanceNamePropertName = "InstanceName";
        public const string SuspectingSiloPropertName = "SuspectingSilos";
        public const string SuspectingTimesPropertName = "SuspectingTimes";
        public const string StartTimePropertName = "StartTime";
        public const string IAmAliveTimePropertName = "IAmAliveTime";
        private const char Seperator = '-';

        public SiloInstanceRecord()
        {
        }

        public string DeploymentId { get; set; }

        public string SiloIdentity { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public int Generation { get; set; }

        public string HostName { get; set; }

        public int Status { get; set; }

        public int ProxyPort { get; set; }

        public string SiloName { get; set; }

        public string SuspectingSilos { get; set; }

        public string SuspectingTimes { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime IAmAliveTime { get; set; }

        public int ETag { get; set; }

        public static SiloAddress UnpackRowKey(string rowKey)
        {
            try
            {
                int idx1 = rowKey.IndexOf(Seperator);
                int idx2 = rowKey.LastIndexOf(Seperator);
                var addressStr = rowKey.Substring(0, idx1);
                var portStr = rowKey.Substring(idx1 + 1, idx2 - idx1 - 1);
                var genStr = rowKey.Substring(idx2 + 1);
                IPAddress address = IPAddress.Parse(addressStr);
                int port = int.Parse(portStr);
                int generation = int.Parse(genStr);
                return SiloAddress.New(new IPEndPoint(address, port), generation);
            }
            catch (Exception exc)
            {
                throw new AggregateException("Error from UnpackRowKey", exc);
            }
        }

        public static string ConstructSiloIdentity(SiloAddress silo)
        {
            return string.Format("{0}-{1}-{2}", silo.Endpoint.Address, silo.Endpoint.Port, silo.Generation);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("OrleansSilo [");
            sb.Append(" Deployment=").Append(this.DeploymentId);
            sb.Append(" LocalEndpoint=").Append(this.Address);
            sb.Append(" LocalPort=").Append(this.Port);
            sb.Append(" Generation=").Append(this.Generation);

            sb.Append(" Host=").Append(this.HostName);
            sb.Append(" Status=").Append(this.Status);
            sb.Append(" ProxyPort=").Append(this.ProxyPort);

            sb.Append(" SiloName=").Append(this.SiloName);

            if (!string.IsNullOrEmpty(this.SuspectingSilos))
            {
                sb.Append(" SuspectingSilos=").Append(this.SuspectingSilos);
            }

            if (!string.IsNullOrEmpty(this.SuspectingTimes))
            {
                sb.Append(" SuspectingTimes=").Append(this.SuspectingTimes);
            }

            sb.Append(" StartTime=").Append(this.StartTime);
            sb.Append(" IAmAliveTime=").Append(this.IAmAliveTime);
            sb.Append("]");
            return sb.ToString();
        }

        public Dictionary<string, AttributeValue> GetKeys()
        {
            var keys = new Dictionary<string, AttributeValue>();
            keys.Add(DeploymentIdPropertyName, new AttributeValue(this.DeploymentId));
            keys.Add(SiloIdentifyPropertyName, new AttributeValue(this.SiloIdentity));
            return keys;
        }
    }
}
