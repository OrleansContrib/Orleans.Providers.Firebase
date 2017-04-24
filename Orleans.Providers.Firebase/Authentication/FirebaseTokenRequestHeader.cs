namespace Orleans.Providers.Firebase.Authentication
{
    using Newtonsoft.Json;

    public class FirebaseTokenRequestHeader
    {
        [JsonProperty("alg")]
        public string Alg { get; set; }

        [JsonProperty("kid")]
        public string Kid { get; set; }

        [JsonProperty("typ")]
        public string Typ { get; set; }
    }
}
