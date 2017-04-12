namespace Orleans.Providers.Firebase
{
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class FirebaseClient
    {
        private HttpClient httpClient;

        public FirebaseClient()
        {
            this.httpClient = new HttpClient();
        }

        public string Auth { get; set; }

        public string BasePath { get; set; }

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
                new StringContent(JsonConvert.SerializeObject(content).ToString(), Encoding.UTF8, "application/json"));
            this.ThrowIfRequestFailed(response);
        }

        private string ConstructFirebasePath(string path)
        {
            return $"{this.BasePath}/{path}.json" + (this.Auth == null ? string.Empty : $"?auth={this.Auth}");
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
