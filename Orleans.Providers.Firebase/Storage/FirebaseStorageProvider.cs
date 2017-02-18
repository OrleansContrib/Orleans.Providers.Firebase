using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.Firebase.Storage
{
    public class FirebaseStorageProvider : IStorageProvider
    {
        public Logger Log { get; set; }

        public string Name { get; set; }

        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            throw new NotImplementedException();
        }

        public Task Close()
        {
            throw new NotImplementedException();
        }

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            throw new NotImplementedException();
        }

        public Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            throw new NotImplementedException();
        }

        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            throw new NotImplementedException();
        }
    }
}
