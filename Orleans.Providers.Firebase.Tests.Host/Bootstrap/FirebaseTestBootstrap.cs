namespace Orleans.Providers.Firebase.Test.Host.Bootstrap
{
    using System;
    using System.Threading.Tasks;
    using Orleans.Providers.Firebase.Tests.Host.Grains;
    using Orleans.Providers.Firebase.Tests.Host.Interfaces;

    public class FirebaseTestBootstrap : IBootstrapProvider
    {
        public string Name { get; set; }

        public Task Close()
        {
            return TaskDone.Done;
        }

        public async Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            await providerRuntime.GrainFactory.GetGrain<IComplexGrain>("ComplexTest").SetValue(new ComplexState { A = 1, B = 2 });
            await providerRuntime.GrainFactory.GetGrain<IIntegerGrain>(Guid.Empty).SetValue(1);
            await providerRuntime.GrainFactory.GetGrain<ICustomPathInstanceGrain>("instanceId").SetValue(1);
            await providerRuntime.GrainFactory.GetGrain<ICustomPathSingletonGrain>(Guid.Empty).SetValue(1);
        }
    }
}
