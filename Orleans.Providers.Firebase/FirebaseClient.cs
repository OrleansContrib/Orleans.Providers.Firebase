using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.Firebase
{
    public class FirebaseClient
    {
        private HttpClient _httpClient;

        public FirebaseClient()
        {
            _httpClient = new HttpClient();
        }

        public string Auth { get; set; }
        public string BasePath { get; set; }
        
        public async Task DeleteAsync(string requestUri)
        {
            var response = await _httpClient.DeleteAsync(ConstructFirebasePath(requestUri));
            ThrowIfRequestFailed(response);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<T> GetAsync<T>(string requestUri)
        {
            var response = await _httpClient.GetAsync(ConstructFirebasePath(requestUri));
            ThrowIfRequestFailed(response);
            var content = await response.Content.ReadAsStringAsync();
            if (typeof(T) == typeof(string))
                return (T)(object)content;
            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task PutAsync(string requestUri, object content)
        {
            var response = await _httpClient.PutAsync(ConstructFirebasePath(requestUri),
                new StringContent(JsonConvert.SerializeObject(content).ToString(), Encoding.UTF8, "application/json"));
            ThrowIfRequestFailed(response);
        }

        private string ConstructFirebasePath(string path)
        {
            return $"{BasePath}/{path}.json" + (Auth == null ? "" : $"?auth={Auth}");
        }

        private void ThrowIfRequestFailed(HttpResponseMessage response, [CallerMemberName] string operation = null)
        {
            if (response.IsSuccessStatusCode)
                return;

            throw new HttpRequestException($"HTTP requested failed with status code '{response.StatusCode}' for operation '{operation}'.");
        }
    }
}
