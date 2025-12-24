using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace MercadoBitcoin.Client.Internal.Helpers;

/// <summary>
/// Universal Filter + Batching helper with intelligent strategy selection.
/// - Native chunking for endpoints that accept CSV symbols
/// - Parallel fan-out with rate limiting for single-symbol endpoints
/// - Symbol discovery + normalization + negative cache
/// </summary>
internal static class BatchHelper
{
    private class CachedSymbols
    {
        public HashSet<string> Symbols { get; init; } = new();
        public HashSet<string> InvalidSymbols { get; init; } = new();
        public DateTime ExpiresAt { get; init; }
    }

    private static readonly ConcurrentDictionary<string, CachedSymbols> _symbolCache = new();
    private const string SymbolCacheKey = "all_symbols";

    /// <summary>
    /// Executes a batched API call for endpoints that support comma-separated symbols (native chunking).
    /// </summary>
    public static async Task<IEnumerable<TResult>> ExecuteNativeBatchAsync<TResult>(
        IEnumerable<string>? symbols,
        int batchSize,
        Func<CancellationToken, Task<IEnumerable<string>>> getAllSymbolsFunc,
        Func<string, CancellationToken, Task<IEnumerable<TResult>>> apiCall,
        CancellationToken ct,
        TokenBucketRateLimiter? rateLimiter = null)
    {
        var symbolsToProcess = await ResolveSymbolsAsync(symbols, getAllSymbolsFunc, ct).ConfigureAwait(false);
        if (symbolsToProcess.Count == 0) return Enumerable.Empty<TResult>();

        var tasks = new List<Task<IEnumerable<TResult>>>();

        foreach (var chunk in symbolsToProcess.Chunk(batchSize))
        {
            if (rateLimiter != null)
            {
                await rateLimiter.AcquireAsync(1, ct).ConfigureAwait(false);
            }

            tasks.Add(apiCall(string.Join(",", chunk), ct));
        }

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
        CancellationToken ct,
        TokenBucketRateLimiter? rateLimiter = null)
    {
        var symbolsToProcess = await ResolveSymbolsAsync(symbols, getAllSymbolsFunc, ct).ConfigureAwait(false);
        if (symbolsToProcess.Count == 0) return Enumerable.Empty<TResult>();

        var results = new ConcurrentBag<TResult>();
        var errors = new ConcurrentBag<(string Symbol, Exception Error)>();

        await Parallel.ForEachAsync(symbolsToProcess, new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = ct
        }, async (symbol, token) =>
        {
            try
            {
                if (rateLimiter != null)
                {
                    await rateLimiter.AcquireAsync(1, token).ConfigureAwait(false);
                }

                var result = await apiCall(symbol, token).ConfigureAwait(false);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                errors.Add((symbol, ex));
            }
        }).ConfigureAwait(false);

        // If every symbol failed, throw aggregate to surface the issue
        if (errors.Count == symbolsToProcess.Count && errors.Count > 0)
        {
            throw new AggregateException(
                $"All symbols failed. Details: {string.Join("; ", errors.Select(e => $"{e.Symbol}: {e.Error.Message}"))}",
                errors.Select(e => e.Error));
        }

        return results;
    }

    /// <summary>
    /// Clears the cached symbol list (useful for tests or when asset universe changes).
    /// </summary>
    public static void ClearSymbolCache() => _symbolCache.Clear();

    private static async Task<List<string>> ResolveSymbolsAsync(
        IEnumerable<string>? symbols,
        Func<CancellationToken, Task<IEnumerable<string>>> getAllSymbolsFunc,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        if (_symbolCache.TryGetValue(SymbolCacheKey, out var cached) && cached.ExpiresAt > now)
        {
            if (symbols == null || !symbols.Any())
            {
                return cached.Symbols.ToList();
            }

            return NormalizeAndValidateSymbols(symbols, cached.Symbols, cached.InvalidSymbols).ToList();
        }

        // Cache miss: fetch all symbols and refresh cache
        var allSymbols = await getAllSymbolsFunc(ct).ConfigureAwait(false);
        var symbolSet = new HashSet<string>(allSymbols.Select(s => s.Trim().ToUpperInvariant()), StringComparer.Ordinal);

        var newCache = new CachedSymbols
        {
            Symbols = symbolSet,
            InvalidSymbols = new HashSet<string>(StringComparer.Ordinal),
            ExpiresAt = now.AddHours(1)
        };

        _symbolCache.AddOrUpdate(SymbolCacheKey, newCache, (_, __) => newCache);

        if (symbols == null || !symbols.Any())
        {
            return symbolSet.ToList();
        }

        return NormalizeAndValidateSymbols(symbols, symbolSet, newCache.InvalidSymbols).ToList();
    }

    private static IEnumerable<string> NormalizeAndValidateSymbols(
        IEnumerable<string> symbols,
        HashSet<string> validSymbols,
        HashSet<string> invalidCache)
    {
        var normalized = symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var results = new List<string>();

        foreach (var symbol in normalized)
        {
            if (invalidCache.Contains(symbol))
            {
                continue;
            }

            if (validSymbols.Contains(symbol))
            {
                results.Add(symbol);
            }
            else
            {
                invalidCache.Add(symbol);
            }
        }

        return results;
    }
}
