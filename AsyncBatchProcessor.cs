// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Async.Collections
{
    public class AsyncBatchProcessor<T> : IDisposable
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly int _batchSize;
        private readonly TimeSpan _batchTimeWindow;
        private readonly Func<List<T>, CancellationToken, ValueTask> _processBatchAsync;
        private readonly Timer _batchTimer;
        private readonly object _lock = new object();
        private bool _processing = false;

        public AsyncBatchProcessor(int batchSize, TimeSpan batchTimeWindow, Func<List<T>, CancellationToken, ValueTask> processBatchAsync)
        {
            _batchSize = batchSize;
            _batchTimeWindow = batchTimeWindow;
            _processBatchAsync = processBatchAsync;
            _batchTimer = new Timer(ProcessBatches!, null, batchTimeWindow, batchTimeWindow);
        }

        public void AddItem(T item)
        {
            _queue.Enqueue(item);
        }

        private async void ProcessBatches(object state)
        {
            if (_processing) { return; }
            lock (_lock)
            {
                if (_processing) { return; }
                _processing = true;
            }

            try
            {
                while (_queue.Count >= _batchSize)
                {
                    var batch = new List<T>();
                    for (int i = 0; i < _batchSize; i++)
                    {
                        if (_queue.TryDequeue(out T? item))
                        {
                            batch.Add(item);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await _processBatchAsync(batch, CancellationToken.None);
                    }
                }
            }
            finally
            {
                lock (_lock)
                {
                    _processing = false;
                }
            }
        }

        public void Dispose()
        {
            _batchTimer?.Dispose();
        }
    }
}
