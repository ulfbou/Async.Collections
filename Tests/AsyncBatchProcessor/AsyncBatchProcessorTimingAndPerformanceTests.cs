// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncBatchProcessor
{
    public class AsyncBatchProcessorTimingAndPerformanceTests
    {
        [Fact]
        public async Task ProcessBatches_ShouldProcessBatch_AfterTimeWindowExpires()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromMilliseconds(500);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            processor.Add(1);
            processor.Add(2);

            // Act
            await Task.Delay(batchTimeWindow + TimeSpan.FromMilliseconds(100)); // Allow time for processing

            // Assert
            processBatchAsyncMock.Verify(p => p(It.Is<List<int>>(batch => batch.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessBatches_ShouldHandleLargeBatch()
        {
            // Arrange
            var batchSize = 10;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            var itemsToAdd = Enumerable.Range(0, 100).ToList();

            // Act
            foreach (var item in itemsToAdd)
            {
                processor.Add(item);
            }

            await Task.Delay(batchTimeWindow + TimeSpan.FromMilliseconds(100)); // Allow time for processing

            // Assert
            processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
        }

        [Fact]
        public async Task ProcessBatches_ShouldHandleHighConcurrency()
        {
            // Arrange
            var batchSize = 10;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            var itemsToAdd = Enumerable.Range(0, 1000).ToList();
            var tasks = new List<Task>();

            // Act
            foreach (var item in itemsToAdd)
            {
                tasks.Add(Task.Run(() => processor.Add(item)));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(batchTimeWindow + TimeSpan.FromMilliseconds(100)); // Allow time for processing

            // Assert
            processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.AtLeast(100));
        }
    }
}
