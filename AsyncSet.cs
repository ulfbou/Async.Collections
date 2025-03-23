// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections.Tests
{
    public class AsyncSet<T>
        where T : notnull
    {
        private readonly ConcurrentDictionary<T, byte> _set = new ConcurrentDictionary<T, byte>();

        public ValueTask<bool> AddAsync(T item, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_set.TryAdd(item, 0));
        }

        public ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_set.TryRemove(item, out _));
        }

        public ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_set.ContainsKey(item));
        }

        public int Count => _set.Count;
    }
}
