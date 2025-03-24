using FluentAssertions;

using Xunit;

namespace Async.Collections.Tests
{
    public class AsyncBagTests
    {
        [Fact]
        public async Task AddAsync_ShouldAddSingleItem()
        {
            // Arrange
            var bag = new AsyncBag<int>();

            // Act
            await bag.AddAsync(1);

            // Assert
            var list = bag.ToListAsync().ToBlockingEnumerable();
            list.Should().ContainSingle().Which.Should().Be(1);
        }

        [Fact]
        public async Task AddAsync_ShouldAddMultipleItemsConcurrently()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var tasks = Enumerable.Range(1, 100).Select(i => bag.AddAsync(i).AsTask()).ToArray();

            // Act
            await Task.WhenAll(tasks);

            // Assert
            var list = bag.ToListAsync().ToBlockingEnumerable();
            list.Should().HaveCount(100);
        }

        [Fact]
        public async Task TryTakeAsync_ShouldRemoveItemWhenNotEmpty()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            await bag.AddAsync(1);

            // Act
            var (success, item) = await bag.TryTakeAsync();

            // Assert
            success.Should().BeTrue();
            item.Should().Be(1);
        }

        [Fact]
        public async Task TryTakeAsync_ShouldReturnFalseWhenEmpty()
        {
            // Arrange
            var bag = new AsyncBag<int>();

            // Act
            var (success, item) = await bag.TryTakeAsync();

            // Assert
            success.Should().BeFalse();
            item.Should().Be(default);
        }

        [Fact]
        public async Task ToListAsync_ShouldConvertToListAfterAddingItems()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            await bag.AddAsync(1);
            await bag.AddAsync(2);

            // Act
            var list = bag.ToListAsync().ToBlockingEnumerable();

            // Assert
            list.Should().Contain([1, 2]);
        }

        [Fact]
        public void ToListAsync_ShouldConvertEmptyBagToList()
        {
            // Arrange
            var bag = new AsyncBag<int>();

            // Act
            var list = bag.ToListAsync().ToBlockingEnumerable();

            // Assert
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task Count_ShouldBeValidAfterAddsAndRemoves()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            await bag.AddAsync(1);
            await bag.AddAsync(2);
            await bag.TryTakeAsync();

            // Act
            var count = bag.Count;

            // Assert
            count.Should().Be(1);
        }

        [Fact]
        public async Task Count_ShouldBeConsistentDuringConcurrentModifications()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var addTasks = Enumerable.Range(1, 100).Select(i => bag.AddAsync(i).AsTask()).ToArray();
            var removeTasks = Enumerable.Range(1, 50).Select(_ => bag.TryTakeAsync().AsTask()).ToArray();

            // Act
            await Task.WhenAll(addTasks);
            await Task.WhenAll(removeTasks);

            // Assert
            var count = bag.Count;
            count.Should().Be(50);
        }

        [Fact]
        public async Task ConcurrentAddAndRemove_ShouldWorkCorrectly()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var addTasks = Enumerable.Range(1, 100).Select(i => bag.AddAsync(i).AsTask()).ToArray();
            var removeTasks = Enumerable.Range(1, 50).Select(_ => bag.TryTakeAsync().AsTask()).ToArray();

            // Act
            await Task.WhenAll(addTasks);
            await Task.WhenAll(removeTasks);

            // Assert
            var remainingItems = bag.ToListAsync().ToBlockingEnumerable();
            remainingItems.Should().HaveCount(50);
        }

        [Fact]
        public async Task ToListAsync_ShouldBeThreadSafe()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var addTasks = Enumerable.Range(1, 100).Select(i => bag.AddAsync(i).AsTask()).ToArray();
            var removeTasks = Enumerable.Range(1, 50).Select(_ => bag.TryTakeAsync().AsTask()).ToArray();

            // Act
            await Task.WhenAll(addTasks);
            await Task.WhenAll(removeTasks);
            var list = bag.ToListAsync().ToBlockingEnumerable();

            // Assert
            list.Should().HaveCount(100);
        }

        [Fact]
        public async Task RemoveFromEmptyBag_ShouldReturnFalse()
        {
            // Arrange
            var bag = new AsyncBag<int>();

            // Act
            var (success, item) = await bag.TryTakeAsync();

            // Assert
            success.Should().BeFalse();
            item.Should().Be(default);
        }

        [Fact]
        public void ToListAsyncOnEmptyBag_ShouldReturnEmptyList()
        {
            // Arrange
            var bag = new AsyncBag<int>();

            // Act
            var list = bag.ToListAsync().ToBlockingEnumerable();

            // Assert
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task LargeData_ShouldHandleLargeNumberOfItems()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var addTasks = Enumerable.Range(1, 10000).Select(i => bag.AddAsync(i).AsTask()).ToArray();

            // Act
            await Task.WhenAll(addTasks);
            var list = bag.ToListAsync().ToBlockingEnumerable();

            // Assert
            list.Should().HaveCount(10000);
        }

        [Fact]
        public async Task Dispose_ShouldPreventFurtherOperations()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            await bag.AddAsync(1);
            await bag.DisposeAsync();

            // Act
            var addAct = async () => await bag.AddAsync(2);
            var takeAct = async () => await bag.TryTakeAsync();
            var listAct = () => bag.ToListAsync();

            // Assert
            await addAct.Should().ThrowAsync<ObjectDisposedException>();
            await takeAct.Should().ThrowAsync<ObjectDisposedException>();
            listAct.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task AddAsync_ShouldAddItem()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var item = 42;

            // Act
            await bag.AddAsync(item);

            // Assert
            var items = bag.ToListAsync().ToBlockingEnumerable();
            items.Should().Contain(item);
        }

        [Fact]
        public async Task TryTakeAsync_ShouldReturnTrueAndItem_WhenItemExists()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var item = 42;
            await bag.AddAsync(item);

            // Act
            var (result, takenItem) = await bag.TryTakeAsync();

            // Assert
            result.Should().BeTrue();
            takenItem.Should().Be(item);
        }

        [Fact]
        public async Task TryTakeAsync_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            // Arrange
            var bag = new AsyncBag<int>();

            // Act
            var (result, takenItem) = await bag.TryTakeAsync();

            // Assert
            result.Should().BeFalse();
            takenItem.Should().Be(default(int));
        }

        [Fact]
        public async Task ToListAsync_ShouldReturnAllItems()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var items = new[] { 1, 2, 3 };

            foreach (var item in items)
            {
                await bag.AddAsync(item);
            }

            // Act
            var result = bag.ToListAsync().ToBlockingEnumerable();

            // Assert
            result.Should().BeEquivalentTo(items);
        }

        [Fact]
        public async Task Count_ShouldReturnNumberOfItemsAsync()
        {
            // Arrange
            var bag = new AsyncBag<int>();
            var items = new[] { 1, 2, 3 };
            foreach (var item in items)
            {
                await bag.AddAsync(item);
            }

            // Act
            var count = bag.Count;

            // Assert
            count.Should().Be(items.Length);
        }
    }
}
