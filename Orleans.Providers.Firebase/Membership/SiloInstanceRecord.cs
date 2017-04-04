using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Orleans.Providers.Firebase.Membership
{
    internal class SiloInstanceRecord
    {
        public const string DEPLOYMENT_ID_PROPERTY_NAME = "DeploymentId";
        public const string SILO_IDENTITY_PROPERTY_NAME = "SiloIdentity";
        public const string ETAG_PROPERTY_NAME = "ETag";
        public const string ADDRESS_PROPERTY_NAME = "Address";
        public const string PORT_PROPERTY_NAME = "Port";
        public const string GENERATION_PROPERTY_NAME = "Generation";
        public const string HOSTNAME_PROPERTY_NAME = "HostName";
        public const string STATUS_PROPERTY_NAME = "SiloStatus";
        public const string PROXY_PORT_PROPERTY_NAME = "ProxyPort";
        public const string SILO_NAME_PROPERTY_NAME = "SiloName";
        public const string INSTANCE_NAME_PROPERTY_NAME = "InstanceName";
        public const string SUSPECTING_SILOS_PROPERTY_NAME = "SuspectingSilos";
        public const string SUSPECTING_TIMES_PROPERTY_NAME = "SuspectingTimes";
        public const string START_TIME_PROPERTY_NAME = "StartTime";
        public const string I_AM_ALIVE_TIME_PROPERTY_NAME = "IAmAliveTime";
        internal const char Seperator = '-';

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

        public SiloInstanceRecord() { }

        internal static SiloAddress UnpackRowKey(string rowKey)
        {
            try
            {
                int idx1 = rowKey.IndexOf(Seperator);
                int idx2 = rowKey.LastIndexOf(Seperator);
                var addressStr = rowKey.Substring(0, idx1);
                var portStr = rowKey.Substring(idx1 + 1, idx2 - idx1 - 1);
                var genStr = rowKey.Substring(idx2 + 1);
                IPAddress address = IPAddress.Parse(addressStr);
                int port = Int32.Parse(portStr);
                int generation = Int32.Parse(genStr);
                return SiloAddress.New(new IPEndPoint(address, port), generation);
            }
            catch (Exception exc)
            {
                throw new AggregateException("Error from UnpackRowKey", exc);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("OrleansSilo [");
            sb.Append(" Deployment=").Append(DeploymentId);
            sb.Append(" LocalEndpoint=").Append(Address);
            sb.Append(" LocalPort=").Append(Port);
            sb.Append(" Generation=").Append(Generation);

            sb.Append(" Host=").Append(HostName);
            sb.Append(" Status=").Append(Status);
            sb.Append(" ProxyPort=").Append(ProxyPort);

            sb.Append(" SiloName=").Append(SiloName);

            if (!string.IsNullOrEmpty(SuspectingSilos)) sb.Append(" SuspectingSilos=").Append(SuspectingSilos);
            if (!string.IsNullOrEmpty(SuspectingTimes)) sb.Append(" SuspectingTimes=").Append(SuspectingTimes);
            sb.Append(" StartTime=").Append(StartTime);
            sb.Append(" IAmAliveTime=").Append(IAmAliveTime);
            sb.Append("]");
            return sb.ToString();
        }

        public static string ConstructSiloIdentity(SiloAddress silo)
        {
            return string.Format("{0}-{1}-{2}", silo.Endpoint.Address, silo.Endpoint.Port, silo.Generation);
        }

        public Dictionary<string, AttributeValue> GetKeys()
        {
            var keys = new Dictionary<string, AttributeValue>();
            keys.Add(DEPLOYMENT_ID_PROPERTY_NAME, new AttributeValue(DeploymentId));
            keys.Add(SILO_IDENTITY_PROPERTY_NAME, new AttributeValue(SiloIdentity));
            return keys;
        }
    }
}
