using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Optimization
{
    /// <summary>
    /// Implements the Singleflight pattern to coalesce multiple concurrent requests for the same resource into a single one.
    /// 
    /// This is critical for HFT scenarios where many consumers (e.g., UI components) request the same ticker simultaneously.
    /// By merging these into a single HTTP request, we reduce bandwidth, CPU, and comply with rate limits.
    /// 
    /// Scenario: 50 components ask for BTC-BRL ticker at ~the same time.
    /// Without coalescing: 50 HTTP requests.
    /// With coalescing: 1 HTTP request, shared result distributed to all 50 awaiters.
    /// </summary>
    internal sealed class RequestCoalescer : IDisposable
    {
        private readonly ConcurrentDictionary<string, Task<object>> _pendingRequests = new();
        private readonly object _disposeLock = new();
        private bool _disposed = false;

        /// <summary>
        /// Executes the provided task, or returns an existing pending task if one is already in flight for the same key.
        /// 
        /// Thread-safe and efficient. Uses spin-wait only in case of TryAdd collision, which is rare.
        /// </summary>
        /// <param name="key">Unique identifier for the resource (e.g., "ticker_BTC-BRL")</param>
        /// <param name="action">Async function to execute if no pending request exists</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Result of the action, shared among all concurrent requesters with the same key</returns>
        public async Task<T> ExecuteAsync<T>(string key, Func<CancellationToken, Task<T>> action, CancellationToken ct)
        {
            ThrowIfDisposed();

            while (true)
            {
                // Fast path: check if a request is already in flight
                if (_pendingRequests.TryGetValue(key, out var existingTask))
                {
                    try
                    {
                        var result = await existingTask.ConfigureAwait(false);
                        return (T)result!;
                    }
                    catch
                    {
                        // If the existing task failed, propagate the exception to all waiters
                        // (characteristic of Singleflight: all or nothing)
                        throw;
                    }
                }

                // Slow path: create a new TaskCompletionSource to execute the action
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                if (_pendingRequests.TryAdd(key, tcs.Task))
                {
                    try
                    {
                        // Execute the action
                        var result = await action(ct).ConfigureAwait(false);
                        tcs.SetResult(result!);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                        throw;
                    }
                    finally
                    {
                        // Clean up: remove the completed task from pending
                        _pendingRequests.TryRemove(key, out _);
                    }
                }
                // If TryAdd failed, another thread added a task for the same key in between our check and add.
                // Loop again to retrieve that newly added task (rare, typically <1 iteration)
            }
        }

        /// <summary>
        /// Gets the number of in-flight (pending) requests. Useful for diagnostics/monitoring.
        /// </summary>
        public int PendingRequestCount => _pendingRequests.Count;

        /// <summary>
        /// Clears all pending requests. Use with caution; ongoing awaiters will be orphaned.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            _pendingRequests.Clear();
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    _pendingRequests.Clear();
                    _disposed = true;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RequestCoalescer));
            }
        }
    }
}
