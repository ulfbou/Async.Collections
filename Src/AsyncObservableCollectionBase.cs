// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Async.Collections
{
    public abstract class AsyncObservableCollectionBase<TChange> : IAsyncDisposable
    {
        private readonly List<Func<TChange, ValueTask>> _asyncObservers = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _disposed;

        protected async ValueTask NotifyObserversAsync(TChange change)
        {
            List<ValueTask> tasks;

            lock (_asyncObservers)
            {
                tasks = _asyncObservers.Select(observer => observer(change)).ToList();
            }

            await Task.WhenAll(tasks.Select(vt => vt.AsTask()));
        }

        public IDisposable Subscribe(Func<TChange, ValueTask> observer)
        {
            EnsureNotDisposed();

            lock (_asyncObservers)
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
            await _lock.WaitAsync();
            try
            {
                if (_disposed) return;
                _disposed = true;
                _asyncObservers.Clear();
            }
            finally
            {
                _lock.Release();
                _lock.Dispose();
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
