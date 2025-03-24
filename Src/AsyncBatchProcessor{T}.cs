// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Async.Collections
{
    public class AsyncBatchProcessor<T> : IAsyncDisposable
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly int _batchSize;
        private readonly TimeSpan _batchTimeWindow;
        private readonly Func<List<T>, CancellationToken, ValueTask> _processBatchAsync;
        private readonly Func<Exception, List<T>, Task>? _onErrorAsync;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _batchProcessingTask;
        private bool _disposed;

        public AsyncBatchProcessor(
            int batchSize,
            TimeSpan batchTimeWindow,
            Func<List<T>, CancellationToken, ValueTask> processBatchAsync,
            Func<Exception, List<T>, Task>? onErrorAsync = null)
        {
            _batchSize = batchSize;
            _batchTimeWindow = batchTimeWindow;
            _processBatchAsync = processBatchAsync;
            _onErrorAsync = onErrorAsync;
            _batchProcessingTask = ProcessBatchesAsync();
        }

        public void Add(T item)
        {
            EnsureNotDisposed();
            _queue.Enqueue(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            EnsureNotDisposed();

            foreach (var item in items)
            {
                _queue.Enqueue(item);
            }
        }

        private async Task ProcessBatchesAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var batch = new List<T>();

                    // Collect a batch either by size or by time window.
                    while (batch.Count < _batchSize && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        if (_queue.TryDequeue(out T? item))
                        {
                            batch.Add(item);
                        }
                        else
                        {
                            break;
                        }

                        await Task.Delay(_batchTimeWindow, _cancellationTokenSource.Token);
                    }

                    if (batch.Count > 0)
                    {
                        await ProcessBatchWithErrorHandlingAsync(batch, _cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ProcessBatchWithErrorHandlingAsync(List<T> batch, CancellationToken cancellationToken)
        {
            try
            {
                await _processBatchAsync(batch, cancellationToken);
            }
            catch (Exception ex) when (_onErrorAsync != null)
            {
                await _onErrorAsync(ex, batch);
            }
            catch
            {
            }
        }

        public void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncBatchProcessor<T>));
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            var batch = new List<T>();

            while (_queue.TryDequeue(out T? item))
            {
                batch.Add(item);
            }

            if (batch.Count > 0)
            {
                await ProcessBatchWithErrorHandlingAsync(batch, cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cancellationTokenSource.Cancel();

                try
                {
                    if (_batchProcessingTask != null)
                    {
                        await _batchProcessingTask;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful cancellation during disposal; safe to ignore.
                }
                finally
                {
                    _cancellationTokenSource.Dispose();
                }
            }
        }
    }
}
