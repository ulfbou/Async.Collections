// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Async.Collections
{
    public class AsyncBag<T> : IAsyncEnumerable<T>, IAsyncDisposable
    {
        private readonly ConcurrentBag<T> _bag = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _disposed;

        public async ValueTask AddAsync(T item, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken);
            try
            {
                _bag.Add(item);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<(bool Success, T? Item)> TryTakeAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken);
            try
            {
                bool success = _bag.TryTake(out var item);
                return (success, item);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async IAsyncEnumerable<T> ToListAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken);
            try
            {
                foreach (var item in _bag)
                {
                    yield return item;
                }
            }
            finally
            {
                _lock.Release();
            }

            await Task.CompletedTask;
        }

        public int Count
        {
            get
            {
                EnsureNotDisposed();

                _lock.Wait();
                try
                {
                    return _bag.Count;
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
                throw new ObjectDisposedException(nameof(AsyncBag<T>));
            }
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken);
            try
            {
                foreach (var item in _bag)
                {
                    yield return item;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_disposed) return;
                _bag.Clear();
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
