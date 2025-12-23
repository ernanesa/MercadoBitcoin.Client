using System.Collections.Concurrent;

namespace MercadoBitcoin.Client.Internal.Helpers;

/// <summary>
/// Helper for executing batched requests, both native (chunking) and client-side (parallel fan-out).
/// </summary>
internal static class BatchHelper
{
    /// <summary>
    /// Executes a batched API call for endpoints that support comma-separated symbols.
    /// </summary>
    public static async Task<IEnumerable<TResult>> ExecuteNativeBatchAsync<TResult>(
        IEnumerable<string>? symbols,
        int batchSize,
        Func<CancellationToken, Task<IEnumerable<string>>> getAllSymbolsFunc,
        Func<string, CancellationToken, Task<IEnumerable<TResult>>> apiCall,
        CancellationToken ct)
    {
        var symbolsToProcess = await GetSymbolsToProcessAsync(symbols, getAllSymbolsFunc, ct);
        if (symbolsToProcess.Count == 0) return Enumerable.Empty<TResult>();

        var tasks = symbolsToProcess
            .Chunk(batchSize)
            .Select(batch => apiCall(string.Join(",", batch), ct));

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.SelectMany(r => r);
    }

    /// <summary>
    /// Executes a parallel fan-out for endpoints that do not support native batching.
    /// </summary>
    public static async Task<IEnumerable<TResult>> ExecuteParallelFanOutAsync<TResult>(
        IEnumerable<string>? symbols,
        int maxDegreeOfParallelism,
        Func<CancellationToken, Task<IEnumerable<string>>> getAllSymbolsFunc,
        Func<string, CancellationToken, Task<TResult>> apiCall,
        CancellationToken ct)
    {
        var symbolsToProcess = await GetSymbolsToProcessAsync(symbols, getAllSymbolsFunc, ct);
        if (symbolsToProcess.Count == 0) return Enumerable.Empty<TResult>();

        var results = new ConcurrentBag<TResult>();

        await Parallel.ForEachAsync(symbolsToProcess, new ParallelOptions
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

    private static async Task<List<string>> GetSymbolsToProcessAsync(
        IEnumerable<string>? symbols,
        Func<CancellationToken, Task<IEnumerable<string>>> getAllSymbolsFunc,
        CancellationToken ct)
    {
        if (symbols is null || !symbols.Any())
        {
            var allSymbols = await getAllSymbolsFunc(ct).ConfigureAwait(false);
            return allSymbols.ToList();
        }

        return symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
