using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// Tracks and monitors order execution status for trading operations.
/// Provides real-time order state management with callbacks for status changes.
/// </summary>
public sealed class OrderTracker : IDisposable, IAsyncDisposable
{
    private readonly MercadoBitcoinClient _client;
    private readonly ILogger<OrderTracker>? _logger;
    private readonly OrderTrackerOptions _options;
    private readonly ConcurrentDictionary<string, TrackedOrderInfo> _orders = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _monitoringTasks = new();
    private readonly SemaphoreSlim _pollLock = new(1, 1);

    private Timer? _pollingTimer;
    private bool _disposed;

    // Statistics
    private long _ordersTracked;
    private long _ordersFilled;
    private long _ordersCancelled;
    private long _ordersExpired;
    private long _pollCount;
    private long _totalPollLatencyMs;

    /// <summary>
    /// Event raised when an order status changes.
    /// </summary>
    public event EventHandler<OrderStatusChangedEventArgs>? OrderStatusChanged;

    /// <summary>
    /// Event raised when an order is filled (completely or partially).
    /// </summary>
    public event EventHandler<OrderFilledEventArgs>? OrderFilled;

    /// <summary>
    /// Event raised when an order is cancelled.
    /// </summary>
    public event EventHandler<OrderTrackerCancelledEventArgs>? OrderCancelled;

    /// <summary>
    /// Event raised when an order tracking error occurs.
    /// </summary>
    public event EventHandler<OrderTrackingErrorEventArgs>? TrackingError;

    /// <summary>
    /// Creates a new instance of OrderTracker.
    /// </summary>
    /// <param name="client">The MercadoBitcoin client for API calls.</param>
    /// <param name="options">Tracker configuration options.</param>
    /// <param name="logger">Optional logger.</param>
    public OrderTracker(
        MercadoBitcoinClient client,
        OrderTrackerOptions? options = null,
        ILogger<OrderTracker>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? new OrderTrackerOptions();
        _logger = logger;

        if (_options.EnablePolling)
        {
            StartPolling();
        }
    }

    #region Properties

    /// <summary>
    /// Gets the number of currently tracked orders.
    /// </summary>
    public int ActiveOrderCount => _orders.Count;

    /// <summary>
    /// Gets all currently tracked order IDs.
    /// </summary>
    public IEnumerable<string> TrackedOrderIds => _orders.Keys;

    /// <summary>
    /// Gets the total number of orders tracked since creation.
    /// </summary>
    public long TotalOrdersTracked => Volatile.Read(ref _ordersTracked);

    /// <summary>
    /// Gets the number of orders that were filled.
    /// </summary>
    public long OrdersFilledCount => Volatile.Read(ref _ordersFilled);

    /// <summary>
    /// Gets the number of orders that were cancelled.
    /// </summary>
    public long OrdersCancelledCount => Volatile.Read(ref _ordersCancelled);

    /// <summary>
    /// Gets the number of orders that expired from tracking.
    /// </summary>
    public long OrdersExpiredCount => Volatile.Read(ref _ordersExpired);

    /// <summary>
    /// Gets the average polling latency in milliseconds.
    /// </summary>
    public double AveragePollingLatencyMs
    {
        get
        {
            var count = Volatile.Read(ref _pollCount);
            if (count == 0) return 0;
            return (double)Volatile.Read(ref _totalPollLatencyMs) / count;
        }
    }

    #endregion

    #region Tracking Methods

    /// <summary>
    /// Starts tracking an order.
    /// </summary>
    /// <param name="orderId">The order ID to track.</param>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="initialStatus">Initial order status if known.</param>
    /// <param name="metadata">Optional custom metadata.</param>
    /// <returns>The tracked order info.</returns>
    public TrackedOrderInfo Track(
        string orderId,
        string symbol,
        string accountId,
        TrackedOrderStatus initialStatus = TrackedOrderStatus.Pending,
        IDictionary<string, object>? metadata = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var info = new TrackedOrderInfo
        {
            OrderId = orderId,
            Symbol = symbol,
            AccountId = accountId,
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow,
            LastChecked = DateTime.UtcNow,
            Metadata = metadata != null ? new Dictionary<string, object>(metadata) : new Dictionary<string, object>()
        };

        if (_orders.TryAdd(orderId, info))
        {
            Interlocked.Increment(ref _ordersTracked);
            _logger?.LogDebug("Started tracking order {OrderId} for {Symbol}", orderId, symbol);
        }
        else
        {
            _logger?.LogWarning("Order {OrderId} is already being tracked", orderId);
        }

        return info;
    }

    /// <summary>
    /// Tracks an order and immediately starts monitoring it for status changes.
    /// </summary>
    /// <param name="orderId">The order ID to track.</param>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when order reaches a terminal state.</returns>
    public async Task<TrackedOrderInfo> TrackAndMonitorAsync(
        string orderId,
        string symbol,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var info = Track(orderId, symbol, accountId);

        // Start individual monitoring for this order
        await MonitorOrderAsync(orderId, accountId, cancellationToken);

        return _orders.TryGetValue(orderId, out var finalInfo) ? finalInfo : info;
    }

    /// <summary>
    /// Stops tracking an order.
    /// </summary>
    /// <param name="orderId">The order ID to stop tracking.</param>
    /// <returns>True if the order was being tracked and is now removed.</returns>
    public bool Untrack(string orderId)
    {
        if (_monitoringTasks.TryRemove(orderId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        var removed = _orders.TryRemove(orderId, out _);
        if (removed)
        {
            _logger?.LogDebug("Stopped tracking order {OrderId}", orderId);
        }

        return removed;
    }

    /// <summary>
    /// Gets the current info for a tracked order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>Order info if found, null otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TrackedOrderInfo? GetOrder(string orderId)
    {
        return _orders.TryGetValue(orderId, out var info) ? info : null;
    }

    /// <summary>
    /// Checks if an order is being tracked.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>True if the order is being tracked.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTracking(string orderId)
    {
        return _orders.ContainsKey(orderId);
    }

    /// <summary>
    /// Gets all tracked orders with a specific status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>Enumerable of matching orders.</returns>
    public IEnumerable<TrackedOrderInfo> GetOrdersByStatus(TrackedOrderStatus status)
    {
        return _orders.Values.Where(o => o.Status == status);
    }

    /// <summary>
    /// Gets all tracked orders for a specific symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <returns>Enumerable of matching orders.</returns>
    public IEnumerable<TrackedOrderInfo> GetOrdersBySymbol(string symbol)
    {
        return _orders.Values.Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Manually updates the status of a tracked order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="filledQuantity">Optional filled quantity.</param>
    /// <param name="filledPrice">Optional filled price.</param>
    /// <returns>True if the order was found and updated.</returns>
    public bool UpdateStatus(
        string orderId,
        TrackedOrderStatus newStatus,
        decimal? filledQuantity = null,
        decimal? filledPrice = null)
    {
        if (!_orders.TryGetValue(orderId, out var info))
        {
            return false;
        }

        var previousStatus = info.Status;
        info.Status = newStatus;
        info.LastChecked = DateTime.UtcNow;

        if (filledQuantity.HasValue)
        {
            info.FilledQuantity = filledQuantity.Value;
        }

        if (filledPrice.HasValue)
        {
            info.FilledPrice = filledPrice.Value;
        }

        if (previousStatus != newStatus)
        {
            info.StatusHistory.Add(new StatusChange
            {
                FromStatus = previousStatus,
                ToStatus = newStatus,
                Timestamp = DateTime.UtcNow
            });

            OnOrderStatusChanged(new OrderStatusChangedEventArgs
            {
                OrderId = orderId,
                Symbol = info.Symbol,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                FilledQuantity = info.FilledQuantity,
                Timestamp = DateTime.UtcNow
            });

            // Handle terminal states
            if (newStatus == TrackedOrderStatus.Filled)
            {
                Interlocked.Increment(ref _ordersFilled);
                OnOrderFilled(new OrderFilledEventArgs
                {
                    OrderId = orderId,
                    Symbol = info.Symbol,
                    FilledQuantity = info.FilledQuantity,
                    FilledPrice = info.FilledPrice,
                    Timestamp = DateTime.UtcNow
                });
            }
            else if (newStatus == TrackedOrderStatus.Cancelled)
            {
                Interlocked.Increment(ref _ordersCancelled);
                OnOrderCancelled(new OrderTrackerCancelledEventArgs
                {
                    OrderId = orderId,
                    Symbol = info.Symbol,
                    Reason = "Status update",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        return true;
    }

    /// <summary>
    /// Refreshes the status of a specific order from the API.
    /// </summary>
    /// <param name="orderId">The order ID to refresh.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the order was found and refreshed.</returns>
    public async Task<bool> RefreshOrderAsync(
        string orderId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        if (!_orders.TryGetValue(orderId, out var info))
        {
            return false;
        }

        try
        {
            var sw = Stopwatch.StartNew();
            var order = await _client.GetOrderAsync(info.Symbol, accountId, orderId, cancellationToken);
            sw.Stop();

            Interlocked.Increment(ref _pollCount);
            Interlocked.Add(ref _totalPollLatencyMs, sw.ElapsedMilliseconds);

            if (order != null)
            {
                var newStatus = MapOrderStatus(order.Status);
                decimal? filledQty = decimal.TryParse(order.FilledQty, out var fq) ? fq : null;
                decimal? avgPrice = order.AvgPrice.HasValue ? (decimal)order.AvgPrice.Value : null;
                UpdateStatus(orderId, newStatus, filledQty, avgPrice);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to refresh order {OrderId}", orderId);
            OnTrackingError(new OrderTrackingErrorEventArgs
            {
                OrderId = orderId,
                Error = ex.Message,
                Exception = ex,
                Timestamp = DateTime.UtcNow
            });
        }

        return false;
    }

    /// <summary>
    /// Refreshes all tracked orders from the API.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of orders refreshed.</returns>
    public async Task<int> RefreshAllOrdersAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var refreshed = 0;
        var orderIds = _orders.Keys.ToList();

        foreach (var orderId in orderIds)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (await RefreshOrderAsync(orderId, accountId, cancellationToken))
            {
                refreshed++;
            }

            // Small delay to avoid rate limiting
            await Task.Delay(50, cancellationToken);
        }

        return refreshed;
    }

    #endregion

    #region Monitoring

    /// <summary>
    /// Monitors a specific order until it reaches a terminal state.
    /// </summary>
    private async Task MonitorOrderAsync(
        string orderId,
        string accountId,
        CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitoringTasks[orderId] = cts;

        try
        {
            var attempts = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                if (!_orders.TryGetValue(orderId, out var info))
                {
                    break; // Order no longer tracked
                }

                // Check if terminal state
                if (info.Status is TrackedOrderStatus.Filled or TrackedOrderStatus.Cancelled or TrackedOrderStatus.Rejected or TrackedOrderStatus.Expired)
                {
                    break;
                }

                // Check timeout
                if (DateTime.UtcNow - info.CreatedAt > _options.TrackingTimeout)
                {
                    UpdateStatus(orderId, TrackedOrderStatus.Expired);
                    Interlocked.Increment(ref _ordersExpired);
                    break;
                }

                // Refresh from API
                await RefreshOrderAsync(orderId, accountId, cts.Token);
                attempts++;

                // Calculate backoff
                var delay = CalculateBackoff(attempts);
                await Task.Delay(delay, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error monitoring order {OrderId}", orderId);
        }
        finally
        {
            _monitoringTasks.TryRemove(orderId, out _);
            cts.Dispose();
        }
    }

    private void StartPolling()
    {
        _pollingTimer = new Timer(
            OnPollingTimerCallback,
            null,
            _options.PollingInterval,
            _options.PollingInterval);

        _logger?.LogInformation("Started order polling with interval {Interval}", _options.PollingInterval);
    }

    private async void OnPollingTimerCallback(object? state)
    {
        if (_disposed || _orders.IsEmpty) return;

        var acquired = await _pollLock.WaitAsync(0);
        if (!acquired) return; // Skip if previous poll still running

        try
        {
            var expiredOrders = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var (orderId, info) in _orders)
            {
                // Skip terminal states
                if (info.Status is TrackedOrderStatus.Filled or TrackedOrderStatus.Cancelled or TrackedOrderStatus.Rejected or TrackedOrderStatus.Expired)
                {
                    if (_options.AutoRemoveCompleted && now - info.LastChecked > _options.CompletedOrderRetention)
                    {
                        expiredOrders.Add(orderId);
                    }
                    continue;
                }

                // Check for tracking timeout
                if (now - info.CreatedAt > _options.TrackingTimeout)
                {
                    UpdateStatus(orderId, TrackedOrderStatus.Expired);
                    Interlocked.Increment(ref _ordersExpired);
                    continue;
                }

                // Refresh if needed
                if (now - info.LastChecked >= _options.PollingInterval)
                {
                    await RefreshOrderAsync(orderId, info.AccountId, CancellationToken.None);
                }
            }

            // Remove expired completed orders
            foreach (var orderId in expiredOrders)
            {
                Untrack(orderId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in polling timer callback");
        }
        finally
        {
            _pollLock.Release();
        }
    }

    private TimeSpan CalculateBackoff(int attempt)
    {
        // Exponential backoff: 100ms, 200ms, 400ms, 800ms, max 5s
        var ms = Math.Min(_options.MinPollInterval.TotalMilliseconds * Math.Pow(2, attempt - 1), _options.MaxPollInterval.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(ms);
    }

    #endregion

    #region Helpers

    private static TrackedOrderStatus MapOrderStatus(string? apiStatus)
    {
        return apiStatus?.ToUpperInvariant() switch
        {
            "PENDING" => TrackedOrderStatus.Pending,
            "OPEN" => TrackedOrderStatus.Open,
            "WORKING" => TrackedOrderStatus.Open,
            "PARTIALLY_FILLED" => TrackedOrderStatus.PartiallyFilled,
            "FILLED" => TrackedOrderStatus.Filled,
            "CANCELLED" => TrackedOrderStatus.Cancelled,
            "CANCELED" => TrackedOrderStatus.Cancelled,
            "REJECTED" => TrackedOrderStatus.Rejected,
            "EXPIRED" => TrackedOrderStatus.Expired,
            _ => TrackedOrderStatus.Unknown
        };
    }

    /// <summary>
    /// Gets tracker statistics.
    /// </summary>
    /// <returns>Current statistics.</returns>
    public OrderTrackerStats GetStats()
    {
        return new OrderTrackerStats
        {
            ActiveOrders = _orders.Count,
            TotalTracked = _ordersTracked,
            Filled = _ordersFilled,
            Cancelled = _ordersCancelled,
            Expired = _ordersExpired,
            PollCount = _pollCount,
            AveragePollingLatencyMs = AveragePollingLatencyMs
        };
    }

    #endregion

    #region Events

    private void OnOrderStatusChanged(OrderStatusChangedEventArgs args)
    {
        _logger?.LogDebug(
            "Order {OrderId} status changed: {PreviousStatus} -> {NewStatus}",
            args.OrderId, args.PreviousStatus, args.NewStatus);

        OrderStatusChanged?.Invoke(this, args);
    }

    private void OnOrderFilled(OrderFilledEventArgs args)
    {
        _logger?.LogInformation(
            "Order {OrderId} filled: {Quantity} @ {Price}",
            args.OrderId, args.FilledQuantity, args.FilledPrice);

        OrderFilled?.Invoke(this, args);
    }

    private void OnOrderCancelled(OrderTrackerCancelledEventArgs args)
    {
        _logger?.LogInformation(
            "Order {OrderId} cancelled: {Reason}",
            args.OrderId, args.Reason);

        OrderCancelled?.Invoke(this, args);
    }

    private void OnTrackingError(OrderTrackingErrorEventArgs args)
    {
        TrackingError?.Invoke(this, args);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pollingTimer?.Dispose();
        _pollLock.Dispose();

        foreach (var cts in _monitoringTasks.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _monitoringTasks.Clear();
        _orders.Clear();
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_pollingTimer != null)
        {
            await _pollingTimer.DisposeAsync();
        }

        _pollLock.Dispose();

        foreach (var cts in _monitoringTasks.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _monitoringTasks.Clear();
        _orders.Clear();
    }

    #endregion
}

#region Options and Models

/// <summary>
/// Configuration options for OrderTracker.
/// </summary>
public sealed class OrderTrackerOptions
{
    /// <summary>
    /// Whether to enable automatic polling. Default: true.
    /// </summary>
    public bool EnablePolling { get; set; } = true;

    /// <summary>
    /// Interval between polling cycles. Default: 1 second.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Minimum interval between individual order polls. Default: 100ms.
    /// </summary>
    public TimeSpan MinPollInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum interval for backoff. Default: 5 seconds.
    /// </summary>
    public TimeSpan MaxPollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long to track an order before timing out. Default: 1 hour.
    /// </summary>
    public TimeSpan TrackingTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Whether to automatically remove completed orders. Default: true.
    /// </summary>
    public bool AutoRemoveCompleted { get; set; } = true;

    /// <summary>
    /// How long to keep completed orders before removal. Default: 5 minutes.
    /// </summary>
    public TimeSpan CompletedOrderRetention { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Status of a tracked order.
/// </summary>
public enum TrackedOrderStatus
{
    /// <summary>
    /// Order status is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Order is pending submission.
    /// </summary>
    Pending,

    /// <summary>
    /// Order is open and active.
    /// </summary>
    Open,

    /// <summary>
    /// Order is partially filled.
    /// </summary>
    PartiallyFilled,

    /// <summary>
    /// Order is completely filled.
    /// </summary>
    Filled,

    /// <summary>
    /// Order was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Order was rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Order tracking expired.
    /// </summary>
    Expired
}

/// <summary>
/// Information about a tracked order.
/// </summary>
public sealed class TrackedOrderInfo
{
    /// <summary>
    /// The order ID.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// The account ID.
    /// </summary>
    public required string AccountId { get; init; }

    /// <summary>
    /// Current order status.
    /// </summary>
    public TrackedOrderStatus Status { get; set; }

    /// <summary>
    /// When tracking started.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the order was last checked.
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// Filled quantity.
    /// </summary>
    public decimal FilledQuantity { get; set; }

    /// <summary>
    /// Average filled price.
    /// </summary>
    public decimal FilledPrice { get; set; }

    /// <summary>
    /// Custom metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Status change history.
    /// </summary>
    public List<StatusChange> StatusHistory { get; } = new();

    /// <summary>
    /// Gets the duration the order has been tracked.
    /// </summary>
    public TimeSpan TrackedDuration => DateTime.UtcNow - CreatedAt;
}

/// <summary>
/// Represents a status change in the order history.
/// </summary>
public sealed class StatusChange
{
    /// <summary>
    /// The previous status.
    /// </summary>
    public TrackedOrderStatus FromStatus { get; init; }

    /// <summary>
    /// The new status.
    /// </summary>
    public TrackedOrderStatus ToStatus { get; init; }

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Order tracker statistics.
/// </summary>
public sealed class OrderTrackerStats
{
    /// <summary>
    /// Number of currently active orders.
    /// </summary>
    public int ActiveOrders { get; init; }

    /// <summary>
    /// Total orders tracked.
    /// </summary>
    public long TotalTracked { get; init; }

    /// <summary>
    /// Orders that were filled.
    /// </summary>
    public long Filled { get; init; }

    /// <summary>
    /// Orders that were cancelled.
    /// </summary>
    public long Cancelled { get; init; }

    /// <summary>
    /// Orders that expired.
    /// </summary>
    public long Expired { get; init; }

    /// <summary>
    /// Number of API polls made.
    /// </summary>
    public long PollCount { get; init; }

    /// <summary>
    /// Average polling latency in milliseconds.
    /// </summary>
    public double AveragePollingLatencyMs { get; init; }
}

#endregion

#region Event Arguments

/// <summary>
/// Event arguments for order status changes.
/// </summary>
public sealed class OrderStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// The order ID.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// The previous status.
    /// </summary>
    public required TrackedOrderStatus PreviousStatus { get; init; }

    /// <summary>
    /// The new status.
    /// </summary>
    public required TrackedOrderStatus NewStatus { get; init; }

    /// <summary>
    /// Current filled quantity.
    /// </summary>
    public decimal FilledQuantity { get; init; }

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Event arguments for filled orders.
/// </summary>
public sealed class OrderFilledEventArgs : EventArgs
{
    /// <summary>
    /// The order ID.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// The filled quantity.
    /// </summary>
    public decimal FilledQuantity { get; init; }

    /// <summary>
    /// The filled price.
    /// </summary>
    public decimal FilledPrice { get; init; }

    /// <summary>
    /// When the fill occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Event arguments for cancelled orders in the order tracker.
/// </summary>
public sealed class OrderTrackerCancelledEventArgs : EventArgs
{
    /// <summary>
    /// The order ID.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// The trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// The cancellation reason.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// When the cancellation occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Event arguments for tracking errors.
/// </summary>
public sealed class OrderTrackingErrorEventArgs : EventArgs
{
    /// <summary>
    /// The order ID if known.
    /// </summary>
    public string? OrderId { get; init; }

    /// <summary>
    /// The error message.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// The exception if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// When the error occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

#endregion

#region Extensions

/// <summary>
/// Extension methods for OrderTracker.
/// </summary>
public static class OrderTrackerExtensions
{
    /// <summary>
    /// Waits for an order to reach a terminal state.
    /// </summary>
    /// <param name="tracker">The order tracker.</param>
    /// <param name="orderId">The order ID to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final order info or null if timed out.</returns>
    public static async Task<TrackedOrderInfo?> WaitForCompletionAsync(
        this OrderTracker tracker,
        string orderId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var info = tracker.GetOrder(orderId);
                if (info == null) return null;

                if (info.Status is TrackedOrderStatus.Filled or TrackedOrderStatus.Cancelled or TrackedOrderStatus.Rejected or TrackedOrderStatus.Expired)
                {
                    return info;
                }

                await Task.Delay(100, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on timeout
        }

        return tracker.GetOrder(orderId);
    }

    /// <summary>
    /// Gets all orders that are still active (not in terminal state).
    /// </summary>
    /// <param name="tracker">The order tracker.</param>
    /// <returns>Enumerable of active orders.</returns>
    public static IEnumerable<TrackedOrderInfo> GetActiveOrders(this OrderTracker tracker)
    {
        return tracker.TrackedOrderIds
            .Select(id => tracker.GetOrder(id))
            .Where(o => o != null && o.Status is TrackedOrderStatus.Pending or TrackedOrderStatus.Open or TrackedOrderStatus.PartiallyFilled)
            .Cast<TrackedOrderInfo>();
    }
}

#endregion
