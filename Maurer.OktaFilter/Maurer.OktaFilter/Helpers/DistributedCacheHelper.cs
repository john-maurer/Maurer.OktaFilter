using Maurer.OktaFilter.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Maurer.OktaFilter.Helpers
{
    public class DistributedCacheHelper : IDistributedCacheHelper
    {
        private readonly IDistributedCache _cache;

        public DistributedCacheHelper(IDistributedCache cache) => _cache = cache;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<string?> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) 
                throw new ArgumentException($"Parameter '{nameof(key)}' in DistributedCacheHelper.Get cannot be null or whitespace");

            var raw = await _cache.GetAsync(key);
            return raw != null ? Encoding.UTF8.GetString(raw) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task Set(string key, object value, DistributedCacheEntryOptions options)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cannot be null or whitespace");
            if (value is null) throw new ArgumentException("Cannot be null or whitespace");
            if (options is null) throw new ArgumentException("Cannot be null or whitespace");

            var serializedValue = value as string ?? JsonConvert.SerializeObject(value);

            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(serializedValue), options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> Has(string key)
        {
            var entry = await _cache.GetAsync(key);
            return entry is { Length: > 0 };
        }
    }
}