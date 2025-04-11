// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncSet<T> where T : notnull
    {
        int Count { get; }

        ValueTask<bool> AddAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken = default);
        ValueTask DisposeAsync();
        ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default);
    }
}
