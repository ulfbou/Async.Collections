// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncGraphCollection<TNode, TNodeId> where TNodeId : notnull
    {
        ValueTask AddEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default);
        ValueTask AddNodeAsync(TNodeId nodeId, TNode node, CancellationToken cancellationToken = default);
        ValueTask<IEnumerable<TNodeId>> DepthFirstSearchAsync(TNodeId startNodeId, CancellationToken cancellationToken = default);
        ValueTask DisposeAsync();
        IAsyncEnumerator<KeyValuePair<TNode, TNodeId>> GetAsyncEnumerator(CancellationToken cancellationToken);
        ValueTask<IEnumerable<TNodeId>> GetNeighborsAsync(TNodeId nodeId, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveNodeAsync(TNodeId nodeId, CancellationToken cancellationToken = default);
    }
}
