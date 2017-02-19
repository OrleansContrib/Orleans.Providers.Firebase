using System.Threading.Tasks;

namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    public interface ICustomPathSingletonGrain : IGrainWithGuidKey
    {
        Task SetValue(int value);
    }
}
