using System.Threading.Tasks;
using Orleans.Providers.Firebase.Tests.Host.Interfaces;

namespace Orleans.Providers.Firebase.Tests.Host.Grains
{
    public class ComplexGrain : Grain<ComplexState>, IComplexGrain
    {
        public async Task Clear()
        {
            await ClearStateAsync();
        }

        public async Task SetValue(ComplexState value)
        {
            State = value;
            await WriteStateAsync();
        }
    }
}
