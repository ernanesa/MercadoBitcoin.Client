using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MercadoBitcoin.Client.Diagnostics;
using MercadoBitcoin.Client.Generated;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// High-performance order manager optimized for low-latency trading.
/// Uses object pooling, pre-built templates, and lock-free data structures.
/// </summary>
public sealed class HighPerformanceOrderManager : IDisposable
{
    private readonly MercadoBitcoinClient _client;
    private readonly string _accountId;
    private readonly ILogger<HighPerformanceOrderManager>? _logger;

    // Pool of pre-allocated order requests to minimize allocations
    private readonly ConcurrentBag<PlaceOrderRequest> _orderPool = new();

    // Active orders tracking (lock-free)
    private readonly ConcurrentDictionary<string, TrackedOrder> _activeOrders = new();

    // Counter for generating unique external IDs (lock-free)
    private long _orderCounter;

    // Performance metrics
    private long _ordersPlaced;
    private long _ordersCancelled;
    private long _ordersFilled;
    private long _ordersFailed;

    /// <summary>
    /// Event raised when an order is placed.
    /// </summary>
    public event EventHandler<OrderPlacedEventArgs>? OrderPlaced;

    /// <summary>
    /// Event raised when an order is cancelled.
    /// </summary>
    public event EventHandler<OrderCancelledEventArgs>? OrderCancelled;

    /// <summary>
    /// Event raised when an order fails.
    /// </summary>
    public event EventHandler<OrderFailedEventArgs>? OrderFailed;

    /// <summary>
    /// Creates a new high-performance order manager.
    /// </summary>
    /// <param name="client">The MercadoBitcoin client.</param>
    /// <param name="accountId">The account ID to use for orders.</param>
    /// <param name="poolSize">Initial size of the order request pool. Default: 100.</param>
    /// <param name="logger">Optional logger.</param>
    public HighPerformanceOrderManager(
        MercadoBitcoinClient client,
        string accountId,
        int poolSize = 100,
        ILogger<HighPerformanceOrderManager>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _accountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        _logger = logger;

        // Pre-allocate order requests
        for (var i = 0; i < poolSize; i++)
        {
            _orderPool.Add(CreateNewRequest());
        }

        _logger?.LogInformation(
            "HighPerformanceOrderManager initialized for account {AccountId} with pool size {PoolSize}",
            accountId[..Math.Min(8, accountId.Length)] + "...",
            poolSize);
    }

    #region Properties

    /// <summary>
    /// Gets the number of orders placed.
    /// </summary>
    public long OrdersPlaced => Volatile.Read(ref _ordersPlaced);

    /// <summary>
    /// Gets the number of orders cancelled.
    /// </summary>
    public long OrdersCancelled => Volatile.Read(ref _ordersCancelled);

    /// <summary>
    /// Gets the number of orders filled.
    /// </summary>
    public long OrdersFilled => Volatile.Read(ref _ordersFilled);

    /// <summary>
    /// Gets the number of failed orders.
    /// </summary>
    public long OrdersFailed => Volatile.Read(ref _ordersFailed);

    /// <summary>
    /// Gets the number of active (pending) orders.
    /// </summary>
    public int ActiveOrderCount => _activeOrders.Count;

    /// <summary>
    /// Gets all active orders.
    /// </summary>
    public IReadOnlyCollection<TrackedOrder> ActiveOrders => _activeOrders.Values.ToList();

    #endregion

    #region Order Placement

    /// <summary>
    /// Places a limit buy order with minimal latency.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="price">Limit price.</param>
    /// <param name="externalId">Optional external ID. Auto-generated if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response with order ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PlaceOrderResponse> PlaceBuyOrderAsync(
        string symbol,
        decimal quantity,
        decimal price,
        string? externalId = null,
        CancellationToken ct = default)
    {
        return PlaceOrderInternalAsync(symbol, "buy", "limit", quantity, price, null, externalId, ct);
    }

    /// <summary>
    /// Places a limit sell order with minimal latency.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="price">Limit price.</param>
    /// <param name="externalId">Optional external ID. Auto-generated if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response with order ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PlaceOrderResponse> PlaceSellOrderAsync(
        string symbol,
        decimal quantity,
        decimal price,
        string? externalId = null,
        CancellationToken ct = default)
    {
        return PlaceOrderInternalAsync(symbol, "sell", "limit", quantity, price, null, externalId, ct);
    }

    /// <summary>
    /// Places a market buy order for immediate execution.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="cost">Amount in quote currency (e.g., BRL) to spend.</param>
    /// <param name="externalId">Optional external ID. Auto-generated if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response with order ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PlaceOrderResponse> PlaceMarketBuyAsync(
        string symbol,
        decimal cost,
        string? externalId = null,
        CancellationToken ct = default)
    {
        return PlaceOrderInternalAsync(symbol, "buy", "market", null, null, cost, externalId, ct);
    }

    /// <summary>
    /// Places a market sell order for immediate execution.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="quantity">Quantity to sell.</param>
    /// <param name="externalId">Optional external ID. Auto-generated if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response with order ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PlaceOrderResponse> PlaceMarketSellAsync(
        string symbol,
        decimal quantity,
        string? externalId = null,
        CancellationToken ct = default)
    {
        return PlaceOrderInternalAsync(symbol, "sell", "market", quantity, null, null, externalId, ct);
    }

    /// <summary>
    /// Places a stop-limit order.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="side">"buy" or "sell".</param>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="stopPrice">Price that triggers the order.</param>
    /// <param name="limitPrice">Limit price after trigger.</param>
    /// <param name="externalId">Optional external ID. Auto-generated if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order response with order ID.</returns>
    public async ValueTask<PlaceOrderResponse> PlaceStopLimitOrderAsync(
        string symbol,
        string side,
        decimal quantity,
        decimal stopPrice,
        decimal limitPrice,
        string? externalId = null,
        CancellationToken ct = default)
    {
        var request = GetOrCreateRequest();
        try
        {
            request.Side = side;
            request.Type = "stoplimit";
            request.Qty = FormatDecimal(quantity);
            request.StopPrice = (double)stopPrice;
            request.LimitPrice = (double)limitPrice;
            request.ExternalId = externalId ?? GenerateOrderId();
            request.Async = true;

            return await ExecuteOrderAsync(symbol, request, ct);
        }
        finally
        {
            ReturnRequest(request);
        }
    }

    private async ValueTask<PlaceOrderResponse> PlaceOrderInternalAsync(
        string symbol,
        string side,
        string type,
        decimal? quantity,
        decimal? limitPrice,
        decimal? cost,
        string? externalId,
        CancellationToken ct)
    {
        var request = GetOrCreateRequest();
        try
        {
            request.Side = side;
            request.Type = type;
            request.Qty = quantity.HasValue ? FormatDecimal(quantity.Value) : null;
            request.LimitPrice = limitPrice.HasValue ? (double)limitPrice.Value : null;
            request.Cost = cost.HasValue ? (double)cost.Value : null;
            request.ExternalId = externalId ?? GenerateOrderId();
            request.Async = true; // Always async for lower latency

            return await ExecuteOrderAsync(symbol, request, ct);
        }
        finally
        {
            ReturnRequest(request);
        }
    }

    private async Task<PlaceOrderResponse> ExecuteOrderAsync(
        string symbol,
        PlaceOrderRequest request,
        CancellationToken ct)
    {
        using var activity = MercadoBitcoinTelemetry.StartTradingActivity(
            "PlaceOrder", symbol, request.Side, request.Type);

        var sw = Stopwatch.StartNew();

        try
        {
            _logger?.LogDebug(
                "Placing {Side} {Type} order for {Symbol}: qty={Qty}, price={Price}, cost={Cost}",
                request.Side, request.Type, symbol, request.Qty, request.LimitPrice, request.Cost);

            var response = await _client.PlaceOrderAsync(symbol, _accountId, request, ct);
            sw.Stop();

            Interlocked.Increment(ref _ordersPlaced);

            // Track the order
            var trackedOrder = new TrackedOrder
            {
                OrderId = response.OrderId ?? string.Empty,
                ExternalId = request.ExternalId ?? string.Empty,
                Symbol = symbol,
                Side = request.Side ?? string.Empty,
                Type = request.Type ?? string.Empty,
                Quantity = request.Qty,
                LimitPrice = request.LimitPrice.HasValue ? (decimal)request.LimitPrice.Value : null,
                Cost = request.Cost.HasValue ? (decimal)request.Cost.Value : null,
                PlacedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            _activeOrders[response.OrderId ?? string.Empty] = trackedOrder;

            // Record telemetry
            MercadoBitcoinTelemetry.RecordOrderPlaced(symbol, request.Side ?? "unknown", request.Type ?? "unknown");
            MercadoBitcoinTelemetry.RecordOrderExecutionLatency(symbol, sw.ElapsedMilliseconds, request.Type ?? "unknown");
            activity?.WithOrderId(response.OrderId ?? "unknown").WithLatency(sw.ElapsedMilliseconds).MarkSuccess();

            _logger?.LogInformation(
                "Order placed successfully: {OrderId} in {Latency}ms",
                response.OrderId, sw.ElapsedMilliseconds);

            OnOrderPlaced(new OrderPlacedEventArgs
            {
                OrderId = response.OrderId ?? string.Empty,
                ExternalId = request.ExternalId ?? string.Empty,
                Symbol = symbol,
                Side = request.Side ?? string.Empty,
                Type = request.Type ?? string.Empty,
                LatencyMs = sw.ElapsedMilliseconds
            });

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Interlocked.Increment(ref _ordersFailed);

            _logger?.LogError(ex,
                "Order placement failed for {Symbol} after {Latency}ms: {Error}",
                symbol, sw.ElapsedMilliseconds, ex.Message);

            activity?.MarkError(ex);

            OnOrderFailed(new OrderFailedEventArgs
            {
                Symbol = symbol,
                Side = request.Side ?? string.Empty,
                Type = request.Type ?? string.Empty,
                Error = ex.Message,
                Exception = ex
            });

            throw;
        }
    }

    #endregion

    #region Order Cancellation

    /// <summary>
    /// Cancels an order with aggressive timeout.
    /// </summary>
    /// <param name="symbol">Trading pair symbol.</param>
    /// <param name="orderId">Order ID to cancel.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Default: 2000ms.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if cancellation was successful.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<bool> CancelOrderFastAsync(
        string symbol,
        string orderId,
        int timeoutMs = 2000,
        CancellationToken ct = default)
    {
        using var activity = MercadoBitcoinTelemetry.StartTradingActivity("CancelOrder", symbol);
        activity?.WithOrderId(orderId);

        var sw = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            _logger?.LogDebug("Cancelling order {OrderId} for {Symbol}", orderId, symbol);

            await _client.CancelOrderAsync(_accountId, symbol, orderId, async: true, cts.Token);
            sw.Stop();

            Interlocked.Increment(ref _ordersCancelled);

            // Remove from active orders
            _activeOrders.TryRemove(orderId, out _);

            MercadoBitcoinTelemetry.RecordOrderCancelled(symbol);
            activity?.WithLatency(sw.ElapsedMilliseconds).MarkSuccess();

            _logger?.LogInformation("Order {OrderId} cancelled in {Latency}ms", orderId, sw.ElapsedMilliseconds);

            OnOrderCancelled(new OrderCancelledEventArgs
            {
                OrderId = orderId,
                Symbol = symbol,
                LatencyMs = sw.ElapsedMilliseconds
            });

            return true;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger?.LogWarning(
                "Order cancellation timed out after {Timeout}ms for order {OrderId}",
                timeoutMs, orderId);

            activity?.MarkError("Timeout");
            return false; // Timeout - order may or may not have been cancelled
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogError(ex, "Failed to cancel order {OrderId}: {Error}", orderId, ex.Message);
            activity?.MarkError(ex);
            return false;
        }
    }

    /// <summary>
    /// Cancels multiple orders in parallel.
    /// </summary>
    /// <param name="symbol">Trading pair symbol.</param>
    /// <param name="orderIds">Order IDs to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of successfully cancelled orders.</returns>
    public async Task<int> CancelOrdersBatchAsync(
        string symbol,
        IEnumerable<string> orderIds,
        CancellationToken ct = default)
    {
        var tasks = orderIds.Select(id => CancelOrderFastAsync(symbol, id, ct: ct).AsTask()).ToList();
        await Task.WhenAll(tasks);
        return tasks.Count(t => t.Result);
    }

    /// <summary>
    /// Cancels all active orders for a symbol.
    /// </summary>
    /// <param name="symbol">Trading pair symbol. If null, cancels all orders.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of cancellation requests sent.</returns>
    public async ValueTask<int> CancelAllOrdersAsync(
        string? symbol = null,
        CancellationToken ct = default)
    {
        var ordersToCancel = symbol is null
            ? _activeOrders.Values.ToList()
            : _activeOrders.Values.Where(o => o.Symbol == symbol).ToList();

        if (ordersToCancel.Count == 0)
        {
            _logger?.LogDebug("No active orders to cancel");
            return 0;
        }

        _logger?.LogInformation("Cancelling {Count} orders for {Symbol}", ordersToCancel.Count, symbol ?? "all symbols");

        var tasks = ordersToCancel.Select(o => CancelOrderFastAsync(o.Symbol, o.OrderId, ct: ct).AsTask()).ToList();
        await Task.WhenAll(tasks);
        return tasks.Count(t => t.Result);
    }

    #endregion

    #region Order Tracking

    /// <summary>
    /// Updates the status of a tracked order.
    /// </summary>
    /// <param name="orderId">Order ID.</param>
    /// <param name="status">New status.</param>
    /// <param name="filledQuantity">Filled quantity (optional).</param>
    public void UpdateOrderStatus(string orderId, OrderStatus status, decimal? filledQuantity = null)
    {
        if (_activeOrders.TryGetValue(orderId, out var order))
        {
            order.Status = status;
            if (filledQuantity.HasValue)
            {
                order.FilledQuantity = filledQuantity.Value;
            }

            if (status == OrderStatus.Filled)
            {
                Interlocked.Increment(ref _ordersFilled);
                _activeOrders.TryRemove(orderId, out _);
            }
            else if (status == OrderStatus.Cancelled || status == OrderStatus.Rejected)
            {
                _activeOrders.TryRemove(orderId, out _);
            }
        }
    }

    /// <summary>
    /// Gets a tracked order by ID.
    /// </summary>
    /// <param name="orderId">Order ID.</param>
    /// <returns>The tracked order, or null if not found.</returns>
    public TrackedOrder? GetOrder(string orderId)
    {
        _activeOrders.TryGetValue(orderId, out var order);
        return order;
    }

    /// <summary>
    /// Gets a tracked order by external ID.
    /// </summary>
    /// <param name="externalId">External ID.</param>
    /// <returns>The tracked order, or null if not found.</returns>
    public TrackedOrder? GetOrderByExternalId(string externalId)
    {
        return _activeOrders.Values.FirstOrDefault(o => o.ExternalId == externalId);
    }

    #endregion

    #region Pool Management

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlaceOrderRequest GetOrCreateRequest()
    {
        if (_orderPool.TryTake(out var request))
        {
            return request;
        }
        return CreateNewRequest();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PlaceOrderRequest CreateNewRequest()
    {
        return new PlaceOrderRequest { Async = true };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnRequest(PlaceOrderRequest request)
    {
        // Clear the request before returning to pool
        request.Qty = null;
        request.Cost = null;
        request.LimitPrice = null;
        request.StopPrice = null;
        request.ExternalId = null;
        request.Side = null;
        request.Type = null;
        request.Async = true;

        _orderPool.Add(request);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GenerateOrderId()
    {
        var id = Interlocked.Increment(ref _orderCounter);
        var timestamp = DateTime.UtcNow.Ticks;
        return $"HP{timestamp:X12}{id:X8}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FormatDecimal(decimal value)
    {
        return value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion

    #region Events

    private void OnOrderPlaced(OrderPlacedEventArgs args)
    {
        OrderPlaced?.Invoke(this, args);
    }

    private void OnOrderCancelled(OrderCancelledEventArgs args)
    {
        OrderCancelled?.Invoke(this, args);
    }

    private void OnOrderFailed(OrderFailedEventArgs args)
    {
        OrderFailed?.Invoke(this, args);
    }

    #endregion

    /// <summary>
    /// Disposes the order manager.
    /// </summary>
    public void Dispose()
    {
        _orderPool.Clear();
        _activeOrders.Clear();
    }
}

#region Supporting Types

/// <summary>
/// Represents a tracked order.
/// </summary>
public sealed class TrackedOrder
{
    /// <summary>
    /// The order ID from the exchange.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// The client-generated external ID.
    /// </summary>
    public required string ExternalId { get; init; }

    /// <summary>
    /// Trading pair symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Order side: "buy" or "sell".
    /// </summary>
    public required string Side { get; init; }

    /// <summary>
    /// Order type: "limit", "market", "stoplimit".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Order quantity (as string).
    /// </summary>
    public string? Quantity { get; init; }

    /// <summary>
    /// Limit price.
    /// </summary>
    public decimal? LimitPrice { get; init; }

    /// <summary>
    /// Cost (for market buy orders).
    /// </summary>
    public decimal? Cost { get; init; }

    /// <summary>
    /// When the order was placed.
    /// </summary>
    public required DateTime PlacedAt { get; init; }

    /// <summary>
    /// Current order status.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Filled quantity.
    /// </summary>
    public decimal FilledQuantity { get; set; }
}

/// <summary>
/// Order status enumeration.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order is pending.</summary>
    Pending,
    /// <summary>Order is open on the exchange.</summary>
    Open,
    /// <summary>Order is partially filled.</summary>
    PartiallyFilled,
    /// <summary>Order is fully filled.</summary>
    Filled,
    /// <summary>Order was cancelled.</summary>
    Cancelled,
    /// <summary>Order was rejected.</summary>
    Rejected
}

/// <summary>
/// Event arguments for order placed event.
/// </summary>
public sealed class OrderPlacedEventArgs : EventArgs
{
    /// <summary>Order ID from exchange.</summary>
    public required string OrderId { get; init; }
    /// <summary>External ID.</summary>
    public required string ExternalId { get; init; }
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Order side.</summary>
    public required string Side { get; init; }
    /// <summary>Order type.</summary>
    public required string Type { get; init; }
    /// <summary>Latency in milliseconds.</summary>
    public required long LatencyMs { get; init; }
}

/// <summary>
/// Event arguments for order cancelled event.
/// </summary>
public sealed class OrderCancelledEventArgs : EventArgs
{
    /// <summary>Order ID.</summary>
    public required string OrderId { get; init; }
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Latency in milliseconds.</summary>
    public required long LatencyMs { get; init; }
}

/// <summary>
/// Event arguments for order failed event.
/// </summary>
public sealed class OrderFailedEventArgs : EventArgs
{
    /// <summary>Trading pair symbol.</summary>
    public required string Symbol { get; init; }
    /// <summary>Order side.</summary>
    public required string Side { get; init; }
    /// <summary>Order type.</summary>
    public required string Type { get; init; }
    /// <summary>Error message.</summary>
    public required string Error { get; init; }
    /// <summary>Exception that caused the failure.</summary>
    public Exception? Exception { get; init; }
}

#endregion
