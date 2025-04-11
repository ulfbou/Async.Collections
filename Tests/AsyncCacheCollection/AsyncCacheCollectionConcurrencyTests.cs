// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    public class AsyncCacheCollectionConcurrencyTests
    {
        [Fact]
        public async Task GetOrAddAsync_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            var concurrencyLevel = 50;
            var tasks = new List<Task<int>>();

            // Act
            for (int i = 0; i < concurrencyLevel; i++)
            {
                var key = $"key{i}";
                tasks.Add(cache.GetOrAddAsync(key, async () =>
                {
                    await Task.Delay(5); // Simulate some delay in factory
                    return i;
                }).AsTask());
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(concurrencyLevel);
            for (int i = 0; i < concurrencyLevel; i++)
            {
                results.Should().Contain(i); // Verify all expected values are returned
            }
        }

        [Fact]
        public async Task TryRemove_ShouldHandleConcurrentRemoves()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Add 10 items
            for (int i = 0; i < 10; i++)
            {
                await cache.GetOrAddAsync($"key{i}", () => ValueTask.FromResult<int>(i));
            }

            var concurrencyLevel = 50;
            var tasks = new List<Task<bool>>();

            // Act
            for (int i = 0; i < concurrencyLevel; i++)
            {
                var key = $"key{i % 10}"; // Use 10 distinct keys to ensure some overlap
                tasks.Add(Task.Run(() => cache.TryRemove(key)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            // Count the number of successful removes - should be 10 (one for each key)
            results.Count(r => r).Should().Be(10);

            var items = new List<KeyValuePair<string, int>>();
            await foreach (var item in cache)
            {
                items.Add(item);
            }

            items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncEnumerator_ShouldHandleConcurrentModifications()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);

            // Add initial items
            for (int i = 0; i < 10; i++)
            {
                await cache.GetOrAddAsync($"key{i}", () => ValueTask.FromResult(i));
            }

            // Act
            var modificationTask = Task.Run(async () =>
            {
                for (int i = 10; i < 20; i++)
                {
                    await cache.GetOrAddAsync($"key{i}", () => ValueTask.FromResult(i));
                }

                for (int i = 0; i < 5; i++)
                {
                    cache.TryRemove($"key{i}");
                }
            });

            // Start enumerating while modification is happening
            var enumerationTask = Task.Run(async () =>
            {
                var items = new List<KeyValuePair<string, int>>();
                await foreach (var item in cache)
                {
                    items.Add(item);
                    await Task.Delay(5); // Small delay to increase chance of concurrent modifications
                }
                return items;
            });

            await Task.WhenAll(modificationTask, enumerationTask);
            var items = await enumerationTask;

            // Assert
            items.Should().NotBeEmpty();
            items.Should().OnlyHaveUniqueItems(b => b.Key); // Verify keys are unique
        }

        [Fact]
        public async Task GetOrAddAsync_ShouldBeThreadSafeForSameKey()
        {
            // Arrange
            var evictionInterval = TimeSpan.FromMinutes(1);
            var cache = new AsyncCacheCollection<string, int>(evictionInterval);
            var concurrencyLevel = 50;
            var tasks = new List<Task<int>>();
            var key = "sameKey";
            var factoryCallCount = 0;

            // Act
            for (int i = 0; i < concurrencyLevel; i++)
            {
                tasks.Add(cache.GetOrAddAsync(key, async () =>
                {
                    Interlocked.Increment(ref factoryCallCount);
                    await Task.Delay(5);
                    return 42;
                }).AsTask());
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            factoryCallCount.Should().Be(1); // Factory should be called exactly once
            results.Should().AllBeEquivalentTo(42); // All results should be the same
        }
    }
}
