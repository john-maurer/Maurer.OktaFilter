using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting.Assertions.Filter
{
    public class MockCache : IDistributedCache
    {
        private sealed record Entry(byte[] value, DateTimeOffset? ExpiresAtUtc);
        private readonly FakeTimeProvider _clock;
        private readonly ConcurrentDictionary<string, Entry> _storage = new();

        private bool TryGetEntry(string key, out Entry entry)
        {
            if (!_storage.TryGetValue(key, out entry!)) return false;

            if (_clock.GetUtcNow() >= entry.ExpiresAtUtc)
            {
                //Deleting specific key value avoids race conditions between calls to TryGetValue and TryRemove (causes delete on fresh value)
                ((ICollection<KeyValuePair<string, Entry>>)_storage)
                    .Remove(new KeyValuePair<string, Entry>(key, entry));

                entry = null!;

                return false;
            }

            return true;
        }

        private DateTimeOffset? ComputeExpiry(DistributedCacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue) return options.AbsoluteExpiration.Value;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
                return _clock.GetUtcNow().Add(options.AbsoluteExpirationRelativeToNow.Value);

            return null;
        }

        public MockCache(FakeTimeProvider clock) => _clock = clock;

        public byte[]? Get(string key) =>
            TryGetEntry(key, out var entry) ? entry.value : null;

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default) => 
            await Task.FromResult(Get(key));

        public void Refresh(string key) { }

        public async Task RefreshAsync(string key, CancellationToken token = default) => await Task.CompletedTask;

        public void Remove(string key) => _storage.TryRemove(key, out _);

        public async Task RemoveAsync(string key, CancellationToken token = default) =>
            await Task.Run(() => _storage.TryRemove(key, out _));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _storage[key] = new Entry(value, ComputeExpiry(options));

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            await Task.Run(() => Set(key, value, options));
    }
}
