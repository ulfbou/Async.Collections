// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using System.Collections.Concurrent;

using Xunit;

namespace Async.Collections.Tests.AsyncBatchProcessor
{
    public class AsyncBatchProcessorCoreTests
    {
        [Fact]
        public void AddItem_ShouldAddItemToQueue()
        {
            // Arrange
            var processor = new AsyncBatchProcessor<int>(5, TimeSpan.FromSeconds(1), async (batch, token) => { await Task.CompletedTask; });
            var item = 42;

            // Act
            processor.Add(item);

            // Assert
            // Use reflection to access the private _queue field for testing purposes
            var queueField = typeof(AsyncBatchProcessor<int>).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var queue = (ConcurrentQueue<int>)queueField!.GetValue(processor)!;
            queue.Should().Contain(item);
        }

        [Fact]
        public async Task ProcessBatches_ShouldProcessBatches_WhenBatchSizeIsReached()
        {
            // Arrange
            var batchSize = 3;
            var processedBatches = new List<List<int>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, TimeSpan.FromSeconds(1), async (batch, token) =>
            {
                processedBatches.Add(batch);
                await Task.CompletedTask;
            });

            // Act
            for (int i = 0; i < batchSize; i++)
            {
                processor.Add(i);
            }

            // Wait for the batch to be processed
            await Task.Delay(2000);

            // Assert
            processedBatches.Should().HaveCount(1);
            processedBatches[0].Should().BeEquivalentTo(new List<int> { 0, 1, 2 });
        }

        [Fact]
        public async Task ProcessBatches_ShouldProcessBatches_WhenTimeWindowIsReached()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processedBatches = new List<List<int>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, async (batch, token) =>
            {
                processedBatches.Add(batch);
                await Task.CompletedTask;
            });

            // Act
            processor.Add(1);
            processor.Add(2);

            // Wait for the batch to be processed
            await Task.Delay(2000);

            // Assert
            processedBatches.Should().HaveCount(1);
            processedBatches[0].Should().BeEquivalentTo(new List<int> { 1, 2 });
        }

        [Fact]
        public async Task Dispose_ShouldDisposeTimerAsync()
        {
            // Arrange
            var processor = new AsyncBatchProcessor<int>(5, TimeSpan.FromSeconds(1), async (batch, token) => { await Task.CompletedTask; });

            // Act
            await processor.DisposeAsync();

            // Assert
            // Use reflection to access the private _batchTimer field for testing purposes
            var timerField = typeof(AsyncBatchProcessor<int>).GetField("_batchTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timer = (Timer)timerField!.GetValue(processor)!;
            timer.Should().BeNull();
        }
    }
}
