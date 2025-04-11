// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncBatchProcessor
{
    public class AsyncBatchProcessorEdgeCasesTests
    {
        [Fact]
        public async Task ProcessBatchAsync_ShouldNotBeCalled_WhenNoItemsAdded()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            await Task.Delay(batchTimeWindow * 2); // Allow time for processing

            // Assert
            processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FlushAsync_ShouldNotThrow_WhenQueueIsEmpty()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            Func<Task> act = async () => await processor.FlushAsync();

            // Assert
            await act.Should().NotThrowAsync();
            processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DisposeAsync_ShouldProcessPartialBatch()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            processor.Add(1);
            processor.Add(2);

            // Act
            await processor.DisposeAsync();

            // Assert
            processBatchAsyncMock.Verify(p => p(It.Is<List<int>>(batch => batch.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
