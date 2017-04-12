namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    using System.Threading.Tasks;

    public interface ICustomPathSingletonGrain : IGrainWithGuidKey
    {
        Task SetValue(int value);
    }
}
