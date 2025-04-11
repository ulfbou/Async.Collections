// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

using System.Collections.Concurrent;

namespace Async.Threading
{
    /// <summary>
    /// An enhanced asynchronous lock implementation that supports event-driven triggers,
    /// batch processing, and DI-friendly configuration.
    /// </summary>
    public class AsyncLock : IAsyncLock
    {
        private readonly object _lockObject = new object();
        private TaskCompletionSource<Releaser<AsyncLock>>? _releaserTcs;
        private readonly TimeSpan _defaultTimeout;
        private long _acquisitionCount;
        private long _releaseCount;
        private readonly ConcurrentDictionary<long, int> _batchLocks = new ConcurrentDictionary<long, int>();

        public event EventHandler? LockAcquired;
        public event EventHandler? LockReleased;

        /// <summary>
        /// Initializes a new instance of AsyncLock.
        /// </summary>
        /// <param name="defaultTimeout">
        /// Optional default timeout for acquiring the lock.
        /// Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.
        /// </param>
        public AsyncLock(TimeSpan? defaultTimeout = null)
        {
            _defaultTimeout = defaultTimeout ?? Timeout.InfiniteTimeSpan;
            _releaserTcs = new TaskCompletionSource<Releaser<AsyncLock>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _releaserTcs.SetResult(new Releaser<AsyncLock>(this));
        }

        event EventHandler IAsyncLock.LockAcquired
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler IAsyncLock.LockReleased
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Asynchronously acquires the lock.
        /// </summary>
        public async Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            return await AcquireLock(cancellationToken, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously acquires the lock for batch operations.
        /// </summary>
        public async Task<IAsyncDisposable> BatchLockAsync(CancellationToken cancellationToken = default)
        {
            return await AcquireLock(cancellationToken, true).ConfigureAwait(false);
        }

        private async Task<IAsyncDisposable> AcquireLock(CancellationToken cancellationToken, bool isBatch)
        {
            TaskCompletionSource<Releaser<AsyncLock>> tcs;
            long threadId = Thread.CurrentThread.ManagedThreadId;
            lock (_lockObject)
            {
                if (isBatch && _batchLocks.TryGetValue(threadId, out int count) && count > 0)
                {
                    _batchLocks[threadId] = count + 1;
                    Interlocked.Increment(ref _acquisitionCount);
                    LockAcquired?.Invoke(this, EventArgs.Empty);
                    return new Releaser<AsyncLock>(this, isBatch);
                }

                tcs = _releaserTcs!;
                _releaserTcs = new TaskCompletionSource<Releaser<AsyncLock>>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            if (_defaultTimeout != Timeout.InfiniteTimeSpan)
            {
                var delayTask = Task.Delay(_defaultTimeout, cancellationToken);
                var completedTask = await Task.WhenAny(tcs.Task, delayTask).ConfigureAwait(false);
                if (completedTask == delayTask)
                {
                    lock (_lockObject)
                    {
                        _releaserTcs?.TrySetCanceled();
                    }
                    throw new TimeoutException("Timeout while waiting to acquire the lock.");
                }
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            Interlocked.Increment(ref _acquisitionCount);
            LockAcquired?.Invoke(this, EventArgs.Empty);

            if (isBatch)
            {
                _batchLocks[threadId] = 1;
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        internal void Release(bool isBatch = false)
        {
            long threadId = Thread.CurrentThread.ManagedThreadId;
            lock (_lockObject)
            {
                if (isBatch && _batchLocks.TryGetValue(threadId, out int count) && count > 1)
                {
                    _batchLocks[threadId] = count - 1;
                    Interlocked.Increment(ref _releaseCount);
                    LockReleased?.Invoke(this, EventArgs.Empty);
                    return;
                }

                _batchLocks.TryRemove(threadId, out _);

                if (_releaserTcs == null)
                {
                    _releaserTcs = new TaskCompletionSource<Releaser<AsyncLock>>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                _releaserTcs.SetResult(new Releaser<AsyncLock>(this));
            }
            Interlocked.Increment(ref _releaseCount);
            LockReleased?.Invoke(this, EventArgs.Empty);
        }

        public long AcquisitionCount => Interlocked.Read(ref _acquisitionCount);
        public long ReleaseCount => Interlocked.Read(ref _releaseCount);

        long IAsyncLock.ReleaseCount => throw new NotImplementedException();

        long IAsyncLock.AcquisitionCount => throw new NotImplementedException();

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }

        Task<IAsyncDisposable> IAsyncLock.LockAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        Task<IAsyncDisposable> IAsyncLock.BatchLockAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        void IAsyncLock.Release(bool isBatch) => throw new NotImplementedException();
        ValueTask IAsyncDisposable.DisposeAsync() => throw new NotImplementedException();

        /// <summary>
        /// Represents a releaser that releases the lock when disposed.
        /// </summary>
        public readonly struct Releaser<TLock> : IAsyncDisposable where TLock : IAsyncLock
        {
            private readonly TLock _asyncLock;

            /// <summary>
            /// Initializes a new instance of the <see langword="readonly"/> <see cref="AsyncReleaser"/> structure.
            /// </summary>
            /// <param name="asyncLock">The <see cref="IAsyncLock"/> to release.</param>
            internal Releaser(TLock asyncLock, bool isBatch = false)
            {
                _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
            }

            /// <inheritdoc />
            public ValueTask DisposeAsync()
            {
                _asyncLock?.Release();
                return ValueTask.CompletedTask;
            }
        }
    }

    public static class AsyncLockExtensions
    {
        public static IServiceCollection AddAsyncLock(this IServiceCollection services, TimeSpan? defaultTimeout = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.Add(new ServiceDescriptor(typeof(IAsyncLock), provider => new AsyncLock(defaultTimeout), lifetime));
            return services;
        }
    }
}
