// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Moq;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    public class AsyncCacheCollectionDisposalTests
    {
        [Fact]
        public async Task DisposeAsync_ShouldCancelEvictionTask()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromSeconds(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Act
            await cache.DisposeAsync();

            // Assert
            var cancellationSourceField = typeof(AsyncCacheCollection<string, int>).GetField("_cancellationTokenSource",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cancellationSource = (CancellationTokenSource)cancellationSourceField!.GetValue(cache)!;

            // Verify the cancellation token is requested
            cancellationSource.IsCancellationRequested.Should().BeTrue();

            // Verify the disposed flag is set
            var disposedField = typeof(AsyncCacheCollection<string, int>).GetField("_disposed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var disposed = (bool)disposedField!.GetValue(cache)!;
            disposed.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_ShouldPreventFurtherGetOrAddAsync()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromSeconds(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            await cache.DisposeAsync();

            // Act & Assert
            Func<Task> act = async () => await cache.GetOrAddAsync("key", () => ValueTask.FromResult(42));
            await act.Should().ThrowAsync<ObjectDisposedException>()
                .WithMessage($"*{nameof(AsyncCacheCollection<string, int>)}*");
        }

        [Fact]
        public async Task DisposeAsync_ShouldPreventFurtherTryRemove()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromSeconds(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            await cache.DisposeAsync();

            // Act & Assert
            Action act = () => cache.TryRemove("key");
            act.Should().Throw<ObjectDisposedException>()
                .WithMessage($"*{nameof(AsyncCacheCollection<string, int>)}*");
        }

        [Fact]
        public async Task DisposeAsync_ShouldPreventFurtherEnumeration()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromSeconds(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            await cache.DisposeAsync();

            // Act & Assert
            Func<Task> act = async () =>
            {
                await foreach (var item in cache)
                {
                    // This should throw before entering the loop
                }
            };

            await act.Should().ThrowAsync<ObjectDisposedException>()
                .WithMessage($"*{nameof(AsyncCacheCollection<string, int>)}*");
        }

        [Fact]
        public async Task DisposeAsync_ShouldBeIdempotent()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromSeconds(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Act
            await cache.DisposeAsync();
            Func<Task> secondDispose = async () => await cache.DisposeAsync();

            // Assert
            await secondDispose.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DisposeAsync_ShouldHandleExceptions()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromSeconds(1);
            var onErrorMock = new Mock<Func<Exception, Task>>();
            var cache = new AsyncCacheCollection<string, int>(evictionInterval, null, onErrorMock.Object);

            // Act
            await cache.DisposeAsync();

            // Assert
            // Just verifying it doesn't throw
        }
    }
}
