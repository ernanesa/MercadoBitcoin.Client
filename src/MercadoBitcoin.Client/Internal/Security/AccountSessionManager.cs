using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MercadoBitcoin.Client.Internal.Security
{
    /// <summary>
    /// Manages isolated session contexts for multiple user accounts.
    /// Each account has its own token store, rate limiter, and session metadata.
    /// Implements account isolation to prevent token leakage or cross-account pollution.
    /// </summary>
    public sealed class AccountSessionManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, AccountSession> _sessions = new();
        private readonly SemaphoreSlim _sessionLock = new(1, 1);
        private volatile bool _disposed;

        /// <summary>
        /// Represents a single account session with isolated security context.
        /// </summary>
        public sealed class AccountSession
        {
            /// <summary>
            /// Unique identifier for the account.
            /// </summary>
            public string AccountId { get; }

            /// <summary>
            /// Token store isolated to this account.
            /// </summary>
            public TokenStore TokenStore { get; }

            /// <summary>
            /// Session creation timestamp.
            /// </summary>
            public long CreatedAtUtc { get; }

            /// <summary>
            /// Last activity timestamp (access time).
            /// </summary>
            public long LastAccessedUtc { get; set; }

            /// <summary>
            /// Session metadata.
            /// </summary>
            public Dictionary<string, object> Metadata { get; }

            /// <summary>
            /// Session expiration timestamp. Null = never expires.
            /// </summary>
            public long? ExpiresAtUtc { get; set; }

            /// <summary>
            /// Creates a new isolated account session.
            /// </summary>
            public AccountSession(string accountId, long? expiresAtUtc = null)
            {
                AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
                TokenStore = new TokenStore();
                CreatedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                LastAccessedUtc = CreatedAtUtc;
                ExpiresAtUtc = expiresAtUtc;
                Metadata = new Dictionary<string, object>();
            }

            /// <summary>
            /// Checks if the session has expired.
            /// </summary>
            public bool IsExpired => ExpiresAtUtc.HasValue && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > ExpiresAtUtc;

            /// <summary>
            /// Updates the last accessed timestamp to track activity.
            /// </summary>
            public void UpdateAccessTime()
            {
                LastAccessedUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// Gets or creates a session for the specified account ID.
        /// </summary>
        public async Task<AccountSession> GetOrCreateSessionAsync(string accountId, long? expiresAtUtc = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));

            if (_sessions.TryGetValue(accountId, out var existingSession))
            {
                if (!existingSession.IsExpired)
                {
                    existingSession.UpdateAccessTime();
                    return existingSession;
                }

                // Session expired, remove it
                _sessions.TryRemove(accountId, out _);
            }

            await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Double-check pattern
                if (_sessions.TryGetValue(accountId, out var existingSession2))
                {
                    if (!existingSession2.IsExpired)
                    {
                        existingSession2.UpdateAccessTime();
                        return existingSession2;
                    }

                    _sessions.TryRemove(accountId, out _);
                }

                var newSession = new AccountSession(accountId, expiresAtUtc);
                _sessions[accountId] = newSession;
                return newSession;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Gets an existing session without creating one.
        /// </summary>
        public AccountSession? GetSession(string accountId)
        {
            ThrowIfDisposed();

            if (_sessions.TryGetValue(accountId, out var session))
            {
                if (!session.IsExpired)
                {
                    session.UpdateAccessTime();
                    return session;
                }

                _sessions.TryRemove(accountId, out _);
            }

            return null;
        }

        /// <summary>
        /// Terminates a session immediately.
        /// </summary>
        public async Task TerminateSessionAsync(string accountId, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _sessions.TryRemove(accountId, out _);
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Invalidates all expired sessions. Call periodically for cleanup.
        /// </summary>
        public void InvalidateExpiredSessions()
        {
            ThrowIfDisposed();

            var expiredKeys = _sessions
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _sessions.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Gets all active session IDs.
        /// </summary>
        public IReadOnlyList<string> GetActiveSessions()
        {
            ThrowIfDisposed();
            InvalidateExpiredSessions();
            return _sessions.Keys.ToList();
        }

        /// <summary>
        /// Gets count of active sessions.
        /// </summary>
        public int ActiveSessionCount
        {
            get
            {
                ThrowIfDisposed();
                InvalidateExpiredSessions();
                return _sessions.Count;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AccountSessionManager));
        }

        /// <summary>
        /// Disposes all sessions and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _sessions.Clear();
            _sessionLock?.Dispose();
        }
    }
}
