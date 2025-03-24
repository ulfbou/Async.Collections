// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Async.Collections
{
    public class AsyncDisposableCollection<T> : IAsyncEnumerable<T>, IAsyncDisposable
        where T : class, IDisposable
    {
        private readonly ConcurrentBag<T> _items = new ConcurrentBag<T>();
        private readonly object _lock = new object();
        private readonly ILogger<AsyncDisposableCollection<T>> _logger;

        public AsyncDisposableCollection(ILogger<AsyncDisposableCollection<T>>? logger = default)
        {
            _logger = logger ?? NullLogger<AsyncDisposableCollection<T>>.Instance;
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                var items = _items.ToList();
                var removed = items.Remove(item);

                if (removed)
                {
                    _items.Clear();

                    foreach (var i in items)
                    {
                        _items.Add(i);
                    }
                }

                return removed;
            }
        }

        public async IAsyncEnumerable<T> ToListAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            List<T> items;

            lock (_lock)
            {
                items = _items.ToList();
            }

            foreach (var item in items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var item in _items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            List<T> itemsToDispose;

            lock (_lock)
            {
                itemsToDispose = _items.ToList();
                _items.Clear();
            }

            var tasks = itemsToDispose.Select(async item =>
            {
                try
                {
                    if (item is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else
                    {
                        item.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while disposing an item.");
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
