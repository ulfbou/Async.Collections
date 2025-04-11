// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Moq;

using System.Collections.Concurrent;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    public class AsyncCacheCollectionCoreTests
    {
        [Fact]
        public async Task GetOrAddAsync_ShouldAddNewItem_WhenKeyDoesNotExist()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var valueFactoryMock = new Mock<Func<ValueTask<int>>>();
            valueFactoryMock.Setup(f => f()).ReturnsAsync(42);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Act
            var value = await cache.GetOrAddAsync("key1", valueFactoryMock.Object);

            // Assert
            value.Should().Be(42);
            valueFactoryMock.Verify(f => f(), Times.Once);
        }

        [Fact]
        public async Task GetOrAddAsync_ShouldRetrieveExistingItem_WithoutInvokingFactory()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var valueFactoryMock = new Mock<Func<ValueTask<int>>>();
            valueFactoryMock.Setup(f => f()).ReturnsAsync(42);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Act
            await cache.GetOrAddAsync("key1", valueFactoryMock.Object);
            var value = await cache.GetOrAddAsync("key1", valueFactoryMock.Object);

            // Assert
            value.Should().Be(42);
            valueFactoryMock.Verify(f => f(), Times.Once);
        }

        [Fact]
        public async Task GetOrAddAsync_ShouldUpdateLastAccessedTime()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var valueFactoryMock = new Mock<Func<ValueTask<int>>>();
            valueFactoryMock.Setup(f => f()).ReturnsAsync(42);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Act
            await cache.GetOrAddAsync("key1", valueFactoryMock.Object);
            var initialAccessTime = DateTime.UtcNow;
            await Task.Delay(100);
            await cache.GetOrAddAsync("key1", valueFactoryMock.Object);
            var updatedAccessTime = DateTime.UtcNow;

            // Assert
            var cacheField = typeof(AsyncCacheCollection<string, int>).GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cacheDict = (ConcurrentDictionary<string, AsyncCacheCollection<string, int>.CacheItem<int>>)cacheField!.GetValue(cache)!;
            cacheDict["key1"].LastAccessed.Should().BeAfter(initialAccessTime).And.BeBefore(updatedAccessTime);
        }

        [Fact]
        public async Task TryRemove_ShouldSuccessfullyRemoveItemAsync()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            await cache.GetOrAddAsync("key1", () => new ValueTask<int>(42));

            // Act
            var result = cache.TryRemove("key1");

            // Assert
            result.Should().BeTrue();
            cache.TryRemove("key1").Should().BeFalse();
        }

        [Fact]
        public void TryRemove_ShouldFailForNonExistentItem()
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
        public async Task GetAsyncEnumerator_ShouldYieldAllKeyValuePairs()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            await cache.GetOrAddAsync("key1", () => ValueTask.FromResult(42));
            await cache.GetOrAddAsync("key2", () => ValueTask.FromResult(43));

            // Act
            var items = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            // Assert
            items.Should().HaveCount(2);
            items.Should().Contain(new KeyValuePair<string, int>("key1", 42));
            items.Should().Contain(new KeyValuePair<string, int>("key2", 43));
        }

        [Fact]
        public async Task GetAsyncEnumerator_ShouldReflectCacheState()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            await cache.GetOrAddAsync("key1", () => ValueTask.FromResult(42));

            // Act
            var itemsBefore = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                itemsBefore.Add(item);
            }

            await cache.GetOrAddAsync("key2", () => ValueTask.FromResult(43));
            cache.TryRemove("key1");

            var itemsAfter = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                itemsAfter.Add(item);
            }

            // Assert
            itemsBefore.Should().HaveCount(1);
            itemsBefore.Should().Contain(new KeyValuePair<string, int>("key1", 42));

            itemsAfter.Should().HaveCount(1);
            itemsAfter.Should().Contain(new KeyValuePair<string, int>("key2", 43));
        }
    }
}
