using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.RateLimiting;

namespace MercadoBitcoin.Client.Internal.RateLimiting
{
    /// <summary>
    /// Per-account rate limiter using TokenBucket algorithm.
    /// Each account maintains its own rate limit quota to prevent one account from consuming all tokens.
    /// </summary>
    public sealed class PerAccountRateLimiter : IDisposable
    {
        private readonly ConcurrentDictionary<string, TokenBucketRateLimiter> _limiters = new();
        private readonly TokenBucketRateLimiterOptions _options;
        private volatile bool _disposed;

        /// <summary>
        /// Creates a per-account rate limiter with shared configuration.
        /// </summary>
        public PerAccountRateLimiter(TokenBucketRateLimiterOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Acquires a rate limit token for the specified account.
        /// Returns true if permit was granted, false otherwise.
        /// </summary>
        public bool TryAcquire(string accountId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));

            var limiter = _limiters.GetOrAdd(accountId, _ => new TokenBucketRateLimiter(_options));
            using var lease = limiter.AttemptAcquire(1);
            return lease != null;
        }

        /// <summary>
        /// Asynchronously acquires a rate limit token for the specified account.
        /// </summary>
        public async ValueTask<RateLimitLease> AcquireAsync(string accountId, int tokenCount = 1, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));

            var limiter = _limiters.GetOrAdd(accountId, _ => new TokenBucketRateLimiter(_options));
            return await limiter.AcquireAsync(tokenCount, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a rate limiter for the specified account (typically on logout/session termination).
        /// </summary>
        public void RemoveLimiter(string accountId)
        {
            ThrowIfDisposed();
            _limiters.TryRemove(accountId, out var limiter);
            limiter?.Dispose();
        }

        /// <summary>
        /// Gets count of active limiters.
        /// </summary>
        public int ActiveLimiterCount => _limiters.Count;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PerAccountRateLimiter));
        }

        /// <summary>
        /// Disposes all limiters and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var limiter in _limiters.Values)
            {
                limiter.Dispose();
            }

            _limiters.Clear();
        }
    }
}
