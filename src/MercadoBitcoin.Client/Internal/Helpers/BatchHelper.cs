using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Helpers
{
    /// <summary>
    /// Helper for executing batched requests, both native (chunking) and client-side (parallel fan-out).
    /// </summary>
    internal static class BatchHelper
    {
        /// <summary>
        /// Executes a batched API call for endpoints that support comma-separated symbols, where each call returns a collection of results.
        /// </summary>
        public static async Task<IEnumerable<TResult>> ExecuteNativeBatchAsync<TResult>(
            IEnumerable<string> symbols,
            int batchSize,
            Func<string, CancellationToken, Task<IEnumerable<TResult>>> apiCall,
            CancellationToken ct)
        {
            var uniqueSymbols = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueSymbols.Count == 0) return Enumerable.Empty<TResult>();

            var tasks = uniqueSymbols
                .Chunk(batchSize)
                .Select(batch => apiCall(string.Join(",", batch), ct));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.SelectMany(r => r);
        }

        /// <summary>
        /// Executes a batched API call for endpoints that support comma-separated symbols, where each call returns a single result object.
        /// </summary>
        public static async Task<IEnumerable<TResult>> ExecuteNativeBatchSingleAsync<TResult>(
            IEnumerable<string> symbols,
            int batchSize,
            Func<string, CancellationToken, Task<TResult>> apiCall,
            CancellationToken ct)
        {
            var uniqueSymbols = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueSymbols.Count == 0) return Enumerable.Empty<TResult>();

            var tasks = uniqueSymbols
                .Chunk(batchSize)
                .Select(batch => apiCall(string.Join(",", batch), ct));

            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a parallel fan-out for endpoints that do not support native batching.
        /// </summary>
        public static async Task<IEnumerable<TResult>> ExecuteParallelFanOutAsync<TResult>(
            IEnumerable<string> symbols,
            int maxDegreeOfParallelism,
            Func<string, CancellationToken, Task<TResult>> apiCall,
            CancellationToken ct)
        {
            var uniqueSymbols = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueSymbols.Count == 0) return Enumerable.Empty<TResult>();

            var results = new System.Collections.Concurrent.ConcurrentBag<TResult>();

            await Parallel.ForEachAsync(uniqueSymbols, new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = ct
            }, async (symbol, token) =>
            {
                var result = await apiCall(symbol, token).ConfigureAwait(false);
                if (result != null)
                {
                    results.Add(result);
                }
            }).ConfigureAwait(false);

            return results;
        }
    }
}
