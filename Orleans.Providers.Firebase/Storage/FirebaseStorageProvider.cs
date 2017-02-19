using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Storage;
using Newtonsoft.Json;

namespace Orleans.Providers.Firebase.Storage
{
    public class FirebaseStorageProvider : IStorageProvider
    {
        private const string GrainSuffix = "Grain";

        public Logger Log { get; set; }

        public string Name { get; set; }

        private string _basePath;
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
            _httpClient = new HttpClient();
            _basePath = config.Properties["BasePath"];
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
            await _httpClient.PutAsync(ConstructFirebasePath(grainType, grainReference),
                new StringContent(JsonConvert.SerializeObject(grainState.State).ToString(), Encoding.UTF8, "application/json"));
        }

        private string ConstructFirebasePath(string grainType, GrainReference grainReference)
        {
            return $"{_basePath}/{ConstructGrainPath(grainType, grainReference)}.json";
        }
        
        private string ConstructGrainPath(string grainType, GrainReference grainReference)
        {
            var grainTypeName = grainType.Split('.').Last();
            var pathName = grainTypeName.Length > 5 && grainTypeName.EndsWith(GrainSuffix)
                ? grainTypeName.Substring(0, grainTypeName.Length - 5)
                : grainTypeName;
            var grainRefString = grainReference.ToString();
            var instanceName = grainRefString.Contains("+")
                ? grainRefString.Split('+')[1]
                : grainReference.GetPrimaryKey().ToString();
            var path = $"{pathName}/{instanceName}";
            return path;
        }
    }
}
