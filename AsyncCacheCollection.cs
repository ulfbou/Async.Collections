// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections
{
    public class AsyncCacheCollection<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache = new ConcurrentDictionary<TKey, CacheItem<TValue>>();
        private readonly Timer _evictionTimer;
        private readonly TimeSpan _evictionInterval;
        private readonly int? _maxSize;
        private readonly object _lock = new object();

        public AsyncCacheCollection(TimeSpan evictionInterval, int? maxSize = null)
        {
            _evictionInterval = evictionInterval;
            _maxSize = maxSize;
            _evictionTimer = new Timer(EvictExpiredItems!, null, evictionInterval, evictionInterval);
        }

        public async ValueTask<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> valueFactory)
        {
            if (_cache.TryGetValue(key, out var cacheItem))
            {
                cacheItem.LastAccessed = DateTime.UtcNow;
                return cacheItem.Value;
            }

            var newValue = await valueFactory();
            var newCacheItem = new CacheItem<TValue> { Value = newValue, LastAccessed = DateTime.UtcNow };

            _cache.AddOrUpdate(key, newCacheItem, (_, existingItem) =>
            {
                existingItem.LastAccessed = DateTime.UtcNow;
                return existingItem;
            });

            return newValue;
        }

        public bool TryRemove(TKey key)
        {
            return _cache.TryRemove(key, out _);
        }

        private void EvictExpiredItems(object state)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var expiredKeys = _cache.Where(kv => now - kv.Value.LastAccessed > _evictionInterval).Select(kv => kv.Key).ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                if (_maxSize.HasValue && _cache.Count > _maxSize.Value)
                {
                    var sortedKeys = _cache.OrderBy(kv => kv.Value.LastAccessed).Take(_cache.Count - _maxSize.Value).Select(kv => kv.Key).ToList();
                    foreach (var key in sortedKeys)
                    {
                        _cache.TryRemove(key, out _);
                    }
                }
            }
        }

        public void Dispose()
        {
            _evictionTimer?.Dispose();
        }

        private class CacheItem<T>
        {
            public required T Value { get; set; }
            public DateTime LastAccessed { get; set; }
        }
    }
}
