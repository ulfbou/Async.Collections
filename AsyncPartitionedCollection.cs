// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections.Tests
{
    public class AsyncPartitionedCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentBag<T>> _partitions = new ConcurrentDictionary<int, ConcurrentBag<T>>();
        private readonly int _partitionSize;
        private int _partitionCount = 0;
        private readonly object _lock = new object();

        public AsyncPartitionedCollection(int partitionSize)
        {
            _partitionSize = partitionSize;
            AddPartition();
        }

        public async ValueTask AddAsync(T item, CancellationToken cancellationToken = default)
        {
            var partitionId = _partitionCount - 1;
            _partitions[partitionId].Add(item);

            if (_partitions[partitionId].Count >= _partitionSize)
            {
                await SplitPartitionAsync(partitionId, cancellationToken);
            }
        }

        public ValueTask SplitPartitionAsync(int partitionId, CancellationToken cancellationToken = default)
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

            return ValueTask.CompletedTask;
        }

        public ValueTask MergePartitionsAsync(int partitionId1, int partitionId2, CancellationToken cancellationToken = default)
        {
            if (_partitions.TryGetValue(partitionId1, out var partition1) && _partitions.TryGetValue(partitionId2, out var partition2))
            {
                foreach (var item in partition2)
                {
                    partition1.Add(item);
                }

                _partitions.TryRemove(partitionId2, out _);
            }

            return ValueTask.CompletedTask;
        }

        private void AddPartition()
        {
            lock (_lock)
            {
                _partitions.TryAdd(_partitionCount, new ConcurrentBag<T>());
                _partitionCount++;
            }
        }

        public int PartitionCount => _partitionCount;
    }
}
