// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using System.Collections.Concurrent;

using Xunit;

namespace Async.Collections.Tests
{
    public class AsyncGraphCollectionTests
    {
        [Fact]
        public async Task AddNodeAsync_ShouldAddNode()
        {
            // Arrange
            var graph = new AsyncGraphCollection<int, int>();
            var nodeId = 1;
            var node = 42;

            // Act
            await graph.AddNodeAsync(nodeId, node);

            // Assert
            var retrievedNode = await graph.GetNodeAsync(nodeId);
            retrievedNode.Should().Be(node);
        }

        [Fact]
        public async Task AddEdgeAsync_ShouldAddEdge()
        {
            // Arrange
            var graph = new AsyncGraphCollection<int, int>();
            var fromNodeId = 1;
            var toNodeId = 2;
            await graph.AddNodeAsync(fromNodeId, 42);
            await graph.AddNodeAsync(toNodeId, 43);

            // Act
            await graph.AddEdgeAsync(fromNodeId, toNodeId);

            // Assert
            (await graph.ContainsEdgeAsync(fromNodeId, toNodeId)).Should().BeTrue();
        }

        [Fact]
        public async Task RemoveNodeAsync_ShouldRemoveNodeAndEdges()
        {
            // Arrange
            var graph = new AsyncGraphCollection<int, int>();
            var nodeId = 1;
            await graph.AddNodeAsync(nodeId, 42);
            await graph.AddEdgeAsync(nodeId, 2);

            // Act
            var removed = await graph.RemoveNodeAsync(nodeId);

            // Assert
            removed.Should().BeTrue();
            (await graph.ContainsNodeAsync(nodeId)).Should().BeFalse();
            (await graph.ContainsEdgeAsync(nodeId, 2)).Should().BeFalse();
        }

        [Fact]
        public async Task RemoveEdgeAsync_ShouldRemoveEdge()
        {
            // Arrange
            var graph = new AsyncGraphCollection<int, int>();
            var fromNodeId = 1;
            var toNodeId = 2;
            await graph.AddNodeAsync(fromNodeId, 42);
            await graph.AddNodeAsync(toNodeId, 43);
            await graph.AddEdgeAsync(fromNodeId, toNodeId);

            // Act
            var removed = await graph.RemoveEdgeAsync(fromNodeId, toNodeId);

            // Assert
            removed.Should().BeTrue();
            (await graph.ContainsEdgeAsync(fromNodeId, toNodeId)).Should().BeFalse();
        }

        [Fact]
        public async Task GetNeighborsAsync_ShouldReturnNeighbors()
        {
            // Arrange
            var graph = new AsyncGraphCollection<int, int>();
            var nodeId = 1;
            var neighborId = 2;
            await graph.AddNodeAsync(nodeId, 42);
            await graph.AddNodeAsync(neighborId, 43);
            await graph.AddEdgeAsync(nodeId, neighborId);

            // Act
            var neighbors = await graph.GetNeighborsAsync(nodeId);

            // Assert
            neighbors.Should().Contain(neighborId);
        }

        [Fact]
        public async Task DepthFirstSearchAsync_ShouldReturnNodesInDepthFirstOrder()
        {
            // Arrange
            var graph = new AsyncGraphCollection<int, int>();
            await graph.AddNodeAsync(1, 42);
            await graph.AddNodeAsync(2, 43);
            await graph.AddNodeAsync(3, 44);
            await graph.AddEdgeAsync(1, 2);
            await graph.AddEdgeAsync(2, 3);

            // Act
            var result = await graph.DepthFirstSearchAsync(1);

            // Assert
            result.Should().ContainInOrder(new[] { 1, 2, 3 });
        }
    }
}
