namespace Orleans.Providers.Firebase.Tests.Host.Interfaces
{
    using System.Threading.Tasks;
    using Orleans.Providers.Firebase.Tests.Host.Grains;

    public interface IComplexGrain : IGrainWithStringKey
    {
        Task Clear();

        Task SetValue(ComplexState value);
    }
}
