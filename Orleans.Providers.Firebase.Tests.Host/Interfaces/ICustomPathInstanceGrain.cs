using System.Threading.Tasks;

namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    public interface ICustomPathInstanceGrain : IGrainWithStringKey
    {
        Task SetValue(int value);
    }
}
