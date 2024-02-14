using Maurer.OktaFilter.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Maurer.OktaFilter.Helpers
{
    [ExcludeFromCodeCoverage]
    public class DistributedCacheHelper : IDistributedCacheHelper
    {
        private readonly IDistributedCache _cache;

        public DistributedCacheHelper(IDistributedCache cache) => _cache = cache;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsJson(string obj)
        {
            try
            {
                _ = System.Text.Json.JsonDocument.Parse(obj);
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<string?> Get(string key)
        {
            var raw = await _cache.GetAsync(key);
            return raw != null ? Encoding.UTF8.GetString(raw) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task Set(string key, object value, DistributedCacheEntryOptions options)
        {
            var serializedValue = value.GetType() != typeof(string)
                ? JsonConvert.SerializeObject(value)
                : (string)value;

            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(serializedValue), options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> Has(string key)
        {
            var entry = await _cache.GetAsync(key);
            return entry != null && entry.Any();
        }
    }
}