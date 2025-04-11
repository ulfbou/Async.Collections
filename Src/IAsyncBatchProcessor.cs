// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncBatchProcessor<T>
    {
        void Add(T item);
        void AddRange(IEnumerable<T> items);
        ValueTask DisposeAsync();
        void EnsureNotDisposed();
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}
