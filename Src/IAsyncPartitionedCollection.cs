// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncPartitionedCollection<T>
    {
        int PartitionCount { get; }

        ValueTask AddAsync(T item, CancellationToken cancellationToken = default);
        ValueTask DisposeAsync();
        IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
        ValueTask MergePartitionsAsync(int partitionId1, int partitionId2, CancellationToken cancellationToken = default);
        ValueTask SplitPartitionAsync(int partitionId, CancellationToken cancellationToken = default);
    }
}
