namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    using System.Threading.Tasks;

    public interface IIntegerGrain : IGrainWithGuidKey
    {
        Task SetValue(int value);
    }
}
