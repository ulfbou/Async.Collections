// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    public class AsyncCacheCollectionObservabilityTests
    {
        [Fact]
        public async Task OnErrorAsync_ShouldBeInvoked_ForEvictionErrors()
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
