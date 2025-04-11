// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Collections
{
    public interface IAsyncObservableCollectionBase<TChange>
    {
        ValueTask DisposeAsync();
        Task<IDisposable> SubscribeAsync(Func<TChange, ValueTask> observer);
    }
}
