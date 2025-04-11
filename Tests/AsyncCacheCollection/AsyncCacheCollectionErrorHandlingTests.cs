// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    public class AsyncCacheCollectionErrorHandlingTests
    {
        [Fact]
        public async Task GetOrAddAsync_ShouldNotCorruptCache_WhenFactoryThrowsException()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            var valueFactoryMock = new Mock<Func<ValueTask<int>>>();
            valueFactoryMock.Setup(f => f()).ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            Func<Task> act = async () => await cache.GetOrAddAsync("key1", valueFactoryMock.Object);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");

            var items = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            items.Should().BeEmpty();
        }

        [Fact]
        public async Task EvictionTask_ShouldHandleExceptionsGracefully()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMilliseconds(100);
            var onErrorMock = new Mock<Func<Exception, Task>>();
            var cache = new AsyncCacheCollection<string, int>(evictionInterval, null, onErrorMock.Object);

            // Add an item to trigger eviction
            await cache.GetOrAddAsync("key1", () => ValueTask.FromResult(42));

            // Act
            // Simulate an exception in the eviction task by accessing a private method
            var evictionTaskField = typeof(AsyncCacheCollection<string, int>).GetField("_evictionTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var evictionTask = (Task)evictionTaskField!.GetValue(cache)!;

            // Wait for the eviction task to run
            await Task.Delay(evictionInterval * 2);

            // Assert
            onErrorMock.Verify(e => e(It.IsAny<Exception>()), Times.AtLeastOnce);
        }
    }
}
