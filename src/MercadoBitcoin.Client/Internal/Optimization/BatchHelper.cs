using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Optimization
{
    /// <summary>
    /// Implements the Universal Filter pattern with intelligent batching strategies.
    /// Automatically chooses between native batching (chunking) and parallel fan-out based on endpoint capabilities.
    /// 
    /// Supports:
    /// - Automatic symbol discovery (caching for 1 hour)
    /// - Symbol normalization and validation
    /// - Native chunking for endpoints accepting CSV lists (e.g., /tickers)
    /// - Parallel fan-out with rate limiting for single-symbol endpoints (e.g., /orderbook)
    /// - Negative caching to prevent repeated failed lookups
    /// </summary>
    public static class BatchHelper
    {
        /// <summary>
        /// Cache for symbol validation. Key: "symbols", Value: HashSet<string> of valid symbols.
        /// </summary>
        private static readonly ConcurrentDictionary<string, CachedSymbols> _symbolCache = new();

        private class CachedSymbols
        {
            public HashSet<string> Symbols { get; init; } = new();
            public DateTime ExpiresAt { get; init; }
            public HashSet<string> InvalidSymbols { get; init; } = new(); // Negative cache
        }

        /// <summary>
        /// Executes a universal batch operation with intelligent strategy selection.
        /// 
        /// Cases:
        /// 1. No symbols specified -> Auto-discover all active symbols
        /// 2. Symbols specified -> Validate and normalize, then batch/fan-out
        /// </summary>
        public static async Task<IEnumerable<TResult>> ExecuteUniversalBatchAsync<TResult>(
            IEnumerable<string>? requestedSymbols,
            Func<CancellationToken, Task<IEnumerable<string>>> getAllSymbolsFunc,
            Func<string, CancellationToken, Task<TResult>> singleItemApiCall,
            Func<IEnumerable<string>, CancellationToken, Task<IEnumerable<TResult>>>? batchApiCall,
            int batchSize = 50,
            int maxDegreeOfParallelism = 10,
            TokenBucketRateLimiter? rateLimiter = null,
            CancellationToken ct = default)
        {
            // Step 1: Resolve and normalize symbols
            var symbolsToProcess = await ResolveSymbolsAsync(requestedSymbols, getAllSymbolsFunc, ct)
                .ConfigureAwait(false);

            if (!symbolsToProcess.Any())
                return Enumerable.Empty<TResult>();

            // Step 2: Choose strategy based on endpoint capability
            if (batchApiCall != null)
            {
                // Native batching strategy: endpoint accepts CSV list
                return await ExecuteNativeBatchAsync(
                    symbolsToProcess,
                    batchApiCall,
                    batchSize,
                    rateLimiter,
                    ct)
                    .ConfigureAwait(false);
            }
            else
            {
                // Fan-out strategy: each symbol requires separate call
                return await ExecuteParallelFanOutAsync(
                    symbolsToProcess,
                    singleItemApiCall,
                    maxDegreeOfParallelism,
                    rateLimiter,
                    ct)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Resolves the symbols to process, handling three cases:
        /// 1. Null or empty -> fetch all from cache or API
        /// 2. Specified -> validate against cache, remove invalid ones
        /// </summary>
        private static async Task<List<string>> ResolveSymbolsAsync(
            IEnumerable<string>? symbols,
            Func<CancellationToken, Task<IEnumerable<string>>> getAllSymbolsFunc,
            CancellationToken ct)
        {
            const string cacheKey = "all_symbols";
            var now = DateTime.UtcNow;

            // Check if cache is still valid
            if (_symbolCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > now)
            {
                // Cache hit
                if (symbols == null || !symbols.Any())
                {
                    return cached.Symbols.ToList();
                }

                // Normalize and validate against cache
                return NormalizeAndValidateSymbols(symbols, cached.Symbols, cached.InvalidSymbols).ToList();
            }

            // Cache miss: fetch from API
            var allSymbols = await getAllSymbolsFunc(ct).ConfigureAwait(false);
            var symbolSet = new HashSet<string>(allSymbols, StringComparer.OrdinalIgnoreCase);

            // Update cache (1 hour TTL)
            var cachedEntry = new CachedSymbols
            {
                Symbols = symbolSet,
                ExpiresAt = now.AddHours(1),
                InvalidSymbols = new()
            };

            _symbolCache.AddOrUpdate(cacheKey, cachedEntry, (k, v) => cachedEntry);

            // Return requested symbols or all if none specified
            if (symbols == null || !symbols.Any())
            {
                return symbolSet.ToList();
            }

            return NormalizeAndValidateSymbols(symbols, symbolSet, cachedEntry.InvalidSymbols).ToList();
        }

        /// <summary>
        /// Normalizes symbols (trim, uppercase, distinct) and filters out invalid ones.
        /// Updates negative cache for invalid symbols to avoid repeated failed lookups.
        /// </summary>
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
            var newInvalid = new List<string>();

            foreach (var symbol in normalized)
            {
                // Check negative cache first (for performance)
                if (invalidCache.Contains(symbol))
                {
                    continue; // Skip known invalid symbols
                }

                // Check positive cache
                if (validSymbols.Contains(symbol))
                {
                    results.Add(symbol);
                }
                else
                {
                    newInvalid.Add(symbol);
                }
            }

            // Update negative cache
            foreach (var invalid in newInvalid)
            {
                invalidCache.Add(invalid);
            }

            return results;
        }

        /// <summary>
        /// Strategy 1: Native Chunking for endpoints that accept CSV lists.
        /// Chunks symbols into batches and executes batch API calls in parallel.
        /// 
        /// Example: /tickers?symbols=BTC-BRL,ETH-BRL,LTC-BRL
        /// </summary>
        private static async Task<IEnumerable<TResult>> ExecuteNativeBatchAsync<TResult>(
            List<string> symbols,
            Func<IEnumerable<string>, CancellationToken, Task<IEnumerable<TResult>>> batchApiCall,
            int batchSize,
            TokenBucketRateLimiter? rateLimiter,
            CancellationToken ct)
        {
            var chunks = symbols.Chunk(batchSize);
            var tasks = new List<Task<IEnumerable<TResult>>>();

            foreach (var chunk in chunks)
            {
                // Rate limit if provided
                if (rateLimiter != null)
                {
                    await rateLimiter.AcquireAsync(1, ct).ConfigureAwait(false);
                }

                // Execute batch call for this chunk
                var task = batchApiCall(chunk, ct);
                tasks.Add(task);
            }

            // Wait for all chunks to complete
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Flatten and return
            return results.SelectMany(r => r);
        }

        /// <summary>
        /// Strategy 2: Parallel Fan-Out for endpoints that only accept single symbols.
        /// Executes individual API calls in parallel with configurable max degree and rate limiting.
        /// 
        /// Example: /orderbook/{symbol} -> called once per symbol
        /// </summary>
        private static async Task<IEnumerable<TResult>> ExecuteParallelFanOutAsync<TResult>(
            List<string> symbols,
            Func<string, CancellationToken, Task<TResult>> singleApiCall,
            int maxDegreeOfParallelism,
            TokenBucketRateLimiter? rateLimiter,
            CancellationToken ct)
        {
            var results = new ConcurrentBag<TResult>();
            var errors = new ConcurrentBag<(string Symbol, Exception Error)>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(symbols, parallelOptions, async (symbol, token) =>
            {
                try
                {
                    // Rate limit if provided
                    if (rateLimiter != null)
                    {
                        await rateLimiter.AcquireAsync(1, token).ConfigureAwait(false);
                    }

                    var result = await singleApiCall(symbol, token).ConfigureAwait(false);

                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    // Collect errors but continue processing other symbols
                    errors.Add((symbol, ex));
                    // Log or handle error as needed - for now, just skip this symbol
                }
            }).ConfigureAwait(false);

            // If any errors occurred and all symbols failed, throw aggregate exception
            if (errors.Count == symbols.Count && errors.Count > 0)
            {
                throw new AggregateException(
                    $"All symbols failed processing. Details: {string.Join("; ", errors.Select(e => $"{e.Symbol}: {e.Error.Message}"))}",
                    errors.Select(e => e.Error));
            }

            return results;
        }

        /// <summary>
        /// Clears the symbol cache. Useful for testing or when symbols have changed.
        /// </summary>
        public static void ClearSymbolCache()
        {
            _symbolCache.Clear();
        }

        /// <summary>
        /// Gets cache statistics for diagnostics.
        /// </summary>
        public static (int CacheSize, DateTime? ExpiresAt, int InvalidCount) GetCacheStats()
        {
            const string cacheKey = "all_symbols";
            if (_symbolCache.TryGetValue(cacheKey, out var cached))
            {
                return (cached.Symbols.Count, cached.ExpiresAt, cached.InvalidSymbols.Count);
            }

            return (0, null, 0);
        }
    }
}
