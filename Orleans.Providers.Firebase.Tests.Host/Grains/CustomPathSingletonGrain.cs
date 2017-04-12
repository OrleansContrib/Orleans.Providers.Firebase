namespace Orleans.Providers.Firebase.Tests.Host.Grains
{
    using System.Threading.Tasks;
    using Orleans.Providers.Firebase.Tests.Host.Interfaces;

    public class CustomPathSingletonGrain : Grain<int>, ICustomPathSingletonGrain
    {
        public async Task SetValue(int value)
        {
            this.State = value;
            await this.WriteStateAsync();
        }
    }
}
