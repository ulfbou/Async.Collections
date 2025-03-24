// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Xunit;

using System.Collections.Concurrent;

namespace Async.Collections.Tests
{
    public class AsyncQueueTests
    {
        [Fact]
        public async Task EnqueueAsync_ShouldAddItemToQueue()
        {
            // Arrange
            var queue = new AsyncQueue<int>();
            var item = 42;

            // Act
            await queue.EnqueueAsync(item);

            // Assert
            var queueField = typeof(AsyncQueue<int>).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var internalQueue = (ConcurrentQueue<int>)queueField!.GetValue(queue)!;
            internalQueue.Should().Contain(item);
        }

        [Fact]
        public async Task DequeueAsync_ShouldReturnItemFromQueue()
        {
            // Arrange
            var queue = new AsyncQueue<int>();
            var item = 42;
            await queue.EnqueueAsync(item);

            // Act
            var result = await queue.DequeueAsync();

            // Assert
            result.Should().Be(item);
        }

        [Fact]
        public async Task DequeueAsync_ShouldThrowInvalidOperationException_WhenQueueIsEmpty()
        {
            // Arrange
            var queue = new AsyncQueue<int>();

            // Act
            Func<Task> act = async () => await queue.DequeueAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Queue is empty after semaphore release.");
        }

        [Fact]
        public async Task TryPeekAsync_ShouldReturnTrueAndItem_WhenItemExists()
        {
            // Arrange
            var queue = new AsyncQueue<int>();
            var item = 42;
            await queue.EnqueueAsync(item);

            // Act
            var (success, result) = await queue.TryPeekAsync();

            // Assert
            success.Should().BeTrue();
            result.Should().Be(item);
        }

        [Fact]
        public async Task TryPeekAsync_ShouldReturnFalse_WhenQueueIsEmpty()
        {
            // Arrange
            var queue = new AsyncQueue<int>();

            // Act
            var (success, result) = await queue.TryPeekAsync();

            // Assert
            success.Should().BeFalse();
            result.Should().Be(default(int));
        }

        [Fact]
        public async Task Count_ShouldReturnNumberOfItemsInQueue()
        {
            // Arrange
            var queue = new AsyncQueue<int>();
            var items = new[] { 1, 2, 3 };
            foreach (var item in items)
            {
                await queue.EnqueueAsync(item);
            }

            // Act
            var count = queue.Count;

            // Assert
            count.Should().Be(items.Length);
        }
    }
}
