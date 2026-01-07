using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// Manages rate limit budget for trading operations.
/// Implements token bucket algorithm with separate buckets for different operation types.
/// </summary>
public sealed class RateLimitBudget : IDisposable
{
    // Mercado Bitcoin API limits
    private const int GlobalLimitPerMinute = 500;
    private const int TradingLimitPerSecond = 3;
    private const int PublicDataLimitPerSecond = 1;
    private const int ListOrdersLimitPerSecond = 10;

    private readonly ILogger<RateLimitBudget>? _logger;
    private readonly SemaphoreSlim _tradingSemaphore;
    private readonly SemaphoreSlim _publicSemaphore;
    private readonly SemaphoreSlim _listOrdersSemaphore;
    private readonly Timer _replenishTimer;
    private readonly Timer _minuteResetTimer;

    private int _tradingTokens;
    private int _publicTokens;
    private int _listOrdersTokens;
    private long _globalUsedThisMinute;
    private bool _disposed;

    /// <summary>
    /// Event raised when rate limit is about to be exceeded.
    /// </summary>
    public event EventHandler<RateLimitWarningEventArgs>? RateLimitWarning;

    /// <summary>
    /// Event raised when a rate limit is hit.
    /// </summary>
    public event EventHandler<RateLimitHitEventArgs>? RateLimitHit;

    /// <summary>
    /// Creates a new rate limit budget manager.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public RateLimitBudget(ILogger<RateLimitBudget>? logger = null)
    {
        _logger = logger;

        _tradingSemaphore = new SemaphoreSlim(TradingLimitPerSecond, TradingLimitPerSecond);
        _publicSemaphore = new SemaphoreSlim(PublicDataLimitPerSecond, PublicDataLimitPerSecond);
        _listOrdersSemaphore = new SemaphoreSlim(ListOrdersLimitPerSecond, ListOrdersLimitPerSecond);

        _tradingTokens = TradingLimitPerSecond;
        _publicTokens = PublicDataLimitPerSecond;
        _listOrdersTokens = ListOrdersLimitPerSecond;

        // Replenish tokens every second
        _replenishTimer = new Timer(ReplenishTokens, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        // Reset global counter every minute
        _minuteResetTimer = new Timer(ResetMinuteCounter, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        _logger?.LogInformation(
            "RateLimitBudget initialized: Trading={Trading}/s, Public={Public}/s, ListOrders={ListOrders}/s, Global={Global}/min",
            TradingLimitPerSecond, PublicDataLimitPerSecond, ListOrdersLimitPerSecond, GlobalLimitPerMinute);
    }

    #region Properties

    /// <summary>
    /// Gets the available trading budget (orders per second).
    /// </summary>
    public int AvailableTradingBudget => _tradingSemaphore.CurrentCount;

    /// <summary>
    /// Gets the available public data budget (requests per second).
    /// </summary>
    public int AvailablePublicBudget => _publicSemaphore.CurrentCount;

    /// <summary>
    /// Gets the available list orders budget (requests per second).
    /// </summary>
    public int AvailableListOrdersBudget => _listOrdersSemaphore.CurrentCount;

    /// <summary>
    /// Gets the remaining global budget for the current minute.
    /// </summary>
    public int RemainingGlobalBudget => GlobalLimitPerMinute - (int)Interlocked.Read(ref _globalUsedThisMinute);

    /// <summary>
    /// Gets the global usage this minute.
    /// </summary>
    public long GlobalUsageThisMinute => Interlocked.Read(ref _globalUsedThisMinute);

    /// <summary>
    /// Gets whether trading operations are currently allowed.
    /// </summary>
    public bool CanTrade => AvailableTradingBudget > 0 && RemainingGlobalBudget > 0;

    /// <summary>
    /// Gets whether public data requests are currently allowed.
    /// </summary>
    public bool CanRequestPublicData => AvailablePublicBudget > 0 && RemainingGlobalBudget > 0;

    #endregion

    #region Acquire Methods

    /// <summary>
    /// Tries to acquire a token for a trading operation (place/cancel order).
    /// Returns false immediately if no budget available (non-blocking).
    /// </summary>
    /// <returns>True if token was acquired.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAcquireTrading()
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
        {
            OnRateLimitHit(RateLimitType.Global, "Global rate limit exceeded");
            return false;
        }

        if (!_tradingSemaphore.Wait(0))
        {
            OnRateLimitHit(RateLimitType.Trading, "Trading rate limit exceeded");
            return false;
        }

        Interlocked.Increment(ref _globalUsedThisMinute);
        CheckWarningThreshold();
        return true;
    }

    /// <summary>
    /// Acquires a token for a trading operation, waiting if necessary.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if token was acquired within the timeout.</returns>
    public async ValueTask<bool> AcquireTradingAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
        {
            OnRateLimitHit(RateLimitType.Global, "Global rate limit exceeded");
            return false;
        }

        if (!await _tradingSemaphore.WaitAsync(timeout, ct))
        {
            OnRateLimitHit(RateLimitType.Trading, "Trading rate limit exceeded (timeout)");
            return false;
        }

        Interlocked.Increment(ref _globalUsedThisMinute);
        CheckWarningThreshold();
        return true;
    }

    /// <summary>
    /// Tries to acquire a token for a public data request.
    /// Returns false immediately if no budget available (non-blocking).
    /// </summary>
    /// <returns>True if token was acquired.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAcquirePublic()
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
        {
            OnRateLimitHit(RateLimitType.Global, "Global rate limit exceeded");
            return false;
        }

        if (!_publicSemaphore.Wait(0))
        {
            OnRateLimitHit(RateLimitType.PublicData, "Public data rate limit exceeded");
            return false;
        }

        Interlocked.Increment(ref _globalUsedThisMinute);
        CheckWarningThreshold();
        return true;
    }

    /// <summary>
    /// Acquires a token for a public data request, waiting if necessary.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if token was acquired within the timeout.</returns>
    public async ValueTask<bool> AcquirePublicAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
        {
            OnRateLimitHit(RateLimitType.Global, "Global rate limit exceeded");
            return false;
        }

        if (!await _publicSemaphore.WaitAsync(timeout, ct))
        {
            OnRateLimitHit(RateLimitType.PublicData, "Public data rate limit exceeded (timeout)");
            return false;
        }

        Interlocked.Increment(ref _globalUsedThisMinute);
        CheckWarningThreshold();
        return true;
    }

    /// <summary>
    /// Tries to acquire a token for listing orders.
    /// Returns false immediately if no budget available (non-blocking).
    /// </summary>
    /// <returns>True if token was acquired.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAcquireListOrders()
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
        {
            OnRateLimitHit(RateLimitType.Global, "Global rate limit exceeded");
            return false;
        }

        if (!_listOrdersSemaphore.Wait(0))
        {
            OnRateLimitHit(RateLimitType.ListOrders, "List orders rate limit exceeded");
            return false;
        }

        Interlocked.Increment(ref _globalUsedThisMinute);
        CheckWarningThreshold();
        return true;
    }

    /// <summary>
    /// Acquires a token for listing orders, waiting if necessary.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if token was acquired within the timeout.</returns>
    public async ValueTask<bool> AcquireListOrdersAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
        {
            OnRateLimitHit(RateLimitType.Global, "Global rate limit exceeded");
            return false;
        }

        if (!await _listOrdersSemaphore.WaitAsync(timeout, ct))
        {
            OnRateLimitHit(RateLimitType.ListOrders, "List orders rate limit exceeded (timeout)");
            return false;
        }

        Interlocked.Increment(ref _globalUsedThisMinute);
        CheckWarningThreshold();
        return true;
    }

    #endregion

    #region Wait Methods

    /// <summary>
    /// Waits until trading budget is available.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async ValueTask WaitForTradingBudgetAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            if (TryAcquireTrading())
            {
                // Release immediately - we just wanted to wait
                _tradingSemaphore.Release();
                Interlocked.Decrement(ref _globalUsedThisMinute);
                return;
            }

            await Task.Delay(100, ct);
        }
    }

    /// <summary>
    /// Gets the estimated wait time until a trading token is available.
    /// </summary>
    /// <returns>Estimated wait time, or TimeSpan.Zero if available now.</returns>
    public TimeSpan EstimatedTradingWait()
    {
        if (CanTrade) return TimeSpan.Zero;

        // If global limit is hit, wait until minute reset
        if (RemainingGlobalBudget <= 0)
        {
            // Approximate time until minute reset
            return TimeSpan.FromSeconds(30); // Conservative estimate
        }

        // If trading limit is hit, wait 1 second for replenish
        return TimeSpan.FromSeconds(1);
    }

    #endregion

    #region Internal Methods

    private void ReplenishTokens(object? state)
    {
        if (_disposed) return;

        // Replenish trading tokens
        var tradingToRelease = TradingLimitPerSecond - _tradingSemaphore.CurrentCount;
        for (var i = 0; i < tradingToRelease; i++)
        {
            try { _tradingSemaphore.Release(); } catch { }
        }

        // Replenish public tokens
        var publicToRelease = PublicDataLimitPerSecond - _publicSemaphore.CurrentCount;
        for (var i = 0; i < publicToRelease; i++)
        {
            try { _publicSemaphore.Release(); } catch { }
        }

        // Replenish list orders tokens
        var listOrdersToRelease = ListOrdersLimitPerSecond - _listOrdersSemaphore.CurrentCount;
        for (var i = 0; i < listOrdersToRelease; i++)
        {
            try { _listOrdersSemaphore.Release(); } catch { }
        }
    }

    private void ResetMinuteCounter(object? state)
    {
        if (_disposed) return;

        var previousUsage = Interlocked.Exchange(ref _globalUsedThisMinute, 0);
        _logger?.LogDebug("Minute rate limit reset. Previous usage: {Usage}/{Limit}", previousUsage, GlobalLimitPerMinute);
    }

    private void CheckWarningThreshold()
    {
        var usage = Interlocked.Read(ref _globalUsedThisMinute);
        var threshold = GlobalLimitPerMinute * 0.8; // 80% threshold

        if (usage >= threshold && usage < GlobalLimitPerMinute)
        {
            OnRateLimitWarning(RateLimitType.Global, (int)usage, GlobalLimitPerMinute);
        }
    }

    private void OnRateLimitWarning(RateLimitType type, int currentUsage, int limit)
    {
        _logger?.LogWarning(
            "Rate limit warning: {Type} at {Usage}/{Limit} ({Percent}%)",
            type, currentUsage, limit, (currentUsage * 100) / limit);

        RateLimitWarning?.Invoke(this, new RateLimitWarningEventArgs
        {
            Type = type,
            CurrentUsage = currentUsage,
            Limit = limit
        });
    }

    private void OnRateLimitHit(RateLimitType type, string message)
    {
        _logger?.LogWarning("Rate limit hit: {Type} - {Message}", type, message);

        RateLimitHit?.Invoke(this, new RateLimitHitEventArgs
        {
            Type = type,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    /// <summary>
    /// Gets a summary of current rate limit status.
    /// </summary>
    public RateLimitStatus GetStatus()
    {
        return new RateLimitStatus
        {
            TradingAvailable = AvailableTradingBudget,
            TradingLimit = TradingLimitPerSecond,
            PublicAvailable = AvailablePublicBudget,
            PublicLimit = PublicDataLimitPerSecond,
            ListOrdersAvailable = AvailableListOrdersBudget,
            ListOrdersLimit = ListOrdersLimitPerSecond,
            GlobalUsed = (int)GlobalUsageThisMinute,
            GlobalLimit = GlobalLimitPerMinute
        };
    }

    /// <summary>
    /// Disposes the rate limit budget manager.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _replenishTimer.Dispose();
        _minuteResetTimer.Dispose();
        _tradingSemaphore.Dispose();
        _publicSemaphore.Dispose();
        _listOrdersSemaphore.Dispose();
    }
}

#region Supporting Types

/// <summary>
/// Type of rate limit.
/// </summary>
public enum RateLimitType
{
    /// <summary>Global rate limit (500/min).</summary>
    Global,
    /// <summary>Trading operations rate limit (3/s).</summary>
    Trading,
    /// <summary>Public data rate limit (1/s).</summary>
    PublicData,
    /// <summary>List orders rate limit (10/s).</summary>
    ListOrders
}

/// <summary>
/// Current rate limit status.
/// </summary>
public readonly record struct RateLimitStatus
{
    /// <summary>Available trading tokens.</summary>
    public required int TradingAvailable { get; init; }
    /// <summary>Trading limit per second.</summary>
    public required int TradingLimit { get; init; }
    /// <summary>Available public data tokens.</summary>
    public required int PublicAvailable { get; init; }
    /// <summary>Public data limit per second.</summary>
    public required int PublicLimit { get; init; }
    /// <summary>Available list orders tokens.</summary>
    public required int ListOrdersAvailable { get; init; }
    /// <summary>List orders limit per second.</summary>
    public required int ListOrdersLimit { get; init; }
    /// <summary>Global usage this minute.</summary>
    public required int GlobalUsed { get; init; }
    /// <summary>Global limit per minute.</summary>
    public required int GlobalLimit { get; init; }

    /// <summary>Global usage percentage.</summary>
    public int GlobalUsagePercent => GlobalLimit > 0 ? (GlobalUsed * 100) / GlobalLimit : 0;
}

/// <summary>
/// Event arguments for rate limit warning.
/// </summary>
public sealed class RateLimitWarningEventArgs : EventArgs
{
    /// <summary>Type of rate limit.</summary>
    public required RateLimitType Type { get; init; }
    /// <summary>Current usage.</summary>
    public required int CurrentUsage { get; init; }
    /// <summary>Limit.</summary>
    public required int Limit { get; init; }
}

/// <summary>
/// Event arguments for rate limit hit.
/// </summary>
public sealed class RateLimitHitEventArgs : EventArgs
{
    /// <summary>Type of rate limit.</summary>
    public required RateLimitType Type { get; init; }
    /// <summary>Message describing the rate limit hit.</summary>
    public required string Message { get; init; }
    /// <summary>When the rate limit was hit.</summary>
    public required DateTime Timestamp { get; init; }
}

#endregion
