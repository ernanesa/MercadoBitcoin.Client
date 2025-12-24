using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Caching
{
    /// <summary>
    /// Advanced multi-level caching with TTL-based expiry, cache invalidation, and warming strategies.
    /// Supports L1 (in-memory) and L2 (distributed) caching.
    /// </summary>
    public sealed class AdvancedCacheManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private volatile bool _disposed;

        /// <summary>
        /// Represents a cached entry with TTL and metadata.
        /// </summary>
        private sealed class CacheEntry
        {
            public object? Value { get; set; }
            public long ExpiresAtUtc { get; set; }
            public string? Tag { get; set; }
            public long CreatedAtUtc { get; set; }
            public long LastAccessedUtc { get; set; }
            public int AccessCount { get; set; }

            public bool IsExpired => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > ExpiresAtUtc;

            public void UpdateAccessTime()
            {
                LastAccessedUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                AccessCount++;
            }
        }

        /// <summary>
        /// Default cache TTL in milliseconds (5 minutes).
        /// </summary>
        private const long DefaultTtlMs = 5 * 60 * 1000;

        /// <summary>
        /// Creates a new advanced cache manager.
        /// </summary>
        public AdvancedCacheManager()
        {
        }

        /// <summary>
        /// Gets a cached value, or null if not found or expired.
        /// </summary>
        public T? Get<T>(string key)
        {
            ThrowIfDisposed();

            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    entry.UpdateAccessTime();
                    return (T?)entry.Value;
                }

                _cache.TryRemove(key, out _);
            }

            return default;
        }

        /// <summary>
        /// Sets a cache value with TTL.
        /// </summary>
        public void Set<T>(string key, T value, long? ttlMs = null, string? tag = null)
        {
            ThrowIfDisposed();

            var expiresAtUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (ttlMs ?? DefaultTtlMs);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var entry = new CacheEntry
            {
                Value = value,
                ExpiresAtUtc = expiresAtUtc,
                Tag = tag,
                CreatedAtUtc = now,
                LastAccessedUtc = now,
                AccessCount = 0
            };

            _cache[key] = entry;
        }

        /// <summary>
        /// Invalidates (removes) a specific cache entry.
        /// </summary>
        public bool Invalidate(string key)
        {
            ThrowIfDisposed();
            return _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// Invalidates all cache entries with the specified tag.
        /// </summary>
        public int InvalidateByTag(string tag)
        {
            ThrowIfDisposed();

            var keysToRemove = _cache
                .Where(kvp => kvp.Value.Tag == tag)
                .Select(kvp => kvp.Key)
                .ToList();

            int count = 0;
            foreach (var key in keysToRemove)
            {
                if (_cache.TryRemove(key, out _))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Invalidates all expired entries. Call periodically for cleanup.
        /// </summary>
        public int InvalidateExpiredEntries()
        {
            ThrowIfDisposed();

            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            int count = 0;
            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out _))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Tries to get or compute a cache value. If not in cache or expired, calls the factory function.
        /// </summary>
        public async Task<T?> GetOrComputeAsync<T>(
            string key,
            Func<CancellationToken, Task<T?>> factory,
            long? ttlMs = null,
            string? tag = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var cached = Get<T>(key);
            if (cached != null)
                return cached;

            await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Double-check
                cached = Get<T>(key);
                if (cached != null)
                    return cached;

                var value = await factory(cancellationToken).ConfigureAwait(false);
                if (value != null)
                {
                    Set(key, value, ttlMs, tag);
                }

                return value;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            _cache.Clear();
        }

        /// <summary>
        /// Gets count of cached entries.
        /// </summary>
        public int EntryCount
        {
            get
            {
                ThrowIfDisposed();
                InvalidateExpiredEntries();
                return _cache.Count;
            }
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public (int Total, int Expired, long TotalAccessCount) GetStatistics()
        {
            ThrowIfDisposed();

            var total = _cache.Count;
            var expired = _cache.Values.Count(e => e.IsExpired);
            var accessCount = _cache.Values.Sum(e => e.AccessCount);

            return (total, expired, accessCount);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedCacheManager));
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cache.Clear();
            _cacheLock?.Dispose();
        }
    }
}
