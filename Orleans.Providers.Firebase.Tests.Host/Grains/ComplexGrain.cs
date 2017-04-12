namespace Orleans.Providers.Firebase.Tests.Host.Grains
{
    using System.Threading.Tasks;
    using Orleans.Providers.Firebase.Tests.Host.Interfaces;

    public class ComplexGrain : Grain<ComplexState>, IComplexGrain
    {
        public async Task Clear()
        {
            await this.ClearStateAsync();
        }

        public async Task SetValue(ComplexState value)
        {
            this.State = value;
            await this.WriteStateAsync();
        }
    }
}
