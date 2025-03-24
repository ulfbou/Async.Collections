// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;

using System.Collections;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Async.Collections
{
    public class AsyncObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IObservable<CollectionChange<TKey, TValue>>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();
        private readonly List<Func<CollectionChange<TKey, TValue>, ValueTask>> _asyncObservers = new List<Func<CollectionChange<TKey, TValue>, ValueTask>>();

        private readonly List<Action<CollectionChange<TKey, TValue>>> _observers = new List<Action<CollectionChange<TKey, TValue>>>();
        private readonly object _lock = new object();
        private readonly ILogger<AsyncObservableDictionary<TKey, TValue>> _logger;

        public TValue this[TKey key] { get => _dictionary[key]; set => _dictionary[key] = value; }

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public AsyncObservableDictionary(ILogger<AsyncObservableDictionary<TKey, TValue>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Add(TKey key, TValue value)
        {
            if (_dictionary.TryAdd(key, value))
            {
                NotifyObservers(new CollectionChange<TKey, TValue>(CollectionChangeType.Add, key, value));
                _ = NotifyObserversAsync(new CollectionChange<TKey, TValue>(CollectionChangeType.Add, key, value));
            }
            else
            {
                throw new ArgumentException("Key already exists");
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (_dictionary.TryAdd(key, value))
            {
                NotifyObservers(new CollectionChange<TKey, TValue>(CollectionChangeType.Add, key, value));
                _ = NotifyObserversAsync(new CollectionChange<TKey, TValue>(CollectionChangeType.Add, key, value));
                return true;
            }

            return false;
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (_dictionary.TryUpdate(key, newValue, comparisonValue))
            {
                NotifyObservers(new CollectionChange<TKey, TValue>(CollectionChangeType.Update, key, newValue));
                _ = NotifyObserversAsync(new CollectionChange<TKey, TValue>(CollectionChangeType.Update, key, newValue));
                return true;
            }

            return false;
        }

        public bool TryRemove(TKey key, out TValue? value)
        {
            if (_dictionary.TryRemove(key, out value))
            {
                NotifyObservers(new CollectionChange<TKey, TValue>(CollectionChangeType.Remove, key, value));
                _ = NotifyObserversAsync(new CollectionChange<TKey, TValue>(CollectionChangeType.Remove, key, value));
                return true;
            }

            return false;
        }

        private void NotifyObservers(CollectionChange<TKey, TValue> change)
        {
            lock (_lock)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer(change);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying observer");
                    }
                }
            }
        }

        private async ValueTask NotifyObserversAsync(CollectionChange<TKey, TValue> change)
        {
            List<ValueTask> tasks;
            lock (_lock)
            {
                tasks = _asyncObservers.Select(observer => observer(change)).ToList();
            }

            await Task.WhenAll(tasks.Select(vt => vt.AsTask()));
        }

        public IDisposable Subscribe(Func<CollectionChange<TKey, TValue>, ValueTask> observer)
        {
            lock (_lock)
            {
                _asyncObservers.Add(observer);
            }

            return new Unsubscriber(this, obs =>
            {
                lock (_lock)
                {
                    _asyncObservers.Remove(obs);
                }
            }, observer);
        }

        public IDisposable Subscribe(IObserver<CollectionChange<TKey, TValue>> observer)
        {
            lock (_lock)
            {
                _observers.Add(observer.OnNext);
            }
            return new Unsubscriber(this, obs =>
            {
                lock (_lock)
                {
                    _observers.Remove(obs);
                }
            }, observer.OnNext);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear() => _dictionary.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public bool Remove(TKey key) => TryRemove(key, out _);

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (item.Value is null)
            {
                throw new ArgumentException(nameof(item.Value));
            }

            if (_dictionary.TryGetValue(item.Key, out TValue? val) && item.Value.Equals(val))
            {
                return TryRemove(item.Key, out _);
            }

            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => ((IDictionary<TKey, TValue>)_dictionary).TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();

        private class Unsubscriber : IDisposable
        {
            private readonly AsyncObservableDictionary<TKey, TValue> _dictionary;
            private readonly Action<Func<CollectionChange<TKey, TValue>, ValueTask>> _unsubscribe;
            private readonly Func<CollectionChange<TKey, TValue>, ValueTask> _observer;
            private readonly Action<Action<CollectionChange<TKey, TValue>>> _unsubscribeAction;
            private readonly Action<CollectionChange<TKey, TValue>> _actionObserver;

            public Unsubscriber(
                AsyncObservableDictionary<TKey, TValue> dictionary,
                Action<Func<CollectionChange<TKey, TValue>, ValueTask>> unsubscribe,
                Func<CollectionChange<TKey, TValue>, ValueTask> observer)
            {
                _dictionary = dictionary;
                _unsubscribe = unsubscribe;
                _unsubscribeAction = default!;
                _observer = observer;
                _actionObserver = default!;
            }

            public Unsubscriber(
                AsyncObservableDictionary<TKey, TValue> dictionary,
                Action<Action<CollectionChange<TKey, TValue>>> unsubscribe,
                Action<CollectionChange<TKey, TValue>> observer)
            {
                _dictionary = dictionary;
                _unsubscribe = default!;
                _unsubscribeAction = unsubscribe;
                _observer = default!;
                _actionObserver = observer;
            }

            public void Dispose()
            {
                if (_observer != null)
                {
                    _unsubscribe(_observer);
                }
                else
                {
                    _unsubscribeAction(_actionObserver);
                }
            }
        }
    }

    public struct CollectionChange<TKey, TValue>
    {
        public CollectionChangeType ChangeType;
        public TKey Key;
        public TValue Value;

        public CollectionChange(CollectionChangeType changeType, TKey key, TValue value)
        {
            ChangeType = changeType;
            Key = key;
            Value = value;
        }
    }

    public enum CollectionChangeType
    {
        Add,
        Update,
        Remove
    }
}
