namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    using System.Threading.Tasks;

    public interface ICustomPathInstanceGrain : IGrainWithStringKey
    {
        Task SetValue(int value);
    }
}
