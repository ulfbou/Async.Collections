// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncQueue<T>
    {
        int Count { get; }

        ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default);
        ValueTask DisposeAsync();
        ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<(bool Success, T Item)> TryPeekAsync(CancellationToken cancellationToken = default);
    }
}
