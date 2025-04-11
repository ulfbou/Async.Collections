// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections
{
    /// <summary>
    /// Represents an asynchronous priority queue.
    /// </summary>
    /// <typeparam name="TPriority">The type of the priority.</typeparam>
    /// <typeparam name="TItem">The type of the items in the queue.</typeparam>
    public class AsyncPriorityQueue<TPriority, TItem> : IAsyncCollection<TItem>
        where TPriority : IComparable<TPriority>
    {
        private readonly SortedDictionary<TPriority, Queue<TItem>> _queues = new();

        public Func<TItem, TPriority> PrioritySelector { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncPriorityQueue{TPriority, TItem}"/> class.
        /// </summary>
        /// <param name="prioritySelector">The function to select the priority for each item.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="prioritySelector"/> is <see langref="null"/>.</exception>
        public AsyncPriorityQueue(Func<TItem, TPriority> prioritySelector)
        {
            PrioritySelector = prioritySelector ?? throw new ArgumentNullException(nameof(prioritySelector));
        }

        /// <inheritdoc />
        public ValueTask AddAsync(TItem item, CancellationToken cancellationToken = default)
        {
            var priority = PrioritySelector(item);
            lock (_queues)
            {
                if (!_queues.TryGetValue(priority, out var queue))
                {
                    queue = new Queue<TItem>();
                    _queues[priority] = queue;
                }
                queue.Enqueue(item);
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            lock (_queues)
            {
                _queues.Clear();
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public ValueTask<bool> ContainsAsync(TItem item, CancellationToken cancellationToken = default)
        {
            lock (_queues)
            {
                foreach (var queue in _queues.Values)
                {
                    if (queue.Contains(item))
                    {
                        return ValueTask.FromResult(true);
                    }
                }
            }
            return ValueTask.FromResult(false);
        }

        /// <inheritdoc />
        public ValueTask CopyToAsync(TItem[] array, int arrayIndex, CancellationToken cancellationToken = default)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            lock (_queues)
            {
                foreach (var queue in _queues.Values)
                {
                    foreach (var item in queue)
                    {
                        if (arrayIndex >= array.Length)
                        {
                            throw new ArgumentException("The array is too small to hold all the items.");
                        }
                        array[arrayIndex++] = item;
                    }
                }
            }

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        {
            int count = 0;
            lock (_queues)
            {
                foreach (var queue in _queues.Values)
                {
                    count += queue.Count;
                }
            }
            return ValueTask.FromResult(count);
        }

        /// <inheritdoc />
        public ValueTask<bool> RemoveAsync(TItem item, CancellationToken cancellationToken = default)
        {
            lock (_queues)
            {
                foreach (var queue in _queues.Values)
                {
                    if (queue.Contains(item))
                    {
                        var tempQueue = new Queue<TItem>();
                        bool removed = false;

                        while (queue.Count > 0)
                        {
                            var dequeuedItem = queue.Dequeue();
                            if (!removed && EqualityComparer<TItem>.Default.Equals(dequeuedItem, item))
                            {
                                removed = true;
                            }
                            else
                            {
                                tempQueue.Enqueue(dequeuedItem);
                            }
                        }

                        while (tempQueue.Count > 0)
                        {
                            queue.Enqueue(tempQueue.Dequeue());
                        }

                        return ValueTask.FromResult(removed);
                    }
                }
            }
            return ValueTask.FromResult(false);
        }

        /// <inheritdoc />
        public ValueTask<TItem[]> ToArrayAsync(CancellationToken cancellationToken = default)
        {
            lock (_queues)
            {
                var result = _queues.Values.SelectMany(q => q).ToArray();
                return ValueTask.FromResult(result);
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            lock (_queues)
            {
                _queues.Clear();
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public async IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            List<TItem> items;

            lock (_queues)
            {
                items = _queues.OrderByDescending(kvp => kvp.Key)
                               .SelectMany(kvp => kvp.Value)
                               .ToList();
            }

            foreach (var item in items)
            {
                yield return item;
                await Task.Yield();
            }
        }
    }
}
