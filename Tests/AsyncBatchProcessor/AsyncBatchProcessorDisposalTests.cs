// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncBatchProcessor
{
    public class AsyncBatchProcessorDisposalTests
    {
        [Fact]
        public async Task DisposeAsync_ShouldCancelBatchProcessingTask()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            await processor.DisposeAsync();

            // Assert
            var cancellationTokenSourceField = typeof(AsyncBatchProcessor<int>).GetField("_cancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cancellationTokenSource = (CancellationTokenSource)cancellationTokenSourceField!.GetValue(processor)!;
            cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_ShouldNotCallProcessBatchAsyncAfterDisposal()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            await processor.DisposeAsync();
            processor.Add(1);
            await Task.Delay(batchTimeWindow * 2); // Allow time for processing

            // Assert
            processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DisposeAsync_ShouldNotThrowWhenCalledMultipleTimes()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            Func<Task> act = async () =>
            {
                await processor.DisposeAsync();
                await processor.DisposeAsync();
            };

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Add_ShouldThrowObjectDisposedException_AfterDisposalAsync()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            await processor.DisposeAsync();

            // Assert
            processor.Invoking(p => p.Add(1)).Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task AddRange_ShouldThrowObjectDisposedException_AfterDisposalAsync()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            await processor.DisposeAsync();

            // Assert
            processor.Invoking(p => p.AddRange(new List<int> { 1, 2, 3 })).Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task FlushAsync_ShouldThrowObjectDisposedException_AfterDisposalAsync()
        {
            // Arrange
            var batchSize = 5;
            var batchTimeWindow = TimeSpan.FromSeconds(1);
            var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
            var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

            // Act
            await processor.DisposeAsync();

            // Assert
            await processor.Invoking(async p => await p.FlushAsync()).Should().ThrowAsync<ObjectDisposedException>();
        }
    }
}
