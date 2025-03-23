﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Collections.Concurrent;

namespace Async.Collections;

public class ConcurrentDisposableCollection<T> : IAsyncDisposable
    where T : class, IDisposable
{
    private readonly ConcurrentBag<T> _items = new ConcurrentBag<T>();
    private readonly object _lock = new object();
    private readonly ILogger<ConcurrentDisposableCollection<T>> _logger;

    public ConcurrentDisposableCollection(ILogger<ConcurrentDisposableCollection<T>>? logger = default)
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
}
