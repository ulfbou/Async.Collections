// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections
{
    public class AsyncQueue<T> : IAsyncDisposable
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1); // Acts as an AsyncLock.
        private bool _disposed;

        public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            await _lock.WaitAsync(cancellationToken);

            try
            {
                _queue.Enqueue(item);
                _semaphore.Release();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _semaphore.WaitAsync(cancellationToken);
            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (_queue.TryDequeue(out T? item))
                {
                    return item;
                }

                throw new InvalidOperationException("Queue is empty after semaphore release.");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<(bool Success, T Item)> TryPeekAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (_queue.TryPeek(out T? item))
                {
                    return (true, item);
                }

                return (false, default!);
            }
            finally
            {
                _lock.Release();
            }
        }

        public int Count
        {
            get
            {
                EnsureNotDisposed();
                _lock.Wait();

                try
                {
                    return _queue.Count;
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ConcurrentQueue<T>));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_disposed) return;
                _disposed = true;
            }
            finally
            {
                _lock.Release();
            }

            _semaphore.Dispose();
            _lock.Dispose();
            await Task.CompletedTask;
        }
    }
}
