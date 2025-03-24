// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

using FluentAssertions;

using Xunit;

namespace Async.Collections.Tests
{
    public class AsyncSetTests
    {
        [Fact]
        public async Task AddAsync_ShouldAddItemToSet()
        {
            // Arrange
            var set = new AsyncSet<int>();
            var item = 42;

            // Act
            var result = await set.AddAsync(item);

            // Assert
            result.Should().BeTrue();
            var setField = typeof(AsyncSet<int>).GetField("_set", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var internalSet = (ConcurrentDictionary<int, byte>)setField!.GetValue(set)!;
            internalSet.Should().ContainKey(item);
        }

        [Fact]
        public async Task AddAsync_ShouldReturnFalse_WhenItemAlreadyExists()
        {
            // Arrange
            var set = new AsyncSet<int>();
            var item = 42;
            await set.AddAsync(item);

            // Act
            var result = await set.AddAsync(item);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveItemFromSet()
        {
            // Arrange
            var set = new AsyncSet<int>();
            var item = 42;
            await set.AddAsync(item);

            // Act
            var result = await set.RemoveAsync(item);

            // Assert
            result.Should().BeTrue();
            var setField = typeof(AsyncSet<int>).GetField("_set", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var internalSet = (ConcurrentDictionary<int, byte>)setField!.GetValue(set)!;
            internalSet.Should().NotContainKey(item);
        }

        [Fact]
        public async Task RemoveAsync_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            // Arrange
            var set = new AsyncSet<int>();
            var item = 42;

            // Act
            var result = await set.RemoveAsync(item);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ContainsAsync_ShouldReturnTrue_WhenItemExists()
        {
            // Arrange
            var set = new AsyncSet<int>();
            var item = 42;
            await set.AddAsync(item);

            // Act
            var result = await set.ContainsAsync(item);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ContainsAsync_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            // Arrange
            var set = new AsyncSet<int>();
            var item = 42;

            // Act
            var result = await set.ContainsAsync(item);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Count_ShouldReturnNumberOfItemsInSetAsync()
        {
            // Arrange
            var set = new AsyncSet<int>();
            var items = new[] { 1, 2, 3 };

            foreach (var item in items)
            {
                await set.AddAsync(item);
            }

            // Act
            var count = set.Count;

            // Assert
            count.Should().Be(items.Length);
        }
    }
}
