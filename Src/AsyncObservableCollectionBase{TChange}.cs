// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Async.Locks;

using System.Collections.Generic;

namespace Async.Collections
{
    public abstract class AsyncObservableCollectionBase<TChange> : IAsyncDisposable, IAsyncObservableCollectionBase<TChange>
    {
        private readonly List<Func<TChange, ValueTask>> _asyncObservers = new();
        private readonly AsyncLock _lock = new();
        private bool _disposed;

        protected async ValueTask NotifyObserversAsync(TChange change)
        {
            List<ValueTask> tasks;

            await using (await _lock.AcquireAsync())
            {
                tasks = _asyncObservers.Select(observer => observer(change)).ToList();
            }

            await Task.WhenAll(tasks.Select(vt => vt.AsTask()));
        }

        public async Task<IDisposable> SubscribeAsync(Func<TChange, ValueTask> observer)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync())
            {
                _asyncObservers.Add(observer);
            }

            return new Unsubscriber(this, obs =>
            {
                lock (_asyncObservers)
                {
                    _asyncObservers.Remove(obs);
                }
            }, observer);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await using (await _lock.AcquireAsync().ConfigureAwait(false))
            {
                try
                {
                    if (_disposed) return;
                    _disposed = true;
                    _asyncObservers.Clear();
                }
                finally
                {
                    await _lock.ReleaseAsync().ConfigureAwait(false);
                    await _lock.DisposeAsync().ConfigureAwait(false);
                }

            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly AsyncObservableCollectionBase<TChange> _collection;
            private readonly Action<Func<TChange, ValueTask>> _unsubscribeAsync;
            private readonly Func<TChange, ValueTask> _observer;

            public Unsubscriber(
                AsyncObservableCollectionBase<TChange> collection,
                Action<Func<TChange, ValueTask>> unsubscribeAsync,
                Func<TChange, ValueTask> observer)
            {
                _collection = collection;
                _unsubscribeAsync = unsubscribeAsync;
                _observer = observer;
            }

            public void Dispose() => _unsubscribeAsync(_observer);
        }
    }
}
