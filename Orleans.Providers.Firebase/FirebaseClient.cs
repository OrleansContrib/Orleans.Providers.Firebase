namespace Orleans.Providers.Firebase
{
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Orleans.Providers.Firebase.Authentication;

    public class FirebaseClient
    {
        private string accessToken;
        private HttpClient httpClient;
        private JsonSerializerSettings settings;
        private FirebaseTokenRefresher tokenRefresher;

        public FirebaseClient()
        {
            this.httpClient = new HttpClient();
            this.settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            this.tokenRefresher = new FirebaseTokenRefresher();
        }

        public string BasePath { get; set; }

        public FirebaseServiceKey Key { get; set; }

        public static string ToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (value == null)
                {
                    return null;
                }

                return value;
            }

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        public async Task DeleteAsync(string requestUri)
        {
            var response = await this.httpClient.DeleteAsync(this.ConstructFirebasePath(requestUri));
            this.ThrowIfRequestFailed(response);
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        public async Task<T> GetAsync<T>(string requestUri)
        {
            var response = await this.httpClient.GetAsync(this.ConstructFirebasePath(requestUri));
            this.ThrowIfRequestFailed(response);
            var content = await response.Content.ReadAsStringAsync();
            if (typeof(T) == typeof(string))
            {
                return (T)(object)content;
            }

            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task PutAsync(string requestUri, object content)
        {
            var response = await this.httpClient.PutAsync(
                this.ConstructFirebasePath(requestUri),
                new StringContent(JsonConvert.SerializeObject(content, this.settings).ToString(), Encoding.UTF8, "application/json"));
            this.ThrowIfRequestFailed(response);
        }

        public async Task Initialize()
        {
            await this.RefreshToken();
        }

        private string ConstructFirebasePath(string path)
        {
            return $"{this.BasePath}/{path}.json" + (this.accessToken == null ? string.Empty : $"?access_token={this.accessToken}");
        }

        private async Task RefreshToken()
        {
            if (this.Key == null)
            {
                return;
            }

            this.accessToken = await this.tokenRefresher.RefreshTokenAsync(this.httpClient, this.Key);
        }

        private void ThrowIfRequestFailed(HttpResponseMessage response, [CallerMemberName] string operation = null)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            throw new HttpRequestException($"HTTP requested failed with status code '{response.StatusCode}' for operation '{operation}'.");
        }
    }
}
