// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections
{
    public class AsyncQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            _queue.Enqueue(item);
            _semaphore.Release();
            await ValueTask.CompletedTask;
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (_queue.TryDequeue(out T? item))
            {
                return item;
            }

            throw new InvalidOperationException("Queue is empty after semaphore release.");
        }

        public ValueTask<(bool Success, T Item)> TryPeekAsync(CancellationToken cancellationToken = default)
        {
            if (_queue.TryPeek(out T? item))
            {
                return ValueTask.FromResult((true, item));
            }

            return ValueTask.FromResult<(bool, T)>((false, default!));
        }

        public int Count => _queue.Count;
    }
}
