using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace MercadoBitcoin.Client.Internal.Caching
{
    /// <summary>
    /// Micro-cache configuration for Beast Mode optimization.
    /// Provides ultra-short TTL (500ms - 1s) cache for high-frequency data like Tickers and Orderbooks.
    /// 
    /// Purpose: Protects the application from violating rate limits in tight loops (e.g., UI render cycles)
    /// while maintaining data "freshness" from the user's perspective.
    /// 
    /// Example scenario:
    /// - Component A requests Ticker BTC-BRL at T=0ms -> Cache miss, HTTP request, response at T=50ms
    /// - Component B requests Ticker BTC-BRL at T=5ms -> Cache hit, served from memory at T=5.1ms
    /// - Component C requests Ticker BTC-BRL at T=20ms -> Cache hit, served from memory at T=20.1ms
    /// - Cache expires at T=510ms (TTL=500ms)
    /// - Next request at T=600ms -> Cache miss, fresh HTTP request
    /// 
    /// This pattern allows bursts of local requests without violating the global 500 req/min API limit.
    /// </summary>
    public sealed class MicroCacheConfiguration
    {
        /// <summary>
        /// Default TTL for ticker data (500ms). Balances freshness with rate limit protection.
        /// </summary>
        public static readonly TimeSpan DefaultTickerTtl = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Default TTL for orderbook data (1 second). Orderbooks change more frequently than tickers.
        /// </summary>
        public static readonly TimeSpan DefaultOrderbookTtl = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Default TTL for symbol list (1 hour). Symbols rarely change.
        /// </summary>
        public static readonly TimeSpan DefaultSymbolListTtl = TimeSpan.FromHours(1);

        /// <summary>
        /// TTL for ticker cache entries
        /// </summary>
        public TimeSpan TickerTtl { get; set; } = DefaultTickerTtl;

        /// <summary>
        /// TTL for orderbook cache entries
        /// </summary>
        public TimeSpan OrderbookTtl { get; set; } = DefaultOrderbookTtl;

        /// <summary>
        /// TTL for symbol list cache
        /// </summary>
        public TimeSpan SymbolListTtl { get; set; } = DefaultSymbolListTtl;

        /// <summary>
        /// Whether to enable micro-caching globally
        /// </summary>
        public bool EnableMicroCache { get; set; } = true;

        /// <summary>
        /// Enable cache statistics collection (small performance overhead)
        /// </summary>
        public bool CollectStatistics { get; set; } = false;
    }

    /// <summary>
    /// Micro-cache hit/miss statistics for diagnostics
    /// </summary>
    public class MicroCacheStatistics
    {
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public double HitRate => TotalHits + TotalMisses == 0 ? 0 : (double)TotalHits / (TotalHits + TotalMisses);
        public int CurrentCacheSize { get; set; }
        public long SavedRequests => TotalHits; // Each hit = 1 saved API request
    }

    /// <summary>
    /// Provides micro-cache services for high-frequency ticker and orderbook data.
    /// Wraps IMemoryCache with optimized settings for HFT scenarios.
    /// </summary>
    public sealed class MicroCache : IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly MicroCacheConfiguration _config;
        private readonly ConcurrentDictionary<string, CacheEntry> _metadata;
        private readonly object _statsLock = new();
        private MicroCacheStatistics _stats = new();

        private class CacheEntry
        {
            public string CacheKey { get; init; } = string.Empty;
            public CacheType Type { get; init; }
            public DateTime CreatedAt { get; init; }
        }

        private enum CacheType
        {
            Ticker,
            Orderbook,
            SymbolList,
            Custom
        }

        public MicroCache(IMemoryCache cache, MicroCacheConfiguration? config = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _config = config ?? new MicroCacheConfiguration();
            _metadata = new ConcurrentDictionary<string, CacheEntry>();
        }

        /// <summary>
        /// Gets or creates a cached ticker entry.
        /// </summary>
        /// <typeparam name="T">Ticker type</typeparam>
        /// <param name="symbol">Ticker symbol (e.g., "BTC-BRL")</param>
        /// <param name="factory">Factory function to create the ticker if not cached</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Cached or freshly created ticker</returns>
        public async Task<T> GetOrCreateTickerAsync<T>(
            string symbol,
            Func<CancellationToken, Task<T>> factory,
            CancellationToken ct = default) where T : class
        {
            if (!_config.EnableMicroCache)
            {
                return await factory(ct).ConfigureAwait(false);
            }

            var cacheKey = $"ticker_{symbol.ToUpperInvariant()}";
            return await GetOrCreateAsync(cacheKey, factory, _config.TickerTtl, CacheType.Ticker, ct)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets or creates a cached orderbook entry.
        /// </summary>
        public async Task<T> GetOrCreateOrderbookAsync<T>(
            string symbol,
            Func<CancellationToken, Task<T>> factory,
            CancellationToken ct = default) where T : class
        {
            if (!_config.EnableMicroCache)
            {
                return await factory(ct).ConfigureAwait(false);
            }

            var cacheKey = $"orderbook_{symbol.ToUpperInvariant()}";
            return await GetOrCreateAsync(cacheKey, factory, _config.OrderbookTtl, CacheType.Orderbook, ct)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets or creates a cached symbol list.
        /// </summary>
        public async Task<IEnumerable<string>> GetOrCreateSymbolListAsync(
            Func<CancellationToken, Task<IEnumerable<string>>> factory,
            CancellationToken ct = default)
        {
            if (!_config.EnableMicroCache)
            {
                return await factory(ct).ConfigureAwait(false);
            }

            const string cacheKey = "symbol_list";
            return await GetOrCreateAsync(cacheKey, factory, _config.SymbolListTtl, CacheType.SymbolList, ct)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generic get-or-create with custom TTL.
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan ttl,
            CancellationToken ct = default) where T : class
        {
            return await GetOrCreateAsync(cacheKey, factory, ttl, CacheType.Custom, ct)
                .ConfigureAwait(false);
        }

        private async Task<T> GetOrCreateAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan ttl,
            CacheType type,
            CancellationToken ct) where T : class
        {
            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out T? cachedValue))
            {
                RecordHit();
                return cachedValue!;
            }

            // Cache miss: call factory
            RecordMiss();
            var value = await factory(ct).ConfigureAwait(false);

            if (value != null)
            {
                // Store in cache with TTL
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl,
                    Priority = CacheItemPriority.High,
                    Size = EstimateCacheSize(value), // Rough memory estimate
                };

                _cache.Set(cacheKey, value, cacheOptions);

                // Track metadata
                _metadata.AddOrUpdate(cacheKey,
                    new CacheEntry { CacheKey = cacheKey, Type = type, CreatedAt = DateTime.UtcNow },
                    (k, v) => new CacheEntry { CacheKey = k, Type = type, CreatedAt = DateTime.UtcNow });
            }

            return value!;
        }

        /// <summary>
        /// Invalidates a specific cache entry (e.g., after an update).
        /// </summary>
        public void InvalidateTickerCache(string symbol)
        {
            var cacheKey = $"ticker_{symbol.ToUpperInvariant()}";
            _cache.Remove(cacheKey);
            _metadata.TryRemove(cacheKey, out _);
        }

        /// <summary>
        /// Invalidates a specific orderbook cache entry.
        /// </summary>
        public void InvalidateOrderbookCache(string symbol)
        {
            var cacheKey = $"orderbook_{symbol.ToUpperInvariant()}";
            _cache.Remove(cacheKey);
            _metadata.TryRemove(cacheKey, out _);
        }

        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        public void Clear()
        {
            // IMemoryCache doesn't provide a bulk clear; we track metadata and remove individually
            foreach (var key in _metadata.Keys.ToList())
            {
                _cache.Remove(key);
                _metadata.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Gets current cache statistics.
        /// </summary>
        public MicroCacheStatistics GetStatistics()
        {
            lock (_statsLock)
            {
                return new MicroCacheStatistics
                {
                    TotalHits = _stats.TotalHits,
                    TotalMisses = _stats.TotalMisses,
                    CurrentCacheSize = _metadata.Count,
                };
            }
        }

        private void RecordHit()
        {
            if (!_config.CollectStatistics) return;

            lock (_statsLock)
            {
                _stats.TotalHits++;
            }
        }

        private void RecordMiss()
        {
            if (!_config.CollectStatistics) return;

            lock (_statsLock)
            {
                _stats.TotalMisses++;
            }
        }

        private static long EstimateCacheSize(object? obj)
        {
            // Rough estimate: 1KB minimum, +1KB per property for complex objects
            // For a Ticker with ~10 properties, assume ~2-5KB
            return obj switch
            {
                string s => s.Length,
                IEnumerable<object> e => e.Count() * 100, // Rough estimate: 100 bytes per item
                _ => 2048, // Default: 2KB for typical ticker/orderbook
            };
        }

        public void Dispose()
        {
            Clear();
            _cache?.Dispose();
        }
    }
}
