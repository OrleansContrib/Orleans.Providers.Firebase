namespace Orleans.Providers.Firebase.Authentication
{
    using Newtonsoft.Json;

    public class FirebaseTokenRequestPayload
    {
        [JsonProperty("aud")]
        public string Aud { get; set; }

        [JsonProperty("exp")]
        public long Exp { get; set; }

        [JsonProperty("iat")]
        public long Iat { get; set; }

        [JsonProperty("iss")]
        public string Iss { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }
    }
}
