// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Async.Collections;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Async.Collections.Tests
{
    namespace Async.Collections.Tests
    {
        public class AsyncDisposableCollectionTests
        {
            private readonly Mock<ILogger<AsyncDisposableCollection<IDisposable>>> _loggerMock;
            private readonly AsyncDisposableCollection<IDisposable> _collection;
            private readonly Mock<IDisposable> _disposableMock;

            public AsyncDisposableCollectionTests()
            {
                _loggerMock = new Mock<ILogger<AsyncDisposableCollection<IDisposable>>>();
                _collection = new AsyncDisposableCollection<IDisposable>(_loggerMock.Object);
                _disposableMock = new Mock<IDisposable>();
            }

            [Fact]
            public void Add_ShouldAddItemAsync()
            {
                // Act
                _collection.Add(_disposableMock.Object);

                // Assert
                _collection.ToListAsync().ToBlockingEnumerable().Should().Contain(_disposableMock.Object);
            }

            [Fact]
            public void Remove_ShouldRemoveItem()
            {
                // Arrange
                _collection.Add(_disposableMock.Object);

                // Act
                var removed = _collection.Remove(_disposableMock.Object);

                // Assert
                removed.Should().BeTrue();
                _collection.ToListAsync().ToBlockingEnumerable().Should().NotContain(_disposableMock.Object);
            }

            [Fact]
            public async Task DisposeAsync_ShouldDisposeAllItems()
            {
                // Arrange
                _collection.Add(_disposableMock.Object);

                // Act
                await _collection.DisposeAsync();

                // Assert
                _disposableMock.Verify(d => d.Dispose(), Times.Once);
            }

            [Fact]
            public async Task DisposeAsync_ShouldLogError_WhenExceptionOccurs()
            {
                // Arrange
                _disposableMock.Setup(d => d.Dispose()).Throws(new Exception("Dispose error"));
                _collection.Add(_disposableMock.Object);

                // Act
                await _collection.DisposeAsync();

                // Assert
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => true),
                        It.IsAny<Exception>(),
                        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                    Times.Once);
            }
        }
    }
}
