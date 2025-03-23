// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections.Tests
{
    public class AsyncBag<T>
    {
        private readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();

        public ValueTask AddAsync(T item, CancellationToken cancellationToken = default)
        {
            _bag.Add(item);
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryTakeAsync(out T? item, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_bag.TryTake(out item));
        }

        public ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_bag.ToList());
        }

        public int Count => _bag.Count;
    }
}
