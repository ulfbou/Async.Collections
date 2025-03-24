// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Async.Collections
{
    public class AsyncCacheCollection<TKey, TValue> : IAsyncEnumerable<KeyValuePair<TKey, TValue>>, IAsyncDisposable
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache = new ConcurrentDictionary<TKey, CacheItem<TValue>>();
        private readonly TimeSpan _evictionInterval;
        private readonly int? _maxSize;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Func<Exception, Task>? _onErrorAsync;
        private Task? _evictionTask;
        private readonly object _lock = new object();
        private bool _disposed;

        public AsyncCacheCollection(TimeSpan evictionInterval, int? maxSize = null, Func<Exception, Task>? onErrorAsync = null)
        {
            _evictionInterval = evictionInterval;
            _maxSize = maxSize;
            _onErrorAsync = onErrorAsync;
            _evictionTask = Task.Run(EvictExpiredItemsAsync);
        }

        public async ValueTask<TValue> GetOrAddAsync(TKey key, Func<ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            if (key is null)
            {
                throw new ArgumentException("Key cannot be null.", nameof(key));
            }

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
            EnsureNotDisposed();
            return _cache.TryRemove(key, out _);
        }

        private async Task EvictExpiredItemsAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(_evictionInterval, _cancellationTokenSource.Token);

                    lock (_lock)
                    {
                        var now = DateTime.UtcNow;

                        // Evict expired items.
                        var expiredKeys = _cache
                            .Where(kv => now - kv.Value.LastAccessed > _evictionInterval)
                            .Select(kv => kv.Key)
                            .ToList();

                        foreach (var key in expiredKeys)
                        {
                            _cache.TryRemove(key, out _);
                        }

                        // Enforce maximum size.
                        if (_maxSize.HasValue && _cache.Count > _maxSize.Value)
                        {
                            var keysToRemove = _cache
                                .OrderBy(kv => kv.Value.LastAccessed)
                                .Take(_cache.Count - _maxSize.Value)
                                .Select(kv => kv.Key)
                                .ToList();

                            foreach (var key in keysToRemove)
                            {
                                _cache.TryRemove(key, out _);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation, no further action required.
            }
            catch (Exception ex) when (_onErrorAsync != null)
            {
                await _onErrorAsync(ex);
            }
            catch
            {
                // Swallow remaining exceptions if no handler is provided.
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncCacheCollection<TKey, TValue>));
            }
        }

        public async IAsyncEnumerator<KeyValuePair<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            lock (_lock)
            {
                foreach (var kv in _cache)
                {
                    yield return new KeyValuePair<TKey, TValue>(kv.Key, kv.Value.Value);
                }
            }

            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            _cancellationTokenSource.Cancel();

            try
            {
                if (_evictionTask != null)
                {
                    await _evictionTask;
                }
            }
            catch (OperationCanceledException)
            {
                // Gracefully handle cancellation during disposal.
            }

            _cancellationTokenSource.Dispose();
        }

        internal class CacheItem<T>
        {
            public required T Value { get; set; }
            public DateTime LastAccessed { get; set; }
        }
    }
}
