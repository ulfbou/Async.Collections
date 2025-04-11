// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncBatchProcessor
{
    public class AsyncBatchProcessorBenchmarkTests
    {
        //[Fact]
        //public async Task ProcessBatches_ShouldHandleLargeScaleOperations()
        //{
        //    // Arrange
        //    var batchSize = 100;
        //    var batchTimeWindow = TimeSpan.FromMilliseconds(500);
        //    var processBatchAsyncMock = new Mock<Func<List<int>, CancellationToken, ValueTask>>();
        //    var processor = new AsyncBatchProcessor<int>(batchSize, batchTimeWindow, processBatchAsyncMock.Object);

        //    var itemsToAdd = Enumerable.Range(0, 10000).ToList();
        //    var stopwatch = new Stopwatch();

        //    // Act
        //    stopwatch.Start();
        //    foreach (var item in itemsToAdd)
        //    {
        //        processor.Add(item);
        //    }

        //    await Task.Delay(batchTimeWindow + TimeSpan.FromMilliseconds(100));
        //    stopwatch.Stop();

        //    // Assert
        //    processBatchAsyncMock.Verify(p => p(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.AtLeast(100));
        //    stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        //}
    }
}
