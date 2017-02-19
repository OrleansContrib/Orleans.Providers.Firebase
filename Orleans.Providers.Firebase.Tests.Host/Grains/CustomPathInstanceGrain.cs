using System.Threading.Tasks;
using Orleans.Providers.Firebase.Tests.Host.Interfaces;

namespace Orleans.Providers.Firebase.Tests.Host.Grains
{
    public class CustomPathInstanceGrain : Grain<int>, ICustomPathInstanceGrain
    {
        public async Task SetValue(int value)
        {
            State = value;
            await WriteStateAsync();
        }
    }
}
