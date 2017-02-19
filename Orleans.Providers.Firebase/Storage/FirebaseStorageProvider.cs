using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Storage;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Collections.Generic;

namespace Orleans.Providers.Firebase.Storage
{
    public class FirebaseStorageProvider : IStorageProvider
    {
        public Logger Log { get; set; }

        public string Name { get; set; }

        private string _auth;
        private string _basePath;
        private Dictionary<string, string> _customPaths;
        private HttpClient _httpClient;
        
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            await _httpClient.DeleteAsync(ConstructFirebasePath(grainType, grainReference));
        }

        public Task Close()
        {
            _httpClient.Dispose();
            return TaskDone.Done;
        }

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            var props = config.Properties;
            _httpClient = new HttpClient();
            if (props.ContainsKey("Auth"))
                _auth = props["Auth"];
            _basePath = props["BasePath"];
            _customPaths = props.ContainsKey("CustomPaths")
                ? props["CustomPaths"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Split('='))
                    .ToDictionary(split => split[0], split => split[1])
                : new Dictionary<string, string>();
            return TaskDone.Done;
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var response = await _httpClient.GetAsync(ConstructFirebasePath(grainType, grainReference));
            var content = await response.Content.ReadAsStringAsync();
            if (content == "null")
                return;
            var state = JsonConvert.DeserializeObject(content, grainState.State.GetType());
            grainState.State = state;
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var response = await _httpClient.PutAsync(ConstructFirebasePath(grainType, grainReference),
                new StringContent(JsonConvert.SerializeObject(grainState.State).ToString(), Encoding.UTF8, "application/json"));
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Unexpected status code ({response.StatusCode}) when persisting.");
        }

        private string ConstructFirebasePath(string grainType, GrainReference grainReference)
        {
            return $"{_basePath}/{ConstructGrainPath(grainType, grainReference)}.json" +
                (_auth == null ? "" : $"?auth={_auth}");
        }

        private string ConstructGrainPath(string grainType, GrainReference grainReference)
        {
            var grainTypeName = grainType.Split('.').Last();
            var grainRefString = grainReference.ToString();
            var instanceName = grainRefString.Contains("+")
                ? grainRefString.Split('+')[1]
                : grainReference.GetPrimaryKey().ToString();
            if (_customPaths.ContainsKey(grainTypeName))
                return _customPaths[grainTypeName].Replace("{instance}", instanceName);
            var entityPath = grainTypeName.Length > 5 && grainTypeName.EndsWith("Grain")
                ? grainTypeName.Substring(0, grainTypeName.Length - 5)
                : grainTypeName;
            return $"{entityPath}/{instanceName}";
        }
    }
}