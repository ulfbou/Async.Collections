// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncSet<T> : IAsyncDisposable, IAsyncSet<T> where T : notnull
    {
        private readonly ConcurrentDictionary<T, byte> _set = new ConcurrentDictionary<T, byte>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1); // AsyncLock for thread-safe access.
        private bool _disposed;

        public async ValueTask<bool> AddAsync(T item, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _set.TryAdd(item, 0);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _set.TryRemove(item, out _);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken);
            try
            {
                return _set.ContainsKey(item);
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

                _lock.Wait(); // Synchronously acquire lock for property access.
                try
                {
                    return _set.Count;
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
                throw new ObjectDisposedException(nameof(AsyncSet<T>));
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

            _lock.Dispose();
            await Task.CompletedTask;
        }
    }
}
