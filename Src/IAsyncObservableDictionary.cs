// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Async.Collections
{
    public interface IAsyncObservableDictionary<TKey, TValue> where TKey : notnull
    {
        TValue this[TKey key] { get; set; }

        int Count { get; }
        bool IsReadOnly { get; }
        ICollection<TKey> Keys { get; }
        ICollection<TValue> Values { get; }

        void Add(KeyValuePair<TKey, TValue> item);
        void Add(TKey key, TValue value);
        void Clear();
        bool Contains(KeyValuePair<TKey, TValue> item);
        bool ContainsKey(TKey key);
        void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);
        ValueTask DisposeAsync();
        IAsyncEnumerator<KeyValuePair<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = default);
        IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();
        bool Remove(KeyValuePair<TKey, TValue> item);
        bool Remove(TKey key);
        IDisposable Subscribe(Func<CollectionChange<TKey, TValue>, ValueTask> observer);
        IDisposable Subscribe(IObserver<CollectionChange<TKey, TValue>> observer);
        bool TryAdd(TKey key, TValue value);
        bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
        bool TryRemove(TKey key, out TValue? value);
        bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue);
    }
}