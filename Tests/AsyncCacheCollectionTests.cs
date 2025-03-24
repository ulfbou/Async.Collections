// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

using FluentAssertions;

using Xunit;

namespace Async.Collections.Tests
{
    public class AsyncCacheCollectionTests
    {
        [Fact]
        public async Task GetOrAddAsync_ShouldReturnExistingValue_WhenKeyExists()
        {
            // Arrange
            var cache = new AsyncCacheCollection<string, int>(TimeSpan.FromMinutes(1));
            var key = "testKey";
            var value = 42;
            await cache.GetOrAddAsync(key, () => Task.FromResult(value));

            // Act
            var result = await cache.GetOrAddAsync(key, () => Task.FromResult(0));

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task GetOrAddAsync_ShouldAddAndReturnNewValue_WhenKeyDoesNotExist()
        {
            // Arrange
            var cache = new AsyncCacheCollection<string, int>(TimeSpan.FromMinutes(1));
            var key = "testKey";
            var value = 42;

            // Act
            var result = await cache.GetOrAddAsync(key, () => Task.FromResult(value));

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task TryRemove_ShouldRemoveItem_WhenKeyExists()
        {
            // Arrange
            var cache = new AsyncCacheCollection<string, int>(TimeSpan.FromMinutes(1));
            var key = "testKey";
            var value = 42;
            await cache.GetOrAddAsync(key, () => Task.FromResult(value));

            // Act
            var removed = cache.TryRemove(key);

            // Assert
            removed.Should().BeTrue();
            cache.TryRemove(key).Should().BeFalse();
        }

        [Fact]
        public void TryRemove_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var cache = new AsyncCacheCollection<string, int>(TimeSpan.FromMinutes(1));
            var key = "testKey";

            // Act
            var removed = cache.TryRemove(key);

            // Assert
            removed.Should().BeFalse();
        }

        [Fact]
        public async Task EvictExpiredItems_ShouldRemoveExpiredItems()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMilliseconds(100);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            var key = "testKey";
            var value = 42;
            await cache.GetOrAddAsync(key, () => Task.FromResult(value));

            // Act
            await Task.Delay(evictionInterval + TimeSpan.FromMilliseconds(100));
            cache.GetType().GetMethod("EvictExpiredItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(cache, new object[] { null! });

            // Assert
            cache.TryRemove(key).Should().BeFalse();
        }

        [Fact]
        public async Task Dispose_ShouldDisposeTimerAsync()
        {
            // Arrange
            var cache = new AsyncCacheCollection<string, int>(TimeSpan.FromMinutes(1));

            // Act
            await cache.DisposeAsync();

            // Assert
            var timerField = typeof(AsyncCacheCollection<string, int>).GetField("_evictionTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timer = (Timer)timerField!.GetValue(cache)!;
            timer.Should().BeNull();
        }
    }
}
