// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    /// <summary>
    /// Represents an asynchronous collection of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public interface IAsyncCollection<T> : IAsyncEnumerable<T>, IAsyncDisposable
    {
        /// <summary>
        /// Asynchronously adds an item to the collection.
        /// </summary>
        /// <param name="item">The item to add to the collection.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        ValueTask AddAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes an item from the collection.
        /// </summary>
        /// <param name="item">The item to remove from the collection.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the item was successfully removed.</returns>
        ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the number of elements in the collection.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of elements in the collection.</returns>
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The item to locate in the collection.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the item is found in the collection.</returns>
        ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously clears all items from the collection.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        ValueTask ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        ValueTask CopyToAsync(T[] array, int arrayIndex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously creates an array from the collection.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an array with the elements of the collection.</returns>
        ValueTask<T[]> ToArrayAsync(CancellationToken cancellationToken = default);
    }
}
