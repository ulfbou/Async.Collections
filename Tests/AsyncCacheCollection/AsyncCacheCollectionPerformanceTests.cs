// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using System.Diagnostics;

using Xunit;

namespace Async.Collections.Tests.AsyncCacheCollection
{
    //public class AsyncCacheCollectionPerformanceTests
    //{
    //    [Fact]
    //    public async Task HighLoadBatchAddAndRetrieve_ShouldHandleHighConcurrency()
    //    {
    //        // Arrange
    //        var evictionInterval = TimeSpan.FromMinutes(1);
    //        var cache = new AsyncCacheCollection<string, int>(evictionInterval);
    //        var concurrencyLevel = 1000;
    //        var tasks = new List<Task>();

    //        // Act
    //        for (int i = 0; i < concurrencyLevel; i++)
    //        {
    //            var key = $"key{i}";
    //            tasks.Add(cache.GetOrAddAsync(key, async () =>
    //            {
    //                await Task.Delay(1); // Simulate some delay
    //                return i;
    //            }));
    //        }

    //        await Task.WhenAll(tasks);

    //        // Assert
    //        var items = new List<KeyValuePair<string, int>>();
    //        await foreach (var item in cache)
    //        {
    //            items.Add(item);
    //        }

    //        items.Count.Should().Be(concurrencyLevel);
    //    }

    //    [Fact]
    //    public async Task EvictionUnderLoad_ShouldEvictItemsCorrectly()
    //    {
    //        // Arrange
    //        var evictionInterval = TimeSpan.FromMilliseconds(500);
    //        var cache = new AsyncCacheCollection<string, int>(evictionInterval);
    //        var concurrencyLevel = 1000;
    //        var tasks = new List<Task>();

    //        // Act
    //        for (int i = 0; i < concurrencyLevel; i++)
    //        {
    //            var key = $"key{i}";
    //            tasks.Add(cache.GetOrAddAsync(key, async () =>
    //            {
    //                await Task.Delay(1); // Simulate some delay
    //                return i;
    //            }));
    //        }

    //        await Task.WhenAll(tasks);

    //        // Wait for eviction interval to pass
    //        await Task.Delay(evictionInterval + TimeSpan.FromMilliseconds(100));

    //        // Assert
    //        var items = new List<KeyValuePair<string, int>>();
    //        await foreach (var item in cache)
    //        {
    //            items.Add(item);
    //        }

    //        items.Should().BeEmpty();
    //    }

    //    [Fact]
    //    public async Task EvictionOccursInTimelyManner_ShouldRunCloseToSpecifiedInterval()
    //    {
    //        // Arrange
    //        var evictionInterval = TimeSpan.FromMilliseconds(500);
    //        var cache = new AsyncCacheCollection<string, int>(evictionInterval);
    //        var stopwatch = new Stopwatch();

    //        // Add an item to trigger eviction
    //        await cache.GetOrAddAsync("key1", () => Task.FromResult(42));

    //        // Act
    //        stopwatch.Start();
    //        await Task.Delay(evictionInterval + TimeSpan.FromMilliseconds(100));
    //        stopwatch.Stop();

    //        // Assert
    //        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(evictionInterval.TotalMilliseconds);
    //    }
    //}
}
