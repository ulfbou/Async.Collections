// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncCacheCollection<TKey, TValue> where TKey : notnull
    {
        ValueTask DisposeAsync();
        IAsyncEnumerator<KeyValuePair<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = default);
        ValueTask<TValue> GetOrAddAsync(TKey key, Func<ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default);
        bool TryRemove(TKey key);
    }
}
