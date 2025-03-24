// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Async.Collections
{
    public class AsyncGraphCollection<TNode, TNodeId> : IAsyncEnumerable<KeyValuePair<TNode, TNodeId>>, IAsyncDisposable where TNodeId : notnull
    {
        private readonly ConcurrentDictionary<TNodeId, TNode> _nodes = new ConcurrentDictionary<TNodeId, TNode>();
        private readonly ConcurrentDictionary<TNodeId, ConcurrentDictionary<TNodeId, byte>> _edges = new ConcurrentDictionary<TNodeId, ConcurrentDictionary<TNodeId, byte>>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly object _lock = new object();
        private bool _disposed;

        public async ValueTask AddNodeAsync(TNodeId nodeId, TNode node, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _nodes.TryAdd(nodeId, node);
                    _edges.TryAdd(nodeId, new ConcurrentDictionary<TNodeId, byte>());
                }
            }, cancellationToken);
        }

        public async ValueTask AddEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_edges.TryGetValue(fromNodeId, out var neighbors))
                    {
                        neighbors.TryAdd(toNodeId, 0);
                    }
                }
            }, cancellationToken);
        }

        public async ValueTask<bool> RemoveNodeAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    _nodes.TryRemove(nodeId, out _);
                    _edges.TryRemove(nodeId, out _);

                    foreach (var neighbors in _edges.Values)
                    {
                        neighbors.TryRemove(nodeId, out _);
                    }

                    return true;
                }
            }, cancellationToken);
        }

        public async ValueTask<bool> RemoveEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_edges.TryGetValue(fromNodeId, out var neighbors))
                    {
                        return neighbors.TryRemove(toNodeId, out _);
                    }

                    return false;
                }
            }, cancellationToken);
        }

        public async ValueTask<IEnumerable<TNodeId>> GetNeighborsAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_edges.TryGetValue(nodeId, out var neighbors))
                    {
                        return neighbors.Keys.ToList();
                    }

                    return Enumerable.Empty<TNodeId>();
                }
            }, cancellationToken);
        }

        public async ValueTask<IEnumerable<TNodeId>> DepthFirstSearchAsync(TNodeId startNodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            return await Task.Run(async () =>
            {
                var visited = new HashSet<TNodeId>();
                var stack = new Stack<TNodeId>();
                var result = new List<TNodeId>();

                lock (_lock)
                {
                    if (_nodes.ContainsKey(startNodeId))
                    {
                        stack.Push(startNodeId);
                    }
                }

                while (stack.Count > 0)
                {
                    TNodeId currentNodeId;

                    lock (_lock)
                    {
                        currentNodeId = stack.Pop();
                    }

                    if (!visited.Contains(currentNodeId))
                    {
                        visited.Add(currentNodeId);
                        result.Add(currentNodeId);

                        var neighbors = await GetNeighborsAsync(currentNodeId, cancellationToken);
                        foreach (var neighborId in neighbors)
                        {
                            if (!visited.Contains(neighborId))
                            {
                                lock (_lock)
                                {
                                    stack.Push(neighborId);
                                }
                            }
                        }
                    }
                }

                return result;
            }, cancellationToken);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncGraphCollection<TNode, TNodeId>));
            }
        }

        public async IAsyncEnumerator<KeyValuePair<TNode, TNodeId>> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            List<KeyValuePair<TNodeId, TNode>> nodes;
            lock (_lock)
            {
                nodes = _nodes.ToList();
            }

            foreach (var node in nodes)
            {
                yield return new KeyValuePair<TNode, TNodeId>(node.Value, node.Key);
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

        internal ValueTask<bool> ContainsNodeAsync(TNodeId nodeId)
        {
            lock (_lock)
            {
                return ValueTask.FromResult(_nodes.ContainsKey(nodeId));
            }
        }

        internal ValueTask<TNode?> GetNodeAsync(TNodeId nodeId)
        {
            lock (_lock)
            {
                if (_nodes.TryGetValue(nodeId, out TNode? node))
                {
                    return ValueTask.FromResult<TNode?>(node);
                }
            }

            return ValueTask.FromResult(default(TNode));
        }

        internal ValueTask<bool> ContainsEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId)
        {
            lock (_lock)
            {
                if (_edges.TryGetValue(fromNodeId, out var neighbors))
                {
                    return ValueTask.FromResult(neighbors.ContainsKey(toNodeId));
                }
            }

            return ValueTask.FromResult(false);
        }
    }
}
