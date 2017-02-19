using System.Threading.Tasks;

namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    public interface IIntegerGrain : IGrainWithGuidKey
    {
        Task SetValue(int value);
    }
}
