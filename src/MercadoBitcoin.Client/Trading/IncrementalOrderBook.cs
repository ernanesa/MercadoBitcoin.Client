using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// High-performance incremental order book that supports both full snapshots and delta updates.
/// Maintains sorted bid/ask levels with O(log n) operations.
/// </summary>
/// <remarks>
/// Note: The Mercado Bitcoin WebSocket API currently only provides full order book snapshots.
/// This implementation is designed to efficiently process these snapshots and is ready to
/// support delta updates if/when the API provides them in the future.
/// </remarks>
public sealed class IncrementalOrderBook : IDisposable
{
    private readonly string _symbol;
    private readonly ILogger<IncrementalOrderBook>? _logger;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly IncrementalOrderBookOptions _options;

    // Bids sorted descending (highest first), Asks sorted ascending (lowest first)
    private readonly SortedDictionary<decimal, decimal> _bids = new(Comparer<decimal>.Create((a, b) => b.CompareTo(a)));
    private readonly SortedDictionary<decimal, decimal> _asks = new();

    private long _lastUpdateId;
    private long _snapshotCount;
    private long _deltaCount;
    private DateTime _lastUpdateTime;
    private bool _disposed;

    /// <summary>
    /// Event raised when the order book is updated.
    /// </summary>
    public event EventHandler<OrderBookUpdatedEventArgs>? Updated;

    /// <summary>
    /// Event raised when a significant spread change is detected.
    /// </summary>
    public event EventHandler<SpreadChangedEventArgs>? SpreadChanged;

    /// <summary>
    /// Creates a new instance of IncrementalOrderBook.
    /// </summary>
    /// <param name="symbol">The trading symbol (e.g., "BTC-BRL").</param>
    /// <param name="options">Order book options.</param>
    /// <param name="logger">Optional logger.</param>
    public IncrementalOrderBook(
        string symbol,
        IncrementalOrderBookOptions? options = null,
        ILogger<IncrementalOrderBook>? logger = null)
    {
        _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        _options = options ?? new IncrementalOrderBookOptions();
        _logger = logger;
    }

    /// <summary>
    /// Gets the trading symbol.
    /// </summary>
    public string Symbol => _symbol;

    /// <summary>
    /// Gets the last update ID processed.
    /// </summary>
    public long LastUpdateId => Volatile.Read(ref _lastUpdateId);

    /// <summary>
    /// Gets the time of the last update.
    /// </summary>
    public DateTime LastUpdateTime => _lastUpdateTime;

    /// <summary>
    /// Gets the number of bid levels.
    /// </summary>
    public int BidLevels
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _bids.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets the number of ask levels.
    /// </summary>
    public int AskLevels
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _asks.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets the number of snapshots processed.
    /// </summary>
    public long SnapshotCount => Volatile.Read(ref _snapshotCount);

    /// <summary>
    /// Gets the number of delta updates processed.
    /// </summary>
    public long DeltaCount => Volatile.Read(ref _deltaCount);

    /// <summary>
    /// Gets the best bid price and quantity.
    /// </summary>
    /// <returns>Tuple of (price, quantity) or null if no bids.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (decimal Price, decimal Quantity)? GetBestBid()
    {
        _lock.EnterReadLock();
        try
        {
            if (_bids.Count == 0) return null;
            var first = _bids.First();
            return (first.Key, first.Value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the best ask price and quantity.
    /// </summary>
    /// <returns>Tuple of (price, quantity) or null if no asks.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (decimal Price, decimal Quantity)? GetBestAsk()
    {
        _lock.EnterReadLock();
        try
        {
            if (_asks.Count == 0) return null;
            var first = _asks.First();
            return (first.Key, first.Value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the current spread (best ask - best bid).
    /// </summary>
    /// <returns>The spread or null if book is empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal? GetSpread()
    {
        _lock.EnterReadLock();
        try
        {
            if (_bids.Count == 0 || _asks.Count == 0) return null;
            return _asks.First().Key - _bids.First().Key;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the mid price ((best bid + best ask) / 2).
    /// </summary>
    /// <returns>The mid price or null if book is empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal? GetMidPrice()
    {
        _lock.EnterReadLock();
        try
        {
            if (_bids.Count == 0 || _asks.Count == 0) return null;
            return (_bids.First().Key + _asks.First().Key) / 2;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the spread as a percentage of the mid price.
    /// </summary>
    /// <returns>Spread percentage or null if book is empty.</returns>
    public decimal? GetSpreadPercent()
    {
        _lock.EnterReadLock();
        try
        {
            if (_bids.Count == 0 || _asks.Count == 0) return null;
            var bestBid = _bids.First().Key;
            var bestAsk = _asks.First().Key;
            var mid = (bestBid + bestAsk) / 2;
            if (mid == 0) return null;
            return ((bestAsk - bestBid) / mid) * 100;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Applies a full snapshot, replacing all existing data.
    /// </summary>
    /// <param name="bids">Bid levels as (price, quantity) pairs.</param>
    /// <param name="asks">Ask levels as (price, quantity) pairs.</param>
    /// <param name="updateId">Update ID for sequencing (optional).</param>
    public void ApplySnapshot(
        IEnumerable<(decimal Price, decimal Quantity)> bids,
        IEnumerable<(decimal Price, decimal Quantity)> asks,
        long updateId = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var previousSpread = GetSpread();

        _lock.EnterWriteLock();
        try
        {
            _bids.Clear();
            _asks.Clear();

            foreach (var (price, qty) in bids)
            {
                if (qty > 0)
                {
                    _bids[price] = qty;
                }
            }

            foreach (var (price, qty) in asks)
            {
                if (qty > 0)
                {
                    _asks[price] = qty;
                }
            }

            // Apply depth limit if configured
            ApplyDepthLimit();

            if (updateId > 0)
            {
                _lastUpdateId = updateId;
            }

            _lastUpdateTime = DateTime.UtcNow;
            Interlocked.Increment(ref _snapshotCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _logger?.LogDebug(
            "Applied snapshot for {Symbol}: {BidLevels} bids, {AskLevels} asks (update #{UpdateId})",
            _symbol, _bids.Count, _asks.Count, updateId);

        // Raise events
        OnUpdated(new OrderBookUpdatedEventArgs
        {
            Symbol = _symbol,
            UpdateType = OrderBookUpdateType.Snapshot,
            UpdateId = updateId,
            BidLevels = _bids.Count,
            AskLevels = _asks.Count,
            Timestamp = _lastUpdateTime
        });

        CheckSpreadChange(previousSpread);
    }

    /// <summary>
    /// Applies a delta update to the order book.
    /// </summary>
    /// <param name="delta">The delta update to apply.</param>
    /// <returns>True if the update was applied, false if it was stale.</returns>
    public bool ApplyDelta(OrderBookDelta delta)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Check for stale updates
        if (delta.UpdateId <= Volatile.Read(ref _lastUpdateId))
        {
            _logger?.LogDebug(
                "Ignoring stale delta for {Symbol}: {DeltaId} <= {LastId}",
                _symbol, delta.UpdateId, _lastUpdateId);
            return false;
        }

        var previousSpread = GetSpread();

        _lock.EnterWriteLock();
        try
        {
            // Apply bid updates
            foreach (var (price, qty) in delta.Bids)
            {
                if (qty == 0)
                {
                    _bids.Remove(price);
                }
                else
                {
                    _bids[price] = qty;
                }
            }

            // Apply ask updates
            foreach (var (price, qty) in delta.Asks)
            {
                if (qty == 0)
                {
                    _asks.Remove(price);
                }
                else
                {
                    _asks[price] = qty;
                }
            }

            // Apply depth limit if configured
            ApplyDepthLimit();

            _lastUpdateId = delta.UpdateId;
            _lastUpdateTime = DateTime.UtcNow;
            Interlocked.Increment(ref _deltaCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _logger?.LogDebug(
            "Applied delta for {Symbol}: {BidChanges} bid changes, {AskChanges} ask changes (update #{UpdateId})",
            _symbol, delta.Bids.Count, delta.Asks.Count, delta.UpdateId);

        // Raise events
        OnUpdated(new OrderBookUpdatedEventArgs
        {
            Symbol = _symbol,
            UpdateType = OrderBookUpdateType.Delta,
            UpdateId = delta.UpdateId,
            BidLevels = _bids.Count,
            AskLevels = _asks.Count,
            Timestamp = _lastUpdateTime
        });

        CheckSpreadChange(previousSpread);
        return true;
    }

    /// <summary>
    /// Gets top N bid levels.
    /// </summary>
    /// <param name="count">Number of levels to retrieve.</param>
    /// <returns>Array of (price, quantity) tuples.</returns>
    public (decimal Price, decimal Quantity)[] GetTopBids(int count)
    {
        _lock.EnterReadLock();
        try
        {
            return _bids.Take(count).Select(kv => (kv.Key, kv.Value)).ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets top N ask levels.
    /// </summary>
    /// <param name="count">Number of levels to retrieve.</param>
    /// <returns>Array of (price, quantity) tuples.</returns>
    public (decimal Price, decimal Quantity)[] GetTopAsks(int count)
    {
        _lock.EnterReadLock();
        try
        {
            return _asks.Take(count).Select(kv => (kv.Key, kv.Value)).ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the total volume at bid side up to a given depth.
    /// </summary>
    /// <param name="levels">Number of levels to sum.</param>
    /// <returns>Total bid volume.</returns>
    public decimal GetBidVolume(int levels = int.MaxValue)
    {
        _lock.EnterReadLock();
        try
        {
            return _bids.Take(levels).Sum(kv => kv.Value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the total volume at ask side up to a given depth.
    /// </summary>
    /// <param name="levels">Number of levels to sum.</param>
    /// <returns>Total ask volume.</returns>
    public decimal GetAskVolume(int levels = int.MaxValue)
    {
        _lock.EnterReadLock();
        try
        {
            return _asks.Take(levels).Sum(kv => kv.Value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Calculates the volume-weighted average price (VWAP) for a given quantity.
    /// </summary>
    /// <param name="quantity">The quantity to fill.</param>
    /// <param name="side">The side to calculate for (buy = asks, sell = bids).</param>
    /// <returns>VWAP and actual fillable quantity, or null if book is empty.</returns>
    public (decimal Vwap, decimal FillableQuantity)? CalculateVwap(decimal quantity, OrderSide side)
    {
        _lock.EnterReadLock();
        try
        {
            var levels = side == OrderSide.Buy ? _asks : _bids;
            if (levels.Count == 0) return null;

            decimal totalCost = 0;
            decimal filledQuantity = 0;

            foreach (var (price, availableQty) in levels)
            {
                var fillQty = Math.Min(availableQty, quantity - filledQuantity);
                totalCost += fillQty * price;
                filledQuantity += fillQty;

                if (filledQuantity >= quantity)
                {
                    break;
                }
            }

            if (filledQuantity == 0) return null;
            return (totalCost / filledQuantity, filledQuantity);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the order book imbalance ratio.
    /// Positive values indicate more buying pressure, negative indicates selling pressure.
    /// </summary>
    /// <param name="levels">Number of levels to consider.</param>
    /// <returns>Imbalance ratio between -1 and 1, or null if book is empty.</returns>
    public decimal? GetImbalanceRatio(int levels = 5)
    {
        _lock.EnterReadLock();
        try
        {
            var bidVolume = _bids.Take(levels).Sum(kv => kv.Value);
            var askVolume = _asks.Take(levels).Sum(kv => kv.Value);

            var totalVolume = bidVolume + askVolume;
            if (totalVolume == 0) return null;

            return (bidVolume - askVolume) / totalVolume;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets a snapshot of the current order book state.
    /// </summary>
    /// <param name="depth">Maximum depth to include. Default: all levels.</param>
    /// <returns>Order book snapshot.</returns>
    public OrderBookState GetState(int depth = int.MaxValue)
    {
        _lock.EnterReadLock();
        try
        {
            return new OrderBookState
            {
                Symbol = _symbol,
                Bids = _bids.Take(depth).Select(kv => (kv.Key, kv.Value)).ToArray(),
                Asks = _asks.Take(depth).Select(kv => (kv.Key, kv.Value)).ToArray(),
                BestBid = _bids.Count > 0 ? _bids.First().Key : null,
                BestAsk = _asks.Count > 0 ? _asks.First().Key : null,
                Spread = GetSpreadInternal(),
                MidPrice = GetMidPriceInternal(),
                LastUpdateId = _lastUpdateId,
                LastUpdateTime = _lastUpdateTime,
                TotalBidVolume = _bids.Sum(kv => kv.Value),
                TotalAskVolume = _asks.Sum(kv => kv.Value)
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears the order book.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _bids.Clear();
            _asks.Clear();
            _lastUpdateId = 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _logger?.LogDebug("Cleared order book for {Symbol}", _symbol);
    }

    private void ApplyDepthLimit()
    {
        // Must be called while holding write lock
        if (_options.MaxDepth <= 0) return;

        while (_bids.Count > _options.MaxDepth)
        {
            _bids.Remove(_bids.Last().Key);
        }

        while (_asks.Count > _options.MaxDepth)
        {
            _asks.Remove(_asks.Last().Key);
        }
    }

    // Internal version without lock (called from within locked context)
    private decimal? GetSpreadInternal()
    {
        if (_bids.Count == 0 || _asks.Count == 0) return null;
        return _asks.First().Key - _bids.First().Key;
    }

    private decimal? GetMidPriceInternal()
    {
        if (_bids.Count == 0 || _asks.Count == 0) return null;
        return (_bids.First().Key + _asks.First().Key) / 2;
    }

    private void CheckSpreadChange(decimal? previousSpread)
    {
        var currentSpread = GetSpread();
        if (!previousSpread.HasValue || !currentSpread.HasValue) return;

        var changePercent = Math.Abs((currentSpread.Value - previousSpread.Value) / previousSpread.Value) * 100;
        if (changePercent >= _options.SpreadChangeThresholdPercent)
        {
            OnSpreadChanged(new SpreadChangedEventArgs
            {
                Symbol = _symbol,
                PreviousSpread = previousSpread.Value,
                CurrentSpread = currentSpread.Value,
                ChangePercent = changePercent,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private void OnUpdated(OrderBookUpdatedEventArgs args)
    {
        Updated?.Invoke(this, args);
    }

    private void OnSpreadChanged(SpreadChangedEventArgs args)
    {
        _logger?.LogInformation(
            "Spread changed for {Symbol}: {Previous} -> {Current} ({Change:F2}%)",
            _symbol, args.PreviousSpread, args.CurrentSpread, args.ChangePercent);

        SpreadChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lock.Dispose();
    }
}

/// <summary>
/// Configuration options for IncrementalOrderBook.
/// </summary>
public sealed class IncrementalOrderBookOptions
{
    /// <summary>
    /// Maximum depth to maintain. 0 or negative means unlimited. Default: 100.
    /// </summary>
    public int MaxDepth { get; set; } = 100;

    /// <summary>
    /// Threshold percentage for triggering spread change events. Default: 1%.
    /// </summary>
    public decimal SpreadChangeThresholdPercent { get; set; } = 1m;

    /// <summary>
    /// Whether to log individual updates. Default: false.
    /// </summary>
    public bool LogUpdates { get; set; } = false;
}

/// <summary>
/// Represents a delta update to the order book.
/// </summary>
public sealed class OrderBookDelta
{
    /// <summary>
    /// The update sequence ID.
    /// </summary>
    public required long UpdateId { get; init; }

    /// <summary>
    /// Bid level changes. Quantity of 0 means remove the level.
    /// </summary>
    public required IReadOnlyList<(decimal Price, decimal Quantity)> Bids { get; init; }

    /// <summary>
    /// Ask level changes. Quantity of 0 means remove the level.
    /// </summary>
    public required IReadOnlyList<(decimal Price, decimal Quantity)> Asks { get; init; }
}

/// <summary>
/// The type of order book update.
/// </summary>
public enum OrderBookUpdateType
{
    /// <summary>
    /// Full snapshot replacing all data.
    /// </summary>
    Snapshot,

    /// <summary>
    /// Delta update modifying specific levels.
    /// </summary>
    Delta
}

/// <summary>
/// Order side for VWAP calculations.
/// </summary>
public enum OrderSide
{
    /// <summary>
    /// Buy side (takes from asks).
    /// </summary>
    Buy,

    /// <summary>
    /// Sell side (takes from bids).
    /// </summary>
    Sell
}

/// <summary>
/// Event arguments for order book updates.
/// </summary>
public sealed class OrderBookUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// The type of update.
    /// </summary>
    public required OrderBookUpdateType UpdateType { get; init; }

    /// <summary>
    /// The update sequence ID.
    /// </summary>
    public required long UpdateId { get; init; }

    /// <summary>
    /// Number of bid levels after update.
    /// </summary>
    public required int BidLevels { get; init; }

    /// <summary>
    /// Number of ask levels after update.
    /// </summary>
    public required int AskLevels { get; init; }

    /// <summary>
    /// Timestamp of the update.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Event arguments for spread changes.
/// </summary>
public sealed class SpreadChangedEventArgs : EventArgs
{
    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// The previous spread value.
    /// </summary>
    public required decimal PreviousSpread { get; init; }

    /// <summary>
    /// The current spread value.
    /// </summary>
    public required decimal CurrentSpread { get; init; }

    /// <summary>
    /// The percentage change.
    /// </summary>
    public required decimal ChangePercent { get; init; }

    /// <summary>
    /// Timestamp of the change.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Represents a snapshot of the order book state.
/// </summary>
public sealed class OrderBookState
{
    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Bid levels (price, quantity).
    /// </summary>
    public required (decimal Price, decimal Quantity)[] Bids { get; init; }

    /// <summary>
    /// Ask levels (price, quantity).
    /// </summary>
    public required (decimal Price, decimal Quantity)[] Asks { get; init; }

    /// <summary>
    /// Best bid price.
    /// </summary>
    public decimal? BestBid { get; init; }

    /// <summary>
    /// Best ask price.
    /// </summary>
    public decimal? BestAsk { get; init; }

    /// <summary>
    /// Current spread.
    /// </summary>
    public decimal? Spread { get; init; }

    /// <summary>
    /// Current mid price.
    /// </summary>
    public decimal? MidPrice { get; init; }

    /// <summary>
    /// Last update sequence ID.
    /// </summary>
    public long LastUpdateId { get; init; }

    /// <summary>
    /// Time of last update.
    /// </summary>
    public DateTime LastUpdateTime { get; init; }

    /// <summary>
    /// Total volume on bid side.
    /// </summary>
    public decimal TotalBidVolume { get; init; }

    /// <summary>
    /// Total volume on ask side.
    /// </summary>
    public decimal TotalAskVolume { get; init; }
}

/// <summary>
/// Extension methods for IncrementalOrderBook.
/// </summary>
public static class IncrementalOrderBookExtensions
{
    /// <summary>
    /// Creates an IncrementalOrderBook from WebSocket order book data.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="bids">Raw bid data from WebSocket.</param>
    /// <param name="asks">Raw ask data from WebSocket.</param>
    /// <param name="options">Optional configuration.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Initialized IncrementalOrderBook.</returns>
    public static IncrementalOrderBook CreateFromWebSocket(
        string symbol,
        IEnumerable<decimal[]> bids,
        IEnumerable<decimal[]> asks,
        IncrementalOrderBookOptions? options = null,
        ILogger<IncrementalOrderBook>? logger = null)
    {
        var orderBook = new IncrementalOrderBook(symbol, options, logger);

        var bidLevels = bids.Select(b => (Price: b[0], Quantity: b[1]));
        var askLevels = asks.Select(a => (Price: a[0], Quantity: a[1]));

        orderBook.ApplySnapshot(bidLevels, askLevels);
        return orderBook;
    }

    /// <summary>
    /// Checks if the order book is considered healthy (has both bids and asks).
    /// </summary>
    /// <param name="orderBook">The order book to check.</param>
    /// <returns>True if healthy.</returns>
    public static bool IsHealthy(this IncrementalOrderBook orderBook)
    {
        return orderBook.BidLevels > 0 && orderBook.AskLevels > 0;
    }

    /// <summary>
    /// Checks if the order book data is stale.
    /// </summary>
    /// <param name="orderBook">The order book to check.</param>
    /// <param name="maxAge">Maximum age before considered stale.</param>
    /// <returns>True if stale.</returns>
    public static bool IsStale(this IncrementalOrderBook orderBook, TimeSpan maxAge)
    {
        return DateTime.UtcNow - orderBook.LastUpdateTime > maxAge;
    }
}
