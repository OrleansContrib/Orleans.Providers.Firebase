namespace Orleans.Providers.Firebase.Membership
{
    public class AttributeValue
    {
        private string deploymentId;

        public AttributeValue()
        {
        }

        public AttributeValue(string deploymentId)
        {
            this.deploymentId = deploymentId;
        }

        public string N { get; internal set; }

        public string S { get; internal set; }
    }
}