// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Async.Locks;

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
        private readonly AsyncLock _lock = new AsyncLock();

        public void Add(T item)
        {
            _items.Add(item);
        }

        public async Task<bool> RemoveAsync(T item)
        {
            await using (await _lock.AcquireAsync())
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

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
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
            List<T> items;

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                items = _items.ToList();
            }

            foreach (var item in _items)
            {
                yield return item;
            }
        }

        public async ValueTask DisposeAsync()
        {
            List<T> itemsToDispose;

            await using (await _lock.AcquireAsync())
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
                catch { }
            });

            await Task.WhenAll(tasks);
        }
    }
}
