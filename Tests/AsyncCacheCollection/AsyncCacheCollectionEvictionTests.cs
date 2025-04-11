// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    public class AsyncCacheCollectionEvictionTests
    {
        [Fact]
        public async Task ItemsOlderThanEvictionInterval_ShouldBeRemoved()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMilliseconds(500);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            await cache.GetOrAddAsync("key1", () => ValueTask.FromResult(42));
            await cache.GetOrAddAsync("key2", () => ValueTask.FromResult(43));

            // Act
            await Task.Delay(evictionInterval + TimeSpan.FromMilliseconds(100)); // Wait for eviction interval to pass

            // Assert
            var items = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            items.Should().BeEmpty();
        }

        [Fact]
        public async Task Eviction_ShouldSkipFreshlyAccessedItems()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMilliseconds(500);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            await cache.GetOrAddAsync("key1", () => ValueTask.FromResult(42));
            await Task.Delay(evictionInterval / 2);
            await cache.GetOrAddAsync("key1", () => ValueTask.FromResult(42)); // Access the item to refresh its timestamp

            // Act
            await Task.Delay(evictionInterval / 2 + TimeSpan.FromMilliseconds(100)); // Wait for eviction interval to pass

            // Assert
            var items = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            items.Should().ContainSingle(kv => kv.Key == "key1" && kv.Value == 42);
        }

        [Fact]
        public async Task EvictItemsBeyondMaxSize_ShouldRemoveLeastRecentlyAccessedItems()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var maxSize = 3;
            var cache = new AsyncCacheCollection<string, int>(evictionInterval, maxSize);

            await cache.GetOrAddAsync("key1", () => ValueTask.FromResult(42));
            await cache.GetOrAddAsync("key2", () => ValueTask.FromResult(43));
            await cache.GetOrAddAsync("key3", () => ValueTask.FromResult(44));
            await cache.GetOrAddAsync("key4", () => ValueTask.FromResult(45)); // This should trigger eviction

            // Act
            var items = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            // Assert
            items.Should().HaveCount(maxSize);
            items.Should().NotContain(kv => kv.Key == "key1" && kv.Value == 42); // Least recently accessed item should be evicted
        }
    }
}
