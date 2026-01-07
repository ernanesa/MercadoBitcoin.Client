using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Internal.Security;

/// <summary>
/// Manages proactive token refresh to prevent authentication failures.
/// Refreshes the token before it expires, avoiding 401 errors during trading.
/// </summary>
public sealed class ProactiveTokenRefresher : IDisposable
{
    private readonly TokenStore _tokenStore;
    private readonly ILogger<ProactiveTokenRefresher>? _logger;
    private readonly TimeSpan _refreshBefore;
    private readonly Timer _refreshTimer;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private Func<CancellationToken, Task<(string Token, long Expiration)>>? _refreshCallback;
    private long _currentExpiration;
    private int _refreshAttempts;
    private bool _disposed;

    /// <summary>
    /// Event raised when token is refreshed successfully.
    /// </summary>
    public event EventHandler<TokenRefreshedEventArgs>? TokenRefreshed;

    /// <summary>
    /// Event raised when token refresh fails.
    /// </summary>
    public event EventHandler<TokenRefreshFailedEventArgs>? TokenRefreshFailed;

    /// <summary>
    /// Creates a new instance of ProactiveTokenRefresher.
    /// </summary>
    /// <param name="tokenStore">The token store to update.</param>
    /// <param name="refreshBefore">How long before expiration to refresh. Default: 5 minutes.</param>
    /// <param name="logger">Optional logger.</param>
    public ProactiveTokenRefresher(
        TokenStore tokenStore,
        TimeSpan? refreshBefore = null,
        ILogger<ProactiveTokenRefresher>? logger = null)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _refreshBefore = refreshBefore ?? TimeSpan.FromMinutes(5);
        _logger = logger;
        _refreshTimer = new Timer(OnRefreshTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Gets whether a token refresh is currently scheduled.
    /// </summary>
    public bool IsRefreshScheduled => Volatile.Read(ref _currentExpiration) > 0;

    /// <summary>
    /// Gets the number of refresh attempts made.
    /// </summary>
    public int RefreshAttempts => Volatile.Read(ref _refreshAttempts);

    /// <summary>
    /// Gets the time until the next scheduled refresh, or null if not scheduled.
    /// </summary>
    public TimeSpan? TimeUntilRefresh
    {
        get
        {
            var expiration = Volatile.Read(ref _currentExpiration);
            if (expiration <= 0) return null;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiration);
            var refreshAt = expiresAt - _refreshBefore;
            var remaining = refreshAt - DateTimeOffset.UtcNow;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Sets the callback function used to refresh the token.
    /// </summary>
    /// <param name="callback">Function that returns new token and expiration timestamp.</param>
    public void SetRefreshCallback(Func<CancellationToken, Task<(string Token, long Expiration)>> callback)
    {
        _refreshCallback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Schedules a token refresh based on the expiration timestamp.
    /// </summary>
    /// <param name="expirationTimestamp">Unix timestamp when the token expires.</param>
    public void ScheduleRefresh(long expirationTimestamp)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (expirationTimestamp <= 0)
        {
            _logger?.LogWarning("Invalid expiration timestamp: {Timestamp}", expirationTimestamp);
            return;
        }

        Volatile.Write(ref _currentExpiration, expirationTimestamp);

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expirationTimestamp);
        var refreshAt = expiresAt - _refreshBefore;
        var delay = refreshAt - DateTimeOffset.UtcNow;

        if (delay <= TimeSpan.Zero)
        {
            // Token already needs refresh or is expired
            _logger?.LogWarning("Token already needs refresh. Expires at: {ExpiresAt}, Refresh window: {RefreshBefore}",
                expiresAt, _refreshBefore);

            // Schedule immediate refresh
            _refreshTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
        else
        {
            _logger?.LogInformation(
                "Scheduled token refresh in {Delay}. Token expires at {ExpiresAt}",
                delay, expiresAt);

            _refreshTimer.Change(delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Cancels any scheduled refresh.
    /// </summary>
    public void CancelScheduledRefresh()
    {
        _refreshTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        Volatile.Write(ref _currentExpiration, 0);
        _logger?.LogDebug("Cancelled scheduled token refresh");
    }

    /// <summary>
    /// Forces an immediate token refresh.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if refresh was successful.</returns>
    public async Task<bool> ForceRefreshAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_refreshCallback is null)
        {
            _logger?.LogError("Cannot refresh token: no refresh callback configured");
            return false;
        }

        return await RefreshTokenInternalAsync(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async void OnRefreshTimerCallback(object? state)
    {
        if (_disposed) return;

        try
        {
            await RefreshTokenInternalAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unhandled exception in token refresh callback");
        }
    }

    private async Task<bool> RefreshTokenInternalAsync(CancellationToken cancellationToken)
    {
        if (_refreshCallback is null)
        {
            _logger?.LogError("Cannot refresh token: no refresh callback configured");
            return false;
        }

        var acquired = await _refreshLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        if (!acquired)
        {
            _logger?.LogWarning("Token refresh already in progress");
            return false;
        }

        try
        {
            Interlocked.Increment(ref _refreshAttempts);

            _logger?.LogDebug("Starting proactive token refresh (attempt #{Attempt})", _refreshAttempts);
            var startTime = DateTime.UtcNow;

            var (newToken, newExpiration) = await _refreshCallback(cancellationToken);

            if (string.IsNullOrEmpty(newToken))
            {
                _logger?.LogError("Token refresh returned empty token");
                OnTokenRefreshFailed("Refresh callback returned empty token", null);
                return false;
            }

            // Update the token store
            _tokenStore.AccessToken = newToken;

            var refreshDuration = DateTime.UtcNow - startTime;
            _logger?.LogInformation(
                "Token refreshed successfully in {Duration}ms. New expiration: {Expiration}",
                refreshDuration.TotalMilliseconds,
                DateTimeOffset.FromUnixTimeSeconds(newExpiration));

            // Schedule next refresh
            ScheduleRefresh(newExpiration);

            // Raise event
            OnTokenRefreshed(newExpiration, refreshDuration);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogDebug("Token refresh cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Token refresh failed");
            OnTokenRefreshFailed(ex.Message, ex);

            // Schedule retry with exponential backoff
            var retryDelay = CalculateRetryDelay(_refreshAttempts);
            _logger?.LogInformation("Scheduling retry in {Delay}", retryDelay);
            _refreshTimer.Change(retryDelay, Timeout.InfiniteTimeSpan);

            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        // Exponential backoff: 5s, 10s, 20s, 40s, max 60s
        var delay = TimeSpan.FromSeconds(Math.Min(5 * Math.Pow(2, attempt - 1), 60));

        // Add jitter (0-1000ms)
        var jitter = Random.Shared.Next(0, 1000);
        return delay + TimeSpan.FromMilliseconds(jitter);
    }

    private void OnTokenRefreshed(long newExpiration, TimeSpan duration)
    {
        TokenRefreshed?.Invoke(this, new TokenRefreshedEventArgs
        {
            NewExpiration = DateTimeOffset.FromUnixTimeSeconds(newExpiration),
            RefreshDuration = duration,
            AttemptNumber = _refreshAttempts
        });
    }

    private void OnTokenRefreshFailed(string error, Exception? exception)
    {
        TokenRefreshFailed?.Invoke(this, new TokenRefreshFailedEventArgs
        {
            Error = error,
            Exception = exception,
            AttemptNumber = _refreshAttempts,
            WillRetry = true
        });
    }

    /// <summary>
    /// Disposes the refresher and cancels any scheduled refresh.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _refreshTimer.Dispose();
        _refreshLock.Dispose();
    }
}

/// <summary>
/// Event arguments for successful token refresh.
/// </summary>
public sealed class TokenRefreshedEventArgs : EventArgs
{
    /// <summary>
    /// The new token expiration time.
    /// </summary>
    public required DateTimeOffset NewExpiration { get; init; }

    /// <summary>
    /// How long the refresh operation took.
    /// </summary>
    public required TimeSpan RefreshDuration { get; init; }

    /// <summary>
    /// The attempt number that succeeded.
    /// </summary>
    public required int AttemptNumber { get; init; }
}

/// <summary>
/// Event arguments for failed token refresh.
/// </summary>
public sealed class TokenRefreshFailedEventArgs : EventArgs
{
    /// <summary>
    /// The error message.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// The exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// The attempt number that failed.
    /// </summary>
    public required int AttemptNumber { get; init; }

    /// <summary>
    /// Whether a retry will be attempted.
    /// </summary>
    public required bool WillRetry { get; init; }
}

/// <summary>
/// Configuration options for proactive token refresh.
/// </summary>
public sealed class ProactiveTokenRefreshOptions
{
    /// <summary>
    /// How long before token expiration to trigger refresh. Default: 5 minutes.
    /// </summary>
    public TimeSpan RefreshBefore { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether proactive refresh is enabled. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts before giving up. Default: 5.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Base delay for retry backoff. Default: 5 seconds.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum delay for retry backoff. Default: 60 seconds.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(60);
}
