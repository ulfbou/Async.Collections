// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncBatchProcessor
{
    public class AsyncBatchProcessorErrorHandlingTests
    {
        [Fact]
        public async Task ProcessBatchAsync_ShouldInvokeOnErrorAsync_WhenExceptionOccurs()
        {
            // Arrange
            var batchSize = 2;
            var batchTimeWindow = TimeSpan.FromMilliseconds(100);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var onErrorAsyncMock = new Mock<Func<Exception, List<int>, Task>>();

            processBatchAsyncMock
                .Setup(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            var processor = new AsyncBatchProcessor<int>(
                batchSize,
                batchTimeWindow,
                processBatchAsyncMock.Object,
                onErrorAsyncMock.Object);

            // Act
            processor.Add(1);
            processor.Add(2);
            await Task.Delay(batchTimeWindow * 2); // Allow time for processing

            // Assert
            onErrorAsyncMock.Verify(o => o(It.IsAny<Exception>(), It.IsAny<List<int>>()), Times.Once);
        }

        [Fact]
        public async Task ProcessBatchAsync_ShouldContinueProcessingAfterFailure()
        {
            // Arrange
            var batchSize = 2;
            var batchTimeWindow = TimeSpan.FromMilliseconds(100);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var onErrorAsyncMock = new Mock<Func<Exception, List<int>, Task>>();

            processBatchAsyncMock
                .SetupSequence(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"))
                .Returns(new ValueTask());

            var processor = new AsyncBatchProcessor<int>(
                batchSize,
                batchTimeWindow,
                processBatchAsyncMock.Object,
                onErrorAsyncMock.Object);

            // Act
            processor.Add(1);
            processor.Add(2);
            await Task.Delay(batchTimeWindow * 2); // Allow time for processing
            processor.Add(3);
            processor.Add(4);
            await Task.Delay(batchTimeWindow * 2); // Allow time for processing

            // Assert
            processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessBatchAsync_ShouldSwallowExceptions_WhenNoOnErrorAsyncSpecified()
        {
            // Arrange
            var batchSize = 2;
            var batchTimeWindow = TimeSpan.FromMilliseconds(100);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();

            processBatchAsyncMock
                .Setup(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            var processor = new AsyncBatchProcessor<int>(
                batchSize,
                batchTimeWindow,
                processBatchAsyncMock.Object);

            // Act
            processor.Add(1);
            processor.Add(2);
            await Task.Delay(batchTimeWindow * 2); // Allow time for processing

            // Assert
            processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Add_ShouldNotLeaveProcessorInInconsistentState_WhenExceptionOccurs()
        {
            // Arrange
            var batchSize = 2;
            var batchTimeWindow = TimeSpan.FromMilliseconds(100);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var onErrorAsyncMock = new Mock<Func<Exception, List<int>, Task>>();

            var processor = new AsyncBatchProcessor<int>(
                batchSize,
                batchTimeWindow,
                processBatchAsyncMock.Object,
                onErrorAsyncMock.Object);

            // Act & Assert
            processor.Invoking(p => p.Add(default!)).Should().NotThrow();
            processor.Invoking(p => p.AddRange(new List<int> { 1, 2 })).Should().NotThrow();
        }
    }
}
