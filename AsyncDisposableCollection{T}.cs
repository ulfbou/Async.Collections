// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public class AsyncDisposableCollection<T> : IAsyncEnumerable<T>, IAsyncDisposable, IEnumerable<T>
        where T : class, IDisposable
    {
        private readonly ConcurrentBag<T> _items = new ConcurrentBag<T>();
        private readonly object _lock = new object();
        private readonly ILogger<ConcurrentDisposableCollection<T>> _logger;

        public AsyncDisposableCollection(ILogger<ConcurrentDisposableCollection<T>>? logger = default)
        {
            _logger = logger ?? NullLogger<ConcurrentDisposableCollection<T>>.Instance;
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

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var item in _items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }
    }
}
