// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncBag<T>
    {
        int Count { get; }

        ValueTask AddAsync(T item, CancellationToken cancellationToken = default);
        ValueTask DisposeAsync();
        IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
        IAsyncEnumerable<T> ToListAsync(CancellationToken cancellationToken = default);
        ValueTask<(bool Success, T? Item)> TryTakeAsync(CancellationToken cancellationToken = default);
    }
}