using System.Buffers;

namespace MercadoBitcoin.Client.Internal;

/// <summary>
/// Helper class for batching operations with native API support and fan-out strategies.
/// </summary>
internal static class BatchHelper
{
    /// <summary>
    /// Executes batch requests for endpoints that support comma-separated symbols.
    /// </summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="symbols">Symbols to process.</param>
    /// <param name="batchSize">Maximum symbols per batch (default 100).</param>
    /// <param name="apiCall">API call function that accepts comma-separated symbols.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined results from all batches.</returns>
    public static async Task<IEnumerable<TResult>> ExecuteNativeBatchAsync<TResult>(
        IEnumerable<string> symbols,
        int batchSize,
        Func<string, CancellationToken, Task<IEnumerable<TResult>>> apiCall,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(apiCall);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var symbolList = symbols.ToList();
        if (symbolList.Count == 0)
        {
            return [];
        }

        var results = new List<TResult>(symbolList.Count);
        var batches = ChunkSymbols(symbolList, batchSize);

        var tasks = batches.Select(async batch =>
        {
            var symbolsParam = string.Join(",", batch);
            return await apiCall(symbolsParam, cancellationToken).ConfigureAwait(false);
        });

        var batchResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var batchResult in batchResults)
        {
            results.AddRange(batchResult);
        }

        return results;
    }

    /// <summary>
    /// Executes fan-out requests for endpoints that only support single symbols.
    /// </summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="symbols">Symbols to process.</param>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent requests.</param>
    /// <param name="apiCall">API call function for single symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined results from all requests.</returns>
    public static async Task<IEnumerable<TResult>> ExecuteFanOutAsync<TResult>(
        IEnumerable<string> symbols,
        int maxDegreeOfParallelism,
        Func<string, CancellationToken, Task<TResult>> apiCall,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(apiCall);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDegreeOfParallelism);

        var symbolList = symbols.ToList();
        if (symbolList.Count == 0)
        {
            return [];
        }

        var results = new List<TResult>(symbolList.Count);
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(symbolList, parallelOptions, async (symbol, ct) =>
        {
            var result = await apiCall(symbol, ct).ConfigureAwait(false);
            lock (results)
            {
                results.Add(result);
            }
        }).ConfigureAwait(false);

        return results;
    }

    private static IEnumerable<List<string>> ChunkSymbols(IReadOnlyList<string> symbols, int batchSize)
    {
        for (var index = 0; index < symbols.Count; index += batchSize)
        {
            var chunkSize = Math.Min(batchSize, symbols.Count - index);
            var chunk = new List<string>(chunkSize);

            for (var chunkIndex = 0; chunkIndex < chunkSize; chunkIndex++)
            {
                chunk.Add(symbols[index + chunkIndex]);
            }

            yield return chunk;
        }
    }
}
