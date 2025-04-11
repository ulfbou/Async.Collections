// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncBatchProcessor
{
    public class AsyncBatchProcessorConcurrencyTests
    {
        [Fact]
        public async Task Add_ShouldHandleConcurrentAdds()
        {
            // Arrange
            var batchSize = 10;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            var itemsToAdd = Enumerable.Range(0, 100).ToList();
            var tasks = new List<Task>();

            // Act
            foreach (var item in itemsToAdd)
            {
                tasks.Add(Task.Run(() => processor.Add(item)));
            }

            await Task.WhenAll(tasks);

            // Assert
            var queueField = typeof(AsyncBatchProcessor<int>).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var queue = (ConcurrentQueue<int>)queueField!.GetValue(processor)!;
            queue.Should().BeEquivalentTo(itemsToAdd);
        }

        [Fact]
        public async Task AddRange_ShouldHandleConcurrentAddRanges()
        {
            // Arrange
            var batchSize = 10;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            var itemsToAdd = Enumerable.Range(0, 100).ToList();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                var range = itemsToAdd.Skip(i * 10).Take(10).ToList();
                tasks.Add(Task.Run(() => processor.AddRange(range)));
            }

            await Task.WhenAll(tasks);

            // Assert
            var queueField = typeof(AsyncBatchProcessor<int>).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var queue = (ConcurrentQueue<int>)queueField!.GetValue(processor)!;
            queue.Should().BeEquivalentTo(itemsToAdd);
        }

        [Fact]
        public async Task FlushAsync_ShouldHandleConcurrentFlushAndAdd()
        {
            // Arrange
            var batchSize = 10;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            var itemsToAdd = Enumerable.Range(0, 100).ToList();
            var addTasks = new List<Task>();

            // Act
            foreach (var item in itemsToAdd)
            {
                addTasks.Add(Task.Run(() => processor.Add(item)));
            }

            var flushTask = Task.Run(() => processor.FlushAsync());

            await Task.WhenAll(addTasks);
            await flushTask;

            // Assert
            var queueField = typeof(AsyncBatchProcessor<int>).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var queue = (ConcurrentQueue<int>)queueField!.GetValue(processor)!;
            queue.Should().BeEmpty();
        }
    }
}
