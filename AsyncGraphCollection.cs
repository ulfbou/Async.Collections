// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections
{
    public class AsyncGraphCollection<TNode, TNodeId> where TNodeId : notnull
    {
        private readonly ConcurrentDictionary<TNodeId, TNode> _nodes = new ConcurrentDictionary<TNodeId, TNode>();
        private readonly ConcurrentDictionary<TNodeId, ConcurrentDictionary<TNodeId, byte>> _edges = new ConcurrentDictionary<TNodeId, ConcurrentDictionary<TNodeId, byte>>();

        public ValueTask AddNodeAsync(TNodeId nodeId, TNode node, CancellationToken cancellationToken = default)
        {
            _nodes.TryAdd(nodeId, node);
            _edges.TryAdd(nodeId, new ConcurrentDictionary<TNodeId, byte>());
            return ValueTask.CompletedTask;
        }

        public ValueTask AddEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default)
        {
            if (_edges.TryGetValue(fromNodeId, out var neighbors))
            {
                neighbors.TryAdd(toNodeId, 0);
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> RemoveNodeAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            _nodes.TryRemove(nodeId, out _);
            _edges.TryRemove(nodeId, out _);
            foreach (var node in _edges.Values)
            {
                node.TryRemove(nodeId, out _);
            }
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> RemoveEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default)
        {
            if (_edges.TryGetValue(fromNodeId, out var neighbors))
            {
                return ValueTask.FromResult(neighbors.TryRemove(toNodeId, out _));
            }
            return ValueTask.FromResult(false);
        }

        public async ValueTask<IEnumerable<TNodeId>> GetNeighborsAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            if (_edges.TryGetValue(nodeId, out var neighbors))
            {
                return await ValueTask.FromResult(neighbors.Keys);
            }
            return await ValueTask.FromResult(Enumerable.Empty<TNodeId>());
        }

        public async ValueTask<IEnumerable<TNodeId>> DepthFirstSearchAsync(TNodeId startNodeId, CancellationToken cancellationToken = default)
        {
            var visited = new HashSet<TNodeId>();
            var stack = new Stack<TNodeId>();
            var result = new List<TNodeId>();

            if (_nodes.ContainsKey(startNodeId))
            {
                stack.Push(startNodeId);

                while (stack.Count > 0)
                {
                    var nodeId = stack.Pop();
                    if (!visited.Contains(nodeId))
                    {
                        visited.Add(nodeId);
                        result.Add(nodeId);

                        var neighbors = await GetNeighborsAsync(nodeId, cancellationToken);
                        foreach (var neighborId in neighbors)
                        {
                            if (!visited.Contains(neighborId))
                            {
                                stack.Push(neighborId);
                            }
                        }
                    }
                }
            }

            return result;
        }

        internal ValueTask<TNode?> GetNodeAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            _nodes.TryGetValue(nodeId, out TNode? node);
            return ValueTask.FromResult(node);
        }

        internal ValueTask<bool> ContainsEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default)
        {
            if (_edges.TryGetValue(fromNodeId, out var neighbors))
            {
                return ValueTask.FromResult(neighbors.ContainsKey(toNodeId));
            }
            return ValueTask.FromResult(false);
        }

        internal ValueTask<bool> ContainsNodeAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_nodes.ContainsKey(nodeId));
        }
    }
}
