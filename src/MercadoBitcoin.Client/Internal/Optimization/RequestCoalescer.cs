using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Optimization
{
    /// <summary>
    /// Implements the Singleflight pattern to coalesce multiple concurrent requests for the same resource into a single one.
    /// </summary>
    internal sealed class RequestCoalescer : IDisposable
    {
        private readonly ConcurrentDictionary<string, Task<object>> _pendingRequests = new();

        /// <summary>
        /// Executes the provided task, or returns an existing pending task if one is already in flight for the same key.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(string key, Func<CancellationToken, Task<T>> action, CancellationToken ct)
        {
            while (true)
            {
                if (_pendingRequests.TryGetValue(key, out var existingTask))
                {
                    try
                    {
                        var result = await existingTask.ConfigureAwait(false);
                        return (T)result;
                    }
                    catch
                    {
                        // If the existing task failed, we might want to retry or just propagate.
                        // For coalescing, if it failed, all waiters fail.
                        throw;
                    }
                }

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (_pendingRequests.TryAdd(key, tcs.Task))
                {
                    try
                    {
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
                        _pendingRequests.TryRemove(key, out _);
                    }
                }
                // If TryAdd failed, it means another thread just added a task for the same key.
                // Loop again to try and get that task.
            }
        }

        public void Dispose()
        {
            _pendingRequests.Clear();
        }
    }
}
