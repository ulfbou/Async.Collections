// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Async.Locks;

using System.Collections.Concurrent;

namespace Async.Collections
{
    public class AsyncQueue<T> : IAsyncDisposable, IAsyncQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly AsyncLock _lock = new AsyncLock();
        private bool _disposed;

        public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _queue.Enqueue(item);
            }
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (_queue.TryDequeue(out T? item))
                {
                    return item;
                }
            }

            throw new InvalidOperationException("Queue is empty after lock acquisition.");
        }

        public async ValueTask<(bool Success, T Item)> TryPeekAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (_queue.TryPeek(out T? item))
                {
                    return (true, item);
                }
            }

            return (false, default!);
        }

        public int Count
        {
            get
            {
                EnsureNotDisposed();

                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        private void EnsureNotDisposed()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(AsyncQueue<T>));
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await using var releaser = await _lock.AcquireAsync().ConfigureAwait(false);

            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _queue.Clear();
        }
    }
}
