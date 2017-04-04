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

        private Dictionary<string, string> _customPaths;
        private FirebaseClient _firebaseClient;
        
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            await _firebaseClient.DeleteAsync(ConstructGrainPath(grainType, grainReference));
        }

        public Task Close()
        {
            _firebaseClient.Dispose();
            return TaskDone.Done;
        }

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            var props = config.Properties;
            _firebaseClient = new FirebaseClient();
            if (props.ContainsKey("Auth"))
                _firebaseClient.Auth = props["Auth"];
            _firebaseClient.BasePath = props["BasePath"];
            _customPaths = props.ContainsKey("CustomPaths")
                ? props["CustomPaths"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Split('='))
                    .ToDictionary(split => split[0], split => split[1])
                : new Dictionary<string, string>();
            return TaskDone.Done;
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var content = await _firebaseClient.GetAsync<string>(ConstructGrainPath(grainType, grainReference));
            if (content == "null")
                return;
            var state = JsonConvert.DeserializeObject(content, grainState.State.GetType());
            grainState.State = state;
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            await _firebaseClient.PutAsync(ConstructGrainPath(grainType, grainReference), grainState.State);
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