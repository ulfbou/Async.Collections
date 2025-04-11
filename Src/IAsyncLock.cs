// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Threading
{
    /// <summary>
    /// Contract for an asynchronous lock that supports configurable options, batch locking,
    /// event-driven notifications, and diagnostics.
    /// </summary>
    public interface IAsyncLock : IAsyncDisposable
    {
        /// <summary>
        /// Asynchronously acquires the lock.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A releaser that releases the lock on disposal.</returns>
        Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously acquires the lock for batch operations.
        /// Batch locking allows multiple operations to be performed under the same lock without intermediate releases.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A releaser that releases the lock on disposal.</returns>
        Task<IAsyncDisposable> BatchLockAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Event that is raised when the lock is acquired.
        /// </summary>
        event EventHandler LockAcquired;

        /// <summary>
        /// Event that is raised when the lock is released.
        /// </summary>
        event EventHandler LockReleased;

        /// <summary>
        /// Gets the total number of times the lock was acquired.
        /// </summary>
        long AcquisitionCount { get; }

        /// <summary>
        /// Gets the total number of times the lock was released.
        /// </summary>
        long ReleaseCount { get; }

        internal void Release(bool isBatch = false);
    }
}
