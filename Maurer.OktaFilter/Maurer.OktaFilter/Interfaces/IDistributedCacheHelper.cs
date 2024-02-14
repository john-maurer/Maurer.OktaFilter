using Microsoft.Extensions.Caching.Distributed;

namespace Maurer.OktaFilter.Interfaces
{
    public interface IDistributedCacheHelper
    {
        Task<string?> Get(string key);
        Task Set(string key, object value, DistributedCacheEntryOptions options);
        Task<bool> Has(string key);
    }
}
