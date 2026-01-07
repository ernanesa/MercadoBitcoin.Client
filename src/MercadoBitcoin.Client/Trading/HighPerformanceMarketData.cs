using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MercadoBitcoin.Client.Diagnostics;
using MercadoBitcoin.Client.WebSocket;
using MercadoBitcoin.Client.WebSocket.Messages;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// High-performance market data aggregator for real-time price feeds.
/// Uses lock-free data structures and channels for zero-allocation streaming.
/// </summary>
public sealed class HighPerformanceMarketData : IAsyncDisposable
{
    private readonly MercadoBitcoinWebSocketClient _wsClient;
    private readonly ILogger<HighPerformanceMarketData>? _logger;
    private readonly CancellationTokenSource _cts = new();

    // Lock-free storage for latest snapshots
    private readonly ConcurrentDictionary<string, TickerSnapshot> _lastTickers = new();
    private readonly ConcurrentDictionary<string, OrderBookSnapshot> _lastOrderBooks = new();
    private readonly ConcurrentDictionary<string, TradeSnapshot> _lastTrades = new();

    // Channels for streaming updates to consumers
    private readonly Channel<TickerUpdate> _tickerChannel;
    private readonly Channel<TradeUpdate> _tradeChannel;
    private readonly Channel<OrderBookUpdate> _orderBookChannel;

    // Subscription tracking
    private readonly ConcurrentDictionary<string, bool> _activeSubscriptions = new();

    // Performance metrics
    private long _tickerUpdates;
    private long _tradeUpdates;
    private long _orderBookUpdates;
    private long _totalLatencyTicks;
    private long _latencyMeasurements;

    /// <summary>
    /// Event raised when a ticker is updated.
    /// </summary>
    public event EventHandler<TickerSnapshot>? TickerUpdated;

    /// <summary>
    /// Event raised when the order book is updated.
    /// </summary>
    public event EventHandler<OrderBookSnapshot>? OrderBookUpdated;

    /// <summary>
    /// Event raised when a trade occurs.
    /// </summary>
    public event EventHandler<TradeSnapshot>? TradeOccurred;

    /// <summary>
    /// Creates a new high-performance market data aggregator.
    /// </summary>
    /// <param name="options">WebSocket client options.</param>
    /// <param name="logger">Optional logger.</param>
    public HighPerformanceMarketData(
        WebSocketClientOptions? options = null,
        ILogger<HighPerformanceMarketData>? logger = null)
    {
        _wsClient = new MercadoBitcoinWebSocketClient(
            options ?? CreateOptimizedOptions(),
            null);
        _logger = logger;

        // Create bounded channels with backpressure
        var channelOptions = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        };

        _tickerChannel = Channel.CreateBounded<TickerUpdate>(channelOptions);
        _tradeChannel = Channel.CreateBounded<TradeUpdate>(channelOptions);
        _orderBookChannel = Channel.CreateBounded<OrderBookUpdate>(channelOptions);
    }

    #region Properties

    /// <summary>
    /// Gets the number of ticker updates received.
    /// </summary>
    public long TickerUpdateCount => Volatile.Read(ref _tickerUpdates);

    /// <summary>
    /// Gets the number of trade updates received.
    /// </summary>
    public long TradeUpdateCount => Volatile.Read(ref _tradeUpdates);

    /// <summary>
    /// Gets the number of order book updates received.
    /// </summary>
    public long OrderBookUpdateCount => Volatile.Read(ref _orderBookUpdates);

    /// <summary>
    /// Gets the average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs
    {
        get
        {
            var measurements = Volatile.Read(ref _latencyMeasurements);
            if (measurements == 0) return 0;
            var ticks = Volatile.Read(ref _totalLatencyTicks);
            return (double)ticks / measurements / TimeSpan.TicksPerMillisecond;
        }
    }

    /// <summary>
    /// Gets the current WebSocket connection state.
    /// </summary>
    public WebSocketConnectionState ConnectionState => _wsClient.ConnectionState;

    /// <summary>
    /// Gets all active subscriptions.
    /// </summary>
    public IReadOnlyCollection<string> ActiveSubscriptions => _activeSubscriptions.Keys.ToList();

    /// <summary>
    /// Channel reader for ticker updates.
    /// </summary>
    public ChannelReader<TickerUpdate> TickerUpdates => _tickerChannel.Reader;

    /// <summary>
    /// Channel reader for trade updates.
    /// </summary>
    public ChannelReader<TradeUpdate> TradeUpdates => _tradeChannel.Reader;

    /// <summary>
    /// Channel reader for order book updates.
    /// </summary>
    public ChannelReader<OrderBookUpdate> OrderBookUpdates => _orderBookChannel.Reader;

    #endregion

    #region Connection Management

    /// <summary>
    /// Connects to the WebSocket server and starts receiving data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Connecting to market data feed...");
        await _wsClient.ConnectAsync(ct);
        _logger?.LogInformation("Connected to market data feed");
    }

    /// <summary>
    /// Starts subscriptions for the specified symbols.
    /// </summary>
    /// <param name="symbols">Symbols to subscribe to.</param>
    /// <param name="subscribeToTicker">Subscribe to ticker updates.</param>
    /// <param name="subscribeToTrades">Subscribe to trade updates.</param>
    /// <param name="subscribeToOrderBook">Subscribe to order book updates.</param>
    public async Task StartAsync(
        IEnumerable<string> symbols,
        bool subscribeToTicker = true,
        bool subscribeToTrades = true,
        bool subscribeToOrderBook = true)
    {
        if (_wsClient.ConnectionState != WebSocketConnectionState.Connected)
        {
            await ConnectAsync(_cts.Token);
        }

        foreach (var symbol in symbols)
        {
            if (subscribeToTicker)
            {
                _ = SubscribeTickerAsync(symbol);
            }

            if (subscribeToTrades)
            {
                _ = SubscribeTradesAsync(symbol);
            }

            if (subscribeToOrderBook)
            {
                _ = SubscribeOrderBookAsync(symbol);
            }
        }
    }

    /// <summary>
    /// Disconnects from the WebSocket server.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Disconnecting from market data feed...");
        await _wsClient.DisconnectAsync(ct);
        _logger?.LogInformation("Disconnected from market data feed");
    }

    #endregion

    #region Snapshot Access (Zero-Allocation)

    /// <summary>
    /// Gets the last ticker for a symbol (O(1), zero-allocation).
    /// </summary>
    /// <param name="symbol">Trading pair symbol.</param>
    /// <param name="ticker">The ticker snapshot if found.</param>
    /// <returns>True if ticker exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLastTicker(string symbol, out TickerSnapshot ticker)
    {
        return _lastTickers.TryGetValue(symbol, out ticker);
    }

    /// <summary>
    /// Gets the last order book for a symbol (O(1), zero-allocation).
    /// </summary>
    /// <param name="symbol">Trading pair symbol.</param>
    /// <param name="orderBook">The order book snapshot if found.</param>
    /// <returns>True if order book exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLastOrderBook(string symbol, out OrderBookSnapshot orderBook)
    {
        return _lastOrderBooks.TryGetValue(symbol, out orderBook);
    }

    /// <summary>
    /// Gets the last trade for a symbol (O(1), zero-allocation).
    /// </summary>
    /// <param name="symbol">Trading pair symbol.</param>
    /// <param name="trade">The trade snapshot if found.</param>
    /// <returns>True if trade exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLastTrade(string symbol, out TradeSnapshot trade)
    {
        return _lastTrades.TryGetValue(symbol, out trade);
    }

    /// <summary>
    /// Gets the current spread for a symbol (inline calculation).
    /// </summary>
    /// <param name="symbol">Trading pair symbol.</param>
    /// <returns>The spread, or decimal.MaxValue if unknown.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetSpread(string symbol)
    {
        if (_lastTickers.TryGetValue(symbol, out var ticker))
        {
            return ticker.BestAsk - ticker.BestBid;
        }
        return decimal.MaxValue;
    }

    /// <summary>
    /// Gets the mid-price for a symbol (inline calculation).
    /// </summary>
    /// <param name="symbol">Trading pair symbol.</param>
    /// <returns>The mid-price, or 0 if unknown.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetMidPrice(string symbol)
    {
        if (_lastTickers.TryGetValue(symbol, out var ticker))
        {
            return (ticker.BestAsk + ticker.BestBid) / 2m;
        }
        return 0m;
    }

    /// <summary>
    /// Gets the best bid price for a symbol.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetBestBid(string symbol)
    {
        return _lastTickers.TryGetValue(symbol, out var ticker) ? ticker.BestBid : 0m;
    }

    /// <summary>
    /// Gets the best ask price for a symbol.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetBestAsk(string symbol)
    {
        return _lastTickers.TryGetValue(symbol, out var ticker) ? ticker.BestAsk : 0m;
    }

    /// <summary>
    /// Gets the last trade price for a symbol.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetLastPrice(string symbol)
    {
        return _lastTickers.TryGetValue(symbol, out var ticker) ? ticker.Last : 0m;
    }

    /// <summary>
    /// Gets all current ticker snapshots.
    /// </summary>
    public IReadOnlyDictionary<string, TickerSnapshot> GetAllTickers()
    {
        return new Dictionary<string, TickerSnapshot>(_lastTickers);
    }

    #endregion

    #region Subscription Handlers

    private async Task SubscribeTickerAsync(string symbol)
    {
        var key = $"ticker:{symbol}";
        if (!_activeSubscriptions.TryAdd(key, true))
        {
            return; // Already subscribed
        }

        _logger?.LogDebug("Subscribing to ticker for {Symbol}", symbol);

        try
        {
            await foreach (var msg in _wsClient.SubscribeTickerAsync(symbol, _cts.Token))
            {
                if (msg.Data is { } data)
                {
                    var receiveTime = Stopwatch.GetTimestamp();

                    var snapshot = new TickerSnapshot
                    {
                        Symbol = symbol,
                        Last = data.Last,
                        BestBid = data.BestBid,
                        BestAsk = data.BestAsk,
                        High = data.High,
                        Low = data.Low,
                        Volume = data.Volume,
                        Open = data.Open,
                        Timestamp = receiveTime
                    };

                    _lastTickers[symbol] = snapshot;
                    Interlocked.Increment(ref _tickerUpdates);

                    // Publish to channel (non-blocking)
                    _tickerChannel.Writer.TryWrite(new TickerUpdate
                    {
                        Symbol = symbol,
                        Snapshot = snapshot
                    });

                    // Record telemetry
                    MercadoBitcoinTelemetry.RecordWebSocketMessage("ticker", symbol);

                    // Raise event
                    TickerUpdated?.Invoke(this, snapshot);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Ticker subscription cancelled for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ticker subscription for {Symbol}", symbol);
        }
        finally
        {
            _activeSubscriptions.TryRemove(key, out _);
        }
    }

    private async Task SubscribeTradesAsync(string symbol)
    {
        var key = $"trades:{symbol}";
        if (!_activeSubscriptions.TryAdd(key, true))
        {
            return;
        }

        _logger?.LogDebug("Subscribing to trades for {Symbol}", symbol);

        try
        {
            await foreach (var msg in _wsClient.SubscribeTradesAsync(symbol, _cts.Token))
            {
                if (msg.Data is { } data)
                {
                    var receiveTime = Stopwatch.GetTimestamp();

                    var snapshot = new TradeSnapshot
                    {
                        Symbol = symbol,
                        TradeId = data.TradeId,
                        Price = data.Price,
                        Amount = data.Amount,
                        Side = data.Side,
                        IsBuy = data.IsBuy,
                        Timestamp = receiveTime
                    };

                    _lastTrades[symbol] = snapshot;
                    Interlocked.Increment(ref _tradeUpdates);

                    // Publish to channel
                    _tradeChannel.Writer.TryWrite(new TradeUpdate
                    {
                        Symbol = symbol,
                        Snapshot = snapshot
                    });

                    MercadoBitcoinTelemetry.RecordWebSocketMessage("trades", symbol);
                    TradeOccurred?.Invoke(this, snapshot);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Trades subscription cancelled for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in trades subscription for {Symbol}", symbol);
        }
        finally
        {
            _activeSubscriptions.TryRemove(key, out _);
        }
    }

    private async Task SubscribeOrderBookAsync(string symbol)
    {
        var key = $"orderbook:{symbol}";
        if (!_activeSubscriptions.TryAdd(key, true))
        {
            return;
        }

        _logger?.LogDebug("Subscribing to order book for {Symbol}", symbol);

        try
        {
            await foreach (var msg in _wsClient.SubscribeOrderBookAsync(symbol, _cts.Token))
            {
                if (msg.Data is { } data)
                {
                    var receiveTime = Stopwatch.GetTimestamp();

                    var snapshot = new OrderBookSnapshot
                    {
                        Symbol = symbol,
                        BestBidPrice = data.BestBidPrice ?? 0,
                        BestBidQty = data.BestBidQuantity ?? 0,
                        BestAskPrice = data.BestAskPrice ?? 0,
                        BestAskQty = data.BestAskQuantity ?? 0,
                        Spread = data.Spread ?? 0,
                        MidPrice = data.MidPrice ?? 0,
                        BidLevels = data.Bids?.Count ?? 0,
                        AskLevels = data.Asks?.Count ?? 0,
                        Timestamp = receiveTime
                    };

                    _lastOrderBooks[symbol] = snapshot;
                    Interlocked.Increment(ref _orderBookUpdates);

                    // Publish to channel
                    _orderBookChannel.Writer.TryWrite(new OrderBookUpdate
                    {
                        Symbol = symbol,
                        Snapshot = snapshot
                    });

                    MercadoBitcoinTelemetry.RecordWebSocketMessage("orderbook", symbol);
                    OrderBookUpdated?.Invoke(this, snapshot);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Order book subscription cancelled for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in order book subscription for {Symbol}", symbol);
        }
        finally
        {
            _activeSubscriptions.TryRemove(key, out _);
        }
    }

    #endregion

    #region Helper Methods

    private static WebSocketClientOptions CreateOptimizedOptions()
    {
        return new WebSocketClientOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(15),
            KeepAliveTimeout = TimeSpan.FromSeconds(5),
            AutoReconnect = true,
            MaxReconnectAttempts = 100,
            InitialReconnectDelay = TimeSpan.FromMilliseconds(100),
            MaxReconnectDelay = TimeSpan.FromSeconds(5),
            ReceiveBufferSize = 16 * 1024,
            SendBufferSize = 1024,
            ConnectionTimeout = TimeSpan.FromSeconds(5)
        };
    }

    #endregion

    /// <summary>
    /// Disposes the market data aggregator.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        _tickerChannel.Writer.Complete();
        _tradeChannel.Writer.Complete();
        _orderBookChannel.Writer.Complete();

        await _wsClient.DisposeAsync();
        _cts.Dispose();

        _lastTickers.Clear();
        _lastOrderBooks.Clear();
        _lastTrades.Clear();
        _activeSubscriptions.Clear();
    }
}

#region Snapshot Structs (Zero-Allocation)

/// <summary>
/// Immutable ticker snapshot for zero-allocation access.
/// </summary>
public readonly record struct TickerSnapshot
{
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Last trade price.</summary>
    public required decimal Last { get; init; }
    /// <summary>Best bid price.</summary>
    public required decimal BestBid { get; init; }
    /// <summary>Best ask price.</summary>
    public required decimal BestAsk { get; init; }
    /// <summary>24h high.</summary>
    public required decimal High { get; init; }
    /// <summary>24h low.</summary>
    public required decimal Low { get; init; }
    /// <summary>24h volume.</summary>
    public required decimal Volume { get; init; }
    /// <summary>Opening price.</summary>
    public required decimal Open { get; init; }
    /// <summary>Timestamp (Stopwatch ticks).</summary>
    public required long Timestamp { get; init; }

    /// <summary>Calculated spread.</summary>
    public decimal Spread => BestAsk - BestBid;
    /// <summary>Calculated mid-price.</summary>
    public decimal MidPrice => (BestAsk + BestBid) / 2m;
    /// <summary>24h change percentage.</summary>
    public decimal ChangePercent => Open > 0 ? ((Last - Open) / Open) * 100m : 0m;
}

/// <summary>
/// Immutable order book snapshot for zero-allocation access.
/// </summary>
public readonly record struct OrderBookSnapshot
{
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Best bid price.</summary>
    public required decimal BestBidPrice { get; init; }
    /// <summary>Best bid quantity.</summary>
    public required decimal BestBidQty { get; init; }
    /// <summary>Best ask price.</summary>
    public required decimal BestAskPrice { get; init; }
    /// <summary>Best ask quantity.</summary>
    public required decimal BestAskQty { get; init; }
    /// <summary>Spread.</summary>
    public required decimal Spread { get; init; }
    /// <summary>Mid-price.</summary>
    public required decimal MidPrice { get; init; }
    /// <summary>Number of bid levels.</summary>
    public required int BidLevels { get; init; }
    /// <summary>Number of ask levels.</summary>
    public required int AskLevels { get; init; }
    /// <summary>Timestamp (Stopwatch ticks).</summary>
    public required long Timestamp { get; init; }
}

/// <summary>
/// Immutable trade snapshot for zero-allocation access.
/// </summary>
public readonly record struct TradeSnapshot
{
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Trade ID.</summary>
    public required long TradeId { get; init; }
    /// <summary>Trade price.</summary>
    public required decimal Price { get; init; }
    /// <summary>Trade amount.</summary>
    public required decimal Amount { get; init; }
    /// <summary>Trade side string.</summary>
    public required string Side { get; init; }
    /// <summary>Whether this was a buy.</summary>
    public required bool IsBuy { get; init; }
    /// <summary>Timestamp (Stopwatch ticks).</summary>
    public required long Timestamp { get; init; }

    /// <summary>Total value of the trade.</summary>
    public decimal TotalValue => Price * Amount;
}

/// <summary>
/// Ticker update for channel streaming.
/// </summary>
public readonly record struct TickerUpdate
{
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Ticker snapshot.</summary>
    public required TickerSnapshot Snapshot { get; init; }
}

/// <summary>
/// Trade update for channel streaming.
/// </summary>
public readonly record struct TradeUpdate
{
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Trade snapshot.</summary>
    public required TradeSnapshot Snapshot { get; init; }
}

/// <summary>
/// Order book update for channel streaming.
/// </summary>
public readonly record struct OrderBookUpdate
{
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Order book snapshot.</summary>
    public required OrderBookSnapshot Snapshot { get; init; }
}

#endregion
