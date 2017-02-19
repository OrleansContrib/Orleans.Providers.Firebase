using Orleans.Providers.Firebase.Tests.Host.Grains;
using System.Threading.Tasks;

namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    public interface IComplexGrain : IGrainWithStringKey
    {
        Task Clear();
        Task SetValue(ComplexState value);
    }
}
