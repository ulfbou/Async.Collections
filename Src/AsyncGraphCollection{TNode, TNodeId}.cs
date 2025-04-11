// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Async.Locks;

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Async.Collections
{
    public class AsyncGraphCollection<TNode, TNodeId> : IAsyncEnumerable<KeyValuePair<TNode, TNodeId>>, IAsyncDisposable, IAsyncGraphCollection<TNode, TNodeId> where TNodeId : notnull
    {
        private readonly ConcurrentDictionary<TNodeId, TNode> _nodes = new ConcurrentDictionary<TNodeId, TNode>();
        private readonly ConcurrentDictionary<TNodeId, ConcurrentDictionary<TNodeId, byte>> _edges = new ConcurrentDictionary<TNodeId, ConcurrentDictionary<TNodeId, byte>>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly AsyncLock _lock = new AsyncLock();
        private bool _disposed;

        public async ValueTask AddNodeAsync(TNodeId nodeId, TNode node, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                _nodes.TryAdd(nodeId, node);
                _edges.TryAdd(nodeId, new ConcurrentDictionary<TNodeId, byte>());
            }
        }

        public async ValueTask AddEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                if (_edges.TryGetValue(fromNodeId, out var neighbors))
                {
                    neighbors.TryAdd(toNodeId, 0);
                }
            }
        }

        public async ValueTask<bool> RemoveNodeAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                _nodes.TryRemove(nodeId, out _);
                _edges.TryRemove(nodeId, out _);

                foreach (var neighbors in _edges.Values)
                {
                    neighbors.TryRemove(nodeId, out _);
                }

                return true;
            }
        }

        public async ValueTask<bool> RemoveEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                if (_edges.TryGetValue(fromNodeId, out var neighbors))
                {
                    return neighbors.TryRemove(toNodeId, out _);
                }

                return false;
            }
        }

        public async ValueTask<IEnumerable<TNodeId>> GetNeighborsAsync(TNodeId nodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                if (_edges.TryGetValue(nodeId, out var neighbors))
                {
                    return neighbors.Keys.ToList();
                }

                return Enumerable.Empty<TNodeId>();
            }
        }

        public async ValueTask<IEnumerable<TNodeId>> DepthFirstSearchAsync(TNodeId startNodeId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            var visited = new HashSet<TNodeId>();
            var stack = new Stack<TNodeId>();
            var result = new List<TNodeId>();

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                if (_nodes.ContainsKey(startNodeId))
                {
                    stack.Push(startNodeId);
                }
            }

            while (stack.Count > 0)
            {
                var currentNodeId = stack.Pop();

                if (!visited.Contains(currentNodeId))
                {
                    visited.Add(currentNodeId);
                    result.Add(currentNodeId);

                    var neighbors = await GetNeighborsAsync(currentNodeId, cancellationToken);
                    foreach (var neighborId in neighbors)
                    {
                        if (!visited.Contains(neighborId))
                        {
                            stack.Push(neighborId);
                        }
                    }
                }
            }

            return result;
        }

        private void EnsureNotDisposed()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(AsyncGraphCollection<TNode, TNodeId>));
                }
            }
        }

        public async IAsyncEnumerator<KeyValuePair<TNode, TNodeId>> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            List<KeyValuePair<TNodeId, TNode>> nodes;

            await using (await _lock.AcquireAsync(cancellationToken: cancellationToken))
            {
                nodes = _nodes.ToList();
            }

            foreach (var node in nodes)
            {
                yield return new KeyValuePair<TNode, TNodeId>(node.Value, node.Key);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await using (await _lock.AcquireAsync())
            {
                if (_disposed) return;
                _disposed = true;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            await Task.CompletedTask;
        }

        internal async ValueTask<bool> ContainsNodeAsync(TNodeId nodeId)
        {
            await using (await _lock.AcquireAsync())
            {
                return _nodes.ContainsKey(nodeId);
            }
        }

        internal async ValueTask<TNode?> GetNodeAsync(TNodeId nodeId)
        {
            await using (await _lock.AcquireAsync())
            {
                _nodes.TryGetValue(nodeId, out var node);
                return node;
            }
        }

        internal async ValueTask<bool> ContainsEdgeAsync(TNodeId fromNodeId, TNodeId toNodeId)
        {
            await using (await _lock.AcquireAsync())
            {
                if (_edges.TryGetValue(fromNodeId, out var neighbors))
                {
                    return neighbors.ContainsKey(toNodeId);
                }

                return false;
            }
        }
    }
}
