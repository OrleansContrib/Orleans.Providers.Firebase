namespace Orleans.Providers.Firebase.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Orleans.Providers.Firebase.Authentication;
    using Orleans.Runtime;
    using Orleans.Storage;

    public class FirebaseStorageProvider : IStorageProvider
    {
        private Dictionary<string, string> customPaths;
        private FirebaseClient firebaseClient;

        public Logger Log { get; set; }

        public string Name { get; set; }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            await this.firebaseClient.DeleteAsync(this.ConstructGrainPath(grainType, grainReference));
        }

        public Task Close()
        {
            this.firebaseClient.Dispose();
            return TaskDone.Done;
        }

        public async Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            var props = config.Properties;
            this.firebaseClient = new FirebaseClient();
            if (props.ContainsKey("Key"))
            {
                this.firebaseClient.Key = FirebaseServiceKey.FromBase64(props["Key"]);
            }

            this.firebaseClient.BasePath = props["BasePath"];
            this.customPaths = props.ContainsKey("CustomPaths")
                ? props["CustomPaths"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Split('='))
                    .ToDictionary(split => split[0], split => split[1])
                : new Dictionary<string, string>();

            await this.firebaseClient.Initialize();
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var content = await this.firebaseClient.GetAsync<string>(this.ConstructGrainPath(grainType, grainReference));
            if (content == "null")
            {
                return;
            }

            grainState.State = JsonConvert.DeserializeObject(content, grainState.State.GetType());
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            await this.firebaseClient.PutAsync(this.ConstructGrainPath(grainType, grainReference), grainState.State);
        }

        private string ConstructGrainPath(string grainType, GrainReference grainReference)
        {
            var grainTypeName = grainType.Split('.').Last();
            var grainRefString = grainReference.ToString();
            var instanceName = grainRefString.Contains("+")
                ? grainRefString.Split('+')[1]
                : grainReference.GetPrimaryKey().ToString();
            if (this.customPaths.ContainsKey(grainTypeName))
            {
                return this.customPaths[grainTypeName].Replace("{instance}", instanceName);
            }

            var entityPath = grainTypeName.Length > 5 && grainTypeName.EndsWith("Grain")
                ? grainTypeName.Substring(0, grainTypeName.Length - 5)
                : grainTypeName;
            return $"{entityPath}/{instanceName}";
        }
    }
}