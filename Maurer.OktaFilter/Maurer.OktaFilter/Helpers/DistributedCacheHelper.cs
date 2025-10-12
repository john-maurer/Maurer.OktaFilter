using Maurer.OktaFilter.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Maurer.OktaFilter.Helpers
{
    public class DistributedCacheHelper : IDistributedCacheHelper
    {
        private readonly IDistributedCache _cache;

        public DistributedCacheHelper(IDistributedCache cache) => _cache = cache;

        public static bool IsJson(string? plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return false;

            try
            {
                _ = JToken.Parse(plainText);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        public async Task<string?> Get(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace
                (key, $"Parameter '{nameof(key)}' in DistributedCacheHelper.Get cannot be null or whitespace");

            return await _cache.GetStringAsync(key);
        }

        public async Task Set(string key, object value, DistributedCacheEntryOptions options)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace ("Cannot be null or whitespace");

            if (value is null) throw new ArgumentException("Value cannot be null or whitespace");
            if (options is null) throw new ArgumentException("DistributedCacheEntryOptions cannot be null or whitespace");

            var serializedValue = value as string ?? JsonConvert.SerializeObject(value);

            await _cache.SetStringAsync(key, serializedValue, options);
        }

        public async Task<bool> Has(string key)
        {
            var entry = await _cache.GetAsync(key);
            return entry is { Length: > 0 };
        }
    }
}