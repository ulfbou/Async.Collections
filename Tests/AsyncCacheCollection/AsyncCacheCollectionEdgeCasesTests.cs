// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    public class AsyncCacheCollectionEdgeCasesTests
    {
        [Fact]
        public async Task GetOrAddAsync_ShouldAddItems_WhenCacheIsEmpty()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            var valueFactoryMock = new Mock<Func<ValueTask<int>>>();
            valueFactoryMock.Setup(f => f()).ReturnsAsync(42);

            // Act
            var value = await cache.GetOrAddAsync("key1", valueFactoryMock.Object);

            // Assert
            value.Should().Be(42);
            valueFactoryMock.Verify(f => f(), Times.Once);

            var items = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            items.Should().ContainSingle(kv => kv.Key == "key1" && kv.Value == 42);
        }

        [Fact]
        public void TryRemove_ShouldReturnFalse_WhenCacheIsEmpty()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Act
            var result = cache.TryRemove("key1");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrAddAsync_ShouldStoreAndRetrieveNullValues_WhenTValueIsNullable()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, string?>(evictionInterval);
            var valueFactoryMock = new Mock<Func<ValueTask<string?>>>();
            valueFactoryMock.Setup(f => f()).ReturnsAsync((string?)null);

            // Act
            var value = await cache.GetOrAddAsync("key1", valueFactoryMock.Object);

            // Assert
            value.Should().BeNull();
            valueFactoryMock.Verify(f => f(), Times.Once);

            var items = new List<KeyValuePair<string, string?>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            items.Should().ContainSingle(kv => kv.Key == "key1" && kv.Value == null);
        }

        [Fact]
        public async Task GetOrAddAsync_ShouldThrowException_WhenKeyIsNull()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            var valueFactoryMock = new Mock<Func<ValueTask<int>>>();
            valueFactoryMock.Setup(f => f()).ReturnsAsync(42);

            // Act
            Func<Task> act = async () => await cache.GetOrAddAsync(null!, valueFactoryMock.Object);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
