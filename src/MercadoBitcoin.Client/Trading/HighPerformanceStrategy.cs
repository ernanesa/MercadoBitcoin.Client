using System.Diagnostics;
using System.Runtime.CompilerServices;
using MercadoBitcoin.Client.Generated;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// Base class for high-performance trading strategies.
/// Provides infrastructure for real-time market data processing, order management,
/// and performance monitoring.
/// </summary>
/// <remarks>
/// This class is designed for trading algorithmic strategies that operate within
/// the rate limits and latency constraints of the Mercado Bitcoin API.
/// True HFT is not possible due to API limitations.
/// </remarks>
public abstract class HighPerformanceStrategy : IAsyncDisposable
{
    /// <summary>
    /// Market data provider for real-time price updates.
    /// </summary>
    protected HighPerformanceMarketData MarketData { get; }

    /// <summary>
    /// Order manager for placing and cancelling orders.
    /// </summary>
    protected HighPerformanceOrderManager OrderManager { get; }

    /// <summary>
    /// Rate limit budget tracker.
    /// </summary>
    protected RateLimitBudget RateLimitBudget { get; }

    /// <summary>
    /// The trading symbol (e.g., "BTC-BRL").
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Optional logger instance.
    /// </summary>
    protected ILogger? Logger { get; }

    private readonly CancellationTokenSource _cts = new();
    private readonly StrategyOptions _options;
    private Task? _runTask;

    // Performance metrics
    private long _ticksProcessed;
    private long _ordersPlaced;
    private long _ordersCancelled;
    private long _totalTickLatencyTicks;
    private long _slowTickCount;
    private readonly Stopwatch _latencyWatch = new();
    private DateTime _startTime;
    private DateTime _lastTickTime;

    /// <summary>
    /// Creates a new instance of HighPerformanceStrategy.
    /// </summary>
    /// <param name="marketData">Market data provider.</param>
    /// <param name="orderManager">Order manager.</param>
    /// <param name="rateLimitBudget">Rate limit budget tracker.</param>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="options">Strategy options.</param>
    /// <param name="logger">Optional logger.</param>
    protected HighPerformanceStrategy(
        HighPerformanceMarketData marketData,
        HighPerformanceOrderManager orderManager,
        RateLimitBudget rateLimitBudget,
        string symbol,
        StrategyOptions? options = null,
        ILogger? logger = null)
    {
        MarketData = marketData ?? throw new ArgumentNullException(nameof(marketData));
        OrderManager = orderManager ?? throw new ArgumentNullException(nameof(orderManager));
        RateLimitBudget = rateLimitBudget ?? throw new ArgumentNullException(nameof(rateLimitBudget));
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        _options = options ?? new StrategyOptions();
        Logger = logger;
    }

    #region Properties

    /// <summary>
    /// Gets whether the strategy is currently running.
    /// </summary>
    public bool IsRunning => _runTask != null && !_runTask.IsCompleted;

    /// <summary>
    /// Gets the number of ticks processed.
    /// </summary>
    public long TicksProcessed => Volatile.Read(ref _ticksProcessed);

    /// <summary>
    /// Gets the number of orders placed.
    /// </summary>
    public long OrdersPlaced => Volatile.Read(ref _ordersPlaced);

    /// <summary>
    /// Gets the number of orders cancelled.
    /// </summary>
    public long OrdersCancelled => Volatile.Read(ref _ordersCancelled);

    /// <summary>
    /// Gets the number of slow ticks (exceeding threshold).
    /// </summary>
    public long SlowTickCount => Volatile.Read(ref _slowTickCount);

    /// <summary>
    /// Gets the average tick processing latency in microseconds.
    /// </summary>
    public double AverageTickLatencyMicroseconds
    {
        get
        {
            var ticks = Volatile.Read(ref _ticksProcessed);
            if (ticks == 0) return 0;
            var totalTicks = Volatile.Read(ref _totalTickLatencyTicks);
            return (double)totalTicks / ticks / (Stopwatch.Frequency / 1_000_000.0);
        }
    }

    /// <summary>
    /// Gets the strategy uptime.
    /// </summary>
    public TimeSpan Uptime => IsRunning ? DateTime.UtcNow - _startTime : TimeSpan.Zero;

    /// <summary>
    /// Gets the time since the last tick was processed.
    /// </summary>
    public TimeSpan TimeSinceLastTick => DateTime.UtcNow - _lastTickTime;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Starts the strategy.
    /// </summary>
    public void Start()
    {
        if (_runTask != null)
        {
            throw new InvalidOperationException("Strategy is already running.");
        }

        Logger?.LogInformation("Starting strategy for {Symbol}", Symbol);
        _startTime = DateTime.UtcNow;
        _runTask = RunAsync(_cts.Token);
    }

    /// <summary>
    /// Stops the strategy gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the stop operation.</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_runTask == null) return;

        Logger?.LogInformation("Stopping strategy for {Symbol}", Symbol);

        _cts.Cancel();

        try
        {
            await _runTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Logger?.LogInformation(
            "Strategy stopped. Processed {Ticks} ticks, placed {Orders} orders",
            _ticksProcessed, _ordersPlaced);
    }

    /// <summary>
    /// Main run loop for the strategy.
    /// </summary>
    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            // Allow strategy-specific warm-up
            await WarmUpAsync(ct);

            Logger?.LogInformation("Strategy warm-up complete, starting main loop");

            // Subscribe to ticker updates
            await foreach (var update in MarketData.TickerUpdates.ReadAllAsync(ct))
            {
                if (update.Symbol != Symbol) continue;

                _latencyWatch.Restart();

                try
                {
                    await OnTickAsync(update, ct);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
                finally
                {
                    _latencyWatch.Stop();
                    Interlocked.Increment(ref _ticksProcessed);
                    Interlocked.Add(ref _totalTickLatencyTicks, _latencyWatch.ElapsedTicks);
                    _lastTickTime = DateTime.UtcNow;

                    var elapsedMs = _latencyWatch.ElapsedMilliseconds;
                    if (elapsedMs > _options.SlowTickThresholdMs)
                    {
                        Interlocked.Increment(ref _slowTickCount);
                        OnSlowTick(elapsedMs);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Logger?.LogDebug("Strategy loop cancelled");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Fatal error in strategy loop");
            OnFatalError(ex);
            throw;
        }
        finally
        {
            // Cleanup
            await OnStopAsync(ct);
        }
    }

    #endregion

    #region Abstract and Virtual Methods

    /// <summary>
    /// Called once before the main loop starts. Override for strategy-specific initialization.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task WarmUpAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called for each market data tick. This is the main strategy logic.
    /// </summary>
    /// <param name="update">The ticker update.</param>
    /// <param name="ct">Cancellation token.</param>
    protected abstract Task OnTickAsync(TickerUpdate update, CancellationToken ct);

    /// <summary>
    /// Called when the strategy is stopping. Override for cleanup.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnStopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a non-fatal error occurs during tick processing.
    /// </summary>
    /// <param name="ex">The exception.</param>
    protected virtual void OnError(Exception ex)
    {
        Logger?.LogError(ex, "Error processing tick for {Symbol}", Symbol);
    }

    /// <summary>
    /// Called when a fatal error occurs that stops the strategy.
    /// </summary>
    /// <param name="ex">The exception.</param>
    protected virtual void OnFatalError(Exception ex)
    {
        Logger?.LogCritical(ex, "Fatal error in strategy for {Symbol}", Symbol);
    }

    /// <summary>
    /// Called when tick processing exceeds the slow tick threshold.
    /// </summary>
    /// <param name="elapsedMs">The processing time in milliseconds.</param>
    protected virtual void OnSlowTick(long elapsedMs)
    {
        Logger?.LogWarning(
            "Slow tick detected for {Symbol}: {Elapsed}ms (threshold: {Threshold}ms)",
            Symbol, elapsedMs, _options.SlowTickThresholdMs);
    }

    #endregion

    #region Order Helpers

    /// <summary>
    /// Places a buy order with rate limit checking.
    /// </summary>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="limitPrice">Limit price.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response or null if rate limited.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async Task<PlaceOrderResponse?> PlaceBuyAsync(
        decimal quantity,
        decimal limitPrice,
        CancellationToken ct)
    {
        if (!RateLimitBudget.TryAcquireTrading())
        {
            Logger?.LogWarning("Rate limit exceeded, skipping buy order");
            return null;
        }

        var result = await OrderManager.PlaceBuyOrderAsync(Symbol, quantity, limitPrice, null, ct);
        if (result != null)
        {
            Interlocked.Increment(ref _ordersPlaced);
        }
        return result;
    }

    /// <summary>
    /// Places a sell order with rate limit checking.
    /// </summary>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="limitPrice">Limit price.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response or null if rate limited.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async Task<PlaceOrderResponse?> PlaceSellAsync(
        decimal quantity,
        decimal limitPrice,
        CancellationToken ct)
    {
        if (!RateLimitBudget.TryAcquireTrading())
        {
            Logger?.LogWarning("Rate limit exceeded, skipping sell order");
            return null;
        }

        var result = await OrderManager.PlaceSellOrderAsync(Symbol, quantity, limitPrice, null, ct);
        if (result != null)
        {
            Interlocked.Increment(ref _ordersPlaced);
        }
        return result;
    }

    /// <summary>
    /// Places a market buy order with rate limit checking.
    /// </summary>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response or null if rate limited.</returns>
    protected async Task<PlaceOrderResponse?> PlaceMarketBuyAsync(
        decimal quantity,
        CancellationToken ct)
    {
        if (!RateLimitBudget.TryAcquireTrading())
        {
            Logger?.LogWarning("Rate limit exceeded, skipping market buy order");
            return null;
        }

        var result = await OrderManager.PlaceMarketBuyAsync(Symbol, quantity, null, ct);
        if (result != null)
        {
            Interlocked.Increment(ref _ordersPlaced);
        }
        return result;
    }

    /// <summary>
    /// Places a market sell order with rate limit checking.
    /// </summary>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response or null if rate limited.</returns>
    protected async Task<PlaceOrderResponse?> PlaceMarketSellAsync(
        decimal quantity,
        CancellationToken ct)
    {
        if (!RateLimitBudget.TryAcquireTrading())
        {
            Logger?.LogWarning("Rate limit exceeded, skipping market sell order");
            return null;
        }

        var result = await OrderManager.PlaceMarketSellAsync(Symbol, quantity, null, ct);
        if (result != null)
        {
            Interlocked.Increment(ref _ordersPlaced);
        }
        return result;
    }

    /// <summary>
    /// Cancels an order with rate limit checking.
    /// </summary>
    /// <param name="orderId">The order ID to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if cancelled successfully.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async Task<bool> CancelAsync(string orderId, CancellationToken ct)
    {
        if (!RateLimitBudget.TryAcquireTrading())
        {
            Logger?.LogWarning("Rate limit exceeded, skipping cancel");
            return false;
        }

        var success = await OrderManager.CancelOrderFastAsync(Symbol, orderId);
        if (success)
        {
            Interlocked.Increment(ref _ordersCancelled);
        }
        return success;
    }

    #endregion

    #region Market Data Helpers

    /// <summary>
    /// Gets the current spread for the symbol.
    /// </summary>
    /// <returns>Spread or null if unavailable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected decimal? GetSpread()
    {
        return MarketData.GetSpread(Symbol);
    }

    /// <summary>
    /// Gets the current mid price for the symbol.
    /// </summary>
    /// <returns>Mid price or null if unavailable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected decimal? GetMidPrice()
    {
        return MarketData.GetMidPrice(Symbol);
    }

    /// <summary>
    /// Gets the current best bid for the symbol.
    /// </summary>
    /// <returns>Best bid price or null if unavailable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected decimal? GetBestBid()
    {
        return MarketData.GetBestBid(Symbol);
    }

    /// <summary>
    /// Gets the current best ask for the symbol.
    /// </summary>
    /// <returns>Best ask price or null if unavailable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected decimal? GetBestAsk()
    {
        return MarketData.GetBestAsk(Symbol);
    }

    /// <summary>
    /// Gets the last traded price for the symbol.
    /// </summary>
    /// <returns>Last price or null if unavailable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected decimal? GetLastPrice()
    {
        return MarketData.GetLastPrice(Symbol);
    }

    /// <summary>
    /// Gets the latest ticker snapshot for the symbol.
    /// </summary>
    /// <returns>Ticker snapshot or null if unavailable.</returns>
    protected TickerSnapshot? GetTicker()
    {
        return MarketData.TryGetLastTicker(Symbol, out var ticker) ? ticker : null;
    }

    /// <summary>
    /// Gets the latest order book snapshot for the symbol.
    /// </summary>
    /// <returns>Order book snapshot or null if unavailable.</returns>
    protected OrderBookSnapshot? GetOrderBook()
    {
        return MarketData.TryGetLastOrderBook(Symbol, out var orderBook) ? orderBook : null;
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets the current strategy statistics.
    /// </summary>
    /// <returns>Strategy statistics.</returns>
    public StrategyStats GetStats()
    {
        return new StrategyStats
        {
            Symbol = Symbol,
            IsRunning = IsRunning,
            Uptime = Uptime,
            TicksProcessed = _ticksProcessed,
            OrdersPlaced = _ordersPlaced,
            OrdersCancelled = _ordersCancelled,
            SlowTickCount = _slowTickCount,
            AverageTickLatencyMicroseconds = AverageTickLatencyMicroseconds,
            TimeSinceLastTick = TimeSinceLastTick,
            AvailableTradingBudget = RateLimitBudget.AvailableTradingBudget
        };
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the strategy resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_cts.IsCancellationRequested)
        {
            await StopAsync();
        }

        _cts.Dispose();
    }

    #endregion
}

#region Options and Models

/// <summary>
/// Configuration options for high-performance strategies.
/// </summary>
public sealed class StrategyOptions
{
    /// <summary>
    /// Threshold in milliseconds for considering a tick as slow. Default: 10ms.
    /// </summary>
    public long SlowTickThresholdMs { get; set; } = 10;

    /// <summary>
    /// Maximum time to wait for warm-up. Default: 30 seconds.
    /// </summary>
    public TimeSpan WarmUpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to log each tick. Default: false (too verbose).
    /// </summary>
    public bool LogTicks { get; set; } = false;

    /// <summary>
    /// Whether to log each order. Default: true.
    /// </summary>
    public bool LogOrders { get; set; } = true;
}

/// <summary>
/// Strategy execution statistics.
/// </summary>
public sealed class StrategyStats
{
    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Whether the strategy is running.
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// Strategy uptime.
    /// </summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>
    /// Number of ticks processed.
    /// </summary>
    public long TicksProcessed { get; init; }

    /// <summary>
    /// Number of orders placed.
    /// </summary>
    public long OrdersPlaced { get; init; }

    /// <summary>
    /// Number of orders cancelled.
    /// </summary>
    public long OrdersCancelled { get; init; }

    /// <summary>
    /// Number of slow ticks.
    /// </summary>
    public long SlowTickCount { get; init; }

    /// <summary>
    /// Average tick processing latency in microseconds.
    /// </summary>
    public double AverageTickLatencyMicroseconds { get; init; }

    /// <summary>
    /// Time since the last tick was processed.
    /// </summary>
    public TimeSpan TimeSinceLastTick { get; init; }

    /// <summary>
    /// Available trading rate limit budget.
    /// </summary>
    public int AvailableTradingBudget { get; init; }

    /// <summary>
    /// Ticks per second throughput.
    /// </summary>
    public double TicksPerSecond => Uptime.TotalSeconds > 0 ? TicksProcessed / Uptime.TotalSeconds : 0;

    /// <summary>
    /// Orders per minute throughput.
    /// </summary>
    public double OrdersPerMinute => Uptime.TotalMinutes > 0 ? OrdersPlaced / Uptime.TotalMinutes : 0;
}

#endregion

#region Example Implementations

/// <summary>
/// Simple market maker strategy example.
/// Places orders on both sides of the spread and maintains position limits.
/// </summary>
/// <remarks>
/// This is an example implementation for educational purposes.
/// Real trading strategies require extensive testing and risk management.
/// </remarks>
public sealed class SimpleMarketMakerStrategy : HighPerformanceStrategy
{
    private readonly decimal _spreadMultiplier;
    private readonly decimal _orderSize;
    private readonly decimal _maxPosition;

    private decimal _position;
    private string? _activeBuyOrderId;
    private string? _activeSellOrderId;
    private decimal _lastMidPrice;

    /// <summary>
    /// Creates a new SimpleMarketMakerStrategy.
    /// </summary>
    public SimpleMarketMakerStrategy(
        HighPerformanceMarketData marketData,
        HighPerformanceOrderManager orderManager,
        RateLimitBudget rateLimitBudget,
        string symbol,
        decimal spreadMultiplier = 1.001m,
        decimal orderSize = 0.001m,
        decimal maxPosition = 0.01m,
        ILogger? logger = null)
        : base(marketData, orderManager, rateLimitBudget, symbol, null, logger)
    {
        _spreadMultiplier = spreadMultiplier;
        _orderSize = orderSize;
        _maxPosition = maxPosition;
    }

    /// <summary>
    /// Current position.
    /// </summary>
    public decimal Position => _position;

    /// <inheritdoc />
    protected override async Task OnTickAsync(TickerUpdate update, CancellationToken ct)
    {
        var midPrice = update.Snapshot.MidPrice;
        if (midPrice <= 0) return;

        // Only update quotes if price moved significantly
        if (Math.Abs(midPrice - _lastMidPrice) / _lastMidPrice < 0.0001m)
        {
            return;
        }

        _lastMidPrice = midPrice;
        await UpdateQuotesAsync(midPrice, ct);
    }

    private async Task UpdateQuotesAsync(decimal midPrice, CancellationToken ct)
    {
        // Calculate bid/ask prices
        var bidPrice = midPrice / _spreadMultiplier;
        var askPrice = midPrice * _spreadMultiplier;

        // Cancel existing orders
        if (_activeBuyOrderId != null)
        {
            await CancelAsync(_activeBuyOrderId, ct);
            _activeBuyOrderId = null;
        }

        if (_activeSellOrderId != null)
        {
            await CancelAsync(_activeSellOrderId, ct);
            _activeSellOrderId = null;
        }

        // Place new orders if within position limits
        if (_position < _maxPosition)
        {
            var buyResponse = await PlaceBuyAsync(_orderSize, bidPrice, ct);
            _activeBuyOrderId = buyResponse?.OrderId;
        }

        if (_position > -_maxPosition)
        {
            var sellResponse = await PlaceSellAsync(_orderSize, askPrice, ct);
            _activeSellOrderId = sellResponse?.OrderId;
        }
    }

    /// <summary>
    /// Updates position when an order is filled.
    /// Call this from an order fill callback.
    /// </summary>
    /// <param name="orderId">The filled order ID.</param>
    /// <param name="side">The order side (buy/sell).</param>
    /// <param name="quantity">The filled quantity.</param>
    public void OnOrderFilled(string orderId, string side, decimal quantity)
    {
        if (side.Equals("buy", StringComparison.OrdinalIgnoreCase))
        {
            _position += quantity;
            if (_activeBuyOrderId == orderId)
            {
                _activeBuyOrderId = null;
            }
        }
        else
        {
            _position -= quantity;
            if (_activeSellOrderId == orderId)
            {
                _activeSellOrderId = null;
            }
        }
    }
}

#endregion
