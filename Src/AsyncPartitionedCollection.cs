// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Async.Collections
{
    public class AsyncPartitionedCollection<T> : IAsyncEnumerable<T>, IAsyncDisposable
    {
        private readonly ConcurrentDictionary<int, ConcurrentBag<T>> _partitions = new ConcurrentDictionary<int, ConcurrentBag<T>>();
        private readonly int _partitionSize;
        private int _partitionCount = 0;
        private readonly object _lock = new object();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed;

        public AsyncPartitionedCollection(int partitionSize)
        {
            _partitionSize = partitionSize;
            AddPartition();
        }

        public async ValueTask AddAsync(T item, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            int partitionId;
            ConcurrentBag<T> partition;

            lock (_lock)
            {
                partitionId = _partitionCount - 1;

                if (!_partitions.TryGetValue(partitionId, out partition!))
                {
                    return;
                }

                partition.Add(item);
            }

            if (partition.Count >= _partitionSize)
            {
                await SplitPartitionAsync(partitionId, cancellationToken);
            }
        }

        public ValueTask SplitPartitionAsync(int partitionId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            lock (_lock)
            {
                if (_partitions.TryGetValue(partitionId, out var partition))
                {
                    var newPartition = new ConcurrentBag<T>();
                    var items = partition.ToList();
                    partition.Clear();

                    int middle = items.Count / 2;
                    for (int i = 0; i < middle; i++)
                    {
                        partition.Add(items[i]);
                    }

                    for (int i = middle; i < items.Count; i++)
                    {
                        newPartition.Add(items[i]);
                    }

                    AddPartition();
                    _partitions[_partitionCount - 1] = newPartition;
                }
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask MergePartitionsAsync(int partitionId1, int partitionId2, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            lock (_lock)
            {
                if (_partitions.TryGetValue(partitionId1, out var partition1) &&
                    _partitions.TryGetValue(partitionId2, out var partition2))
                {
                    foreach (var item in partition2)
                    {
                        partition1.Add(item);
                    }

                    _partitions.TryRemove(partitionId2, out _);
                }
            }

            return ValueTask.CompletedTask;
        }

        private void AddPartition()
        {
            lock (_lock)
            {
                EnsureNotDisposed();
                _partitions.TryAdd(_partitionCount, new ConcurrentBag<T>());
                _partitionCount++;
            }
        }

        public int PartitionCount
        {
            get
            {
                lock (_lock)
                {
                    EnsureNotDisposed();
                    return _partitionCount;
                }
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncPartitionedCollection<T>));
            }
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            lock (_lock)
            {
                foreach (var partition in _partitions)
                {
                    foreach (var item in partition.Value)
                    {
                        yield return item;
                    }
                }
            }

            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            _cancellationTokenSource.Cancel();
            await Task.CompletedTask;
            _cancellationTokenSource.Dispose();
        }
    }
}
