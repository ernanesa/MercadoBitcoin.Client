using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace MercadoBitcoin.Client.Diagnostics;

/// <summary>
/// OpenTelemetry instrumentation for MercadoBitcoin.Client.
/// Provides distributed tracing and metrics for all API operations.
/// </summary>
public static class MercadoBitcoinTelemetry
{
    /// <summary>
    /// The name of the activity source for tracing.
    /// </summary>
    public const string ActivitySourceName = "MercadoBitcoin.Client";

    /// <summary>
    /// The name of the meter for metrics.
    /// </summary>
    public const string MeterName = "MercadoBitcoin.Client";

    /// <summary>
    /// Current library version.
    /// </summary>
    public const string Version = "6.1.0";

    /// <summary>
    /// Activity source for distributed tracing.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);

    /// <summary>
    /// Meter for metrics collection.
    /// </summary>
    public static readonly Meter Meter = new(MeterName, Version);

    // Counters
    private static readonly Counter<long> _requestCounter = Meter.CreateCounter<long>(
        "mb.client.requests.total",
        unit: "{requests}",
        description: "Total number of API requests made");

    private static readonly Counter<long> _requestSuccessCounter = Meter.CreateCounter<long>(
        "mb.client.requests.success",
        unit: "{requests}",
        description: "Total number of successful API requests");

    private static readonly Counter<long> _requestFailureCounter = Meter.CreateCounter<long>(
        "mb.client.requests.failure",
        unit: "{requests}",
        description: "Total number of failed API requests");

    private static readonly Counter<long> _retryCounter = Meter.CreateCounter<long>(
        "mb.client.retries.total",
        unit: "{retries}",
        description: "Total number of retry attempts");

    private static readonly Counter<long> _rateLimitCounter = Meter.CreateCounter<long>(
        "mb.client.rate_limit.hits",
        unit: "{hits}",
        description: "Number of times rate limit was hit");

    private static readonly Counter<long> _ordersPlacedCounter = Meter.CreateCounter<long>(
        "mb.client.orders.placed",
        unit: "{orders}",
        description: "Total number of orders placed");

    private static readonly Counter<long> _ordersCancelledCounter = Meter.CreateCounter<long>(
        "mb.client.orders.cancelled",
        unit: "{orders}",
        description: "Total number of orders cancelled");

    private static readonly Counter<long> _webSocketMessagesCounter = Meter.CreateCounter<long>(
        "mb.client.websocket.messages",
        unit: "{messages}",
        description: "Total WebSocket messages received");

    private static readonly Counter<long> _webSocketReconnectsCounter = Meter.CreateCounter<long>(
        "mb.client.websocket.reconnects",
        unit: "{reconnects}",
        description: "Total WebSocket reconnection attempts");

    private static readonly Counter<long> _cacheHitsCounter = Meter.CreateCounter<long>(
        "mb.client.cache.hits",
        unit: "{hits}",
        description: "Total cache hits");

    private static readonly Counter<long> _cacheMissesCounter = Meter.CreateCounter<long>(
        "mb.client.cache.misses",
        unit: "{misses}",
        description: "Total cache misses");

    // Histograms
    private static readonly Histogram<double> _requestDurationHistogram = Meter.CreateHistogram<double>(
        "mb.client.request.duration",
        unit: "ms",
        description: "Duration of API requests in milliseconds");

    private static readonly Histogram<double> _orderExecutionLatencyHistogram = Meter.CreateHistogram<double>(
        "mb.client.order.execution_latency",
        unit: "ms",
        description: "Latency of order execution in milliseconds");

    private static readonly Histogram<double> _webSocketLatencyHistogram = Meter.CreateHistogram<double>(
        "mb.client.websocket.latency",
        unit: "ms",
        description: "WebSocket message latency in milliseconds");

    // Gauges (using ObservableGauge)
    private static int _activeConnections;
    private static int _pendingOrders;
    private static int _activeWebSocketSubscriptions;

    static MercadoBitcoinTelemetry()
    {
        Meter.CreateObservableGauge(
            "mb.client.connections.active",
            () => Volatile.Read(ref _activeConnections),
            unit: "{connections}",
            description: "Number of active HTTP connections");

        Meter.CreateObservableGauge(
            "mb.client.orders.pending",
            () => Volatile.Read(ref _pendingOrders),
            unit: "{orders}",
            description: "Number of pending orders");

        Meter.CreateObservableGauge(
            "mb.client.websocket.subscriptions.active",
            () => Volatile.Read(ref _activeWebSocketSubscriptions),
            unit: "{subscriptions}",
            description: "Number of active WebSocket subscriptions");
    }

    #region Activity Methods

    /// <summary>
    /// Starts a new activity for an API operation.
    /// </summary>
    /// <param name="operationName">Name of the operation (e.g., "GetTickers", "PlaceOrder").</param>
    /// <param name="kind">The activity kind (default: Client).</param>
    /// <returns>The started activity, or null if no listener is attached.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartActivity(
        string operationName,
        ActivityKind kind = ActivityKind.Client)
    {
        return ActivitySource.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Starts a new activity for a REST API call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartRestActivity(
        string endpoint,
        string method = "GET",
        string? symbol = null)
    {
        var activity = ActivitySource.StartActivity($"MB.REST.{endpoint}", ActivityKind.Client);

        if (activity is not null)
        {
            activity.SetTag("mb.endpoint", endpoint);
            activity.SetTag("http.method", method);
            activity.SetTag("mb.api_version", "v4");

            if (symbol is not null)
            {
                activity.SetTag("mb.symbol", symbol);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a WebSocket operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartWebSocketActivity(
        string channel,
        string? symbol = null)
    {
        var activity = ActivitySource.StartActivity($"MB.WS.{channel}", ActivityKind.Consumer);

        if (activity is not null)
        {
            activity.SetTag("mb.ws.channel", channel);
            activity.SetTag("messaging.system", "websocket");

            if (symbol is not null)
            {
                activity.SetTag("mb.symbol", symbol);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a trading operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartTradingActivity(
        string operation,
        string symbol,
        string? side = null,
        string? orderType = null)
    {
        var activity = ActivitySource.StartActivity($"MB.Trading.{operation}", ActivityKind.Client);

        if (activity is not null)
        {
            activity.SetTag("mb.trading.operation", operation);
            activity.SetTag("mb.symbol", symbol);

            if (side is not null)
            {
                activity.SetTag("mb.trading.side", side);
            }

            if (orderType is not null)
            {
                activity.SetTag("mb.trading.order_type", orderType);
            }
        }

        return activity;
    }

    /// <summary>
    /// Sets the result of an activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetActivityResult(Activity? activity, bool success, string? errorMessage = null)
    {
        if (activity is null) return;

        if (success)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, errorMessage ?? "Unknown error");
            if (errorMessage is not null)
            {
                activity.SetTag("error.message", errorMessage);
            }
        }
    }

    /// <summary>
    /// Records an exception on an activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordException(Activity? activity, Exception exception)
    {
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("error.type", exception.GetType().Name);
        activity.SetTag("error.message", exception.Message);

        // Add exception event
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        };
        activity.AddEvent(new ActivityEvent("exception", tags: tags));
    }

    #endregion

    #region Counter Methods

    /// <summary>
    /// Records a request.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordRequest(string endpoint, string method = "GET")
    {
        _requestCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method));
    }

    /// <summary>
    /// Records a successful request.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordRequestSuccess(string endpoint, int statusCode)
    {
        _requestSuccessCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status_code", statusCode));
    }

    /// <summary>
    /// Records a failed request.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordRequestFailure(string endpoint, int statusCode, string? errorType = null)
    {
        _requestFailureCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("error_type", errorType ?? "unknown"));
    }

    /// <summary>
    /// Records a retry attempt.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordRetry(string endpoint, int attempt, int statusCode)
    {
        _retryCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("attempt", attempt),
            new KeyValuePair<string, object?>("status_code", statusCode));
    }

    /// <summary>
    /// Records a rate limit hit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordRateLimitHit(string endpoint, bool clientSide = true)
    {
        _rateLimitCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("source", clientSide ? "client" : "server"));
    }

    /// <summary>
    /// Records an order placed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordOrderPlaced(string symbol, string side, string orderType)
    {
        _ordersPlacedCounter.Add(1,
            new KeyValuePair<string, object?>("symbol", symbol),
            new KeyValuePair<string, object?>("side", side),
            new KeyValuePair<string, object?>("order_type", orderType));
    }

    /// <summary>
    /// Records an order cancelled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordOrderCancelled(string symbol)
    {
        _ordersCancelledCounter.Add(1,
            new KeyValuePair<string, object?>("symbol", symbol));
    }

    /// <summary>
    /// Records a WebSocket message received.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordWebSocketMessage(string channel, string? symbol = null)
    {
        _webSocketMessagesCounter.Add(1,
            new KeyValuePair<string, object?>("channel", channel),
            new KeyValuePair<string, object?>("symbol", symbol ?? "unknown"));
    }

    /// <summary>
    /// Records a WebSocket reconnection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordWebSocketReconnect(int attempt, bool success)
    {
        _webSocketReconnectsCounter.Add(1,
            new KeyValuePair<string, object?>("attempt", attempt),
            new KeyValuePair<string, object?>("success", success));
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordCacheHit(string cacheKey)
    {
        _cacheHitsCounter.Add(1,
            new KeyValuePair<string, object?>("cache_key_prefix", GetCacheKeyPrefix(cacheKey)));
    }

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordCacheMiss(string cacheKey)
    {
        _cacheMissesCounter.Add(1,
            new KeyValuePair<string, object?>("cache_key_prefix", GetCacheKeyPrefix(cacheKey)));
    }

    #endregion

    #region Histogram Methods

    /// <summary>
    /// Records request duration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordRequestDuration(string endpoint, double durationMs, int statusCode)
    {
        _requestDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status_code", statusCode));
    }

    /// <summary>
    /// Records order execution latency.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordOrderExecutionLatency(string symbol, double latencyMs, string orderType)
    {
        _orderExecutionLatencyHistogram.Record(latencyMs,
            new KeyValuePair<string, object?>("symbol", symbol),
            new KeyValuePair<string, object?>("order_type", orderType));
    }

    /// <summary>
    /// Records WebSocket message latency.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordWebSocketLatency(string channel, double latencyMs)
    {
        _webSocketLatencyHistogram.Record(latencyMs,
            new KeyValuePair<string, object?>("channel", channel));
    }

    #endregion

    #region Gauge Methods

    /// <summary>
    /// Increments active connections count.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);

    /// <summary>
    /// Decrements active connections count.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DecrementActiveConnections() => Interlocked.Decrement(ref _activeConnections);

    /// <summary>
    /// Sets pending orders count.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPendingOrders(int count) => Volatile.Write(ref _pendingOrders, count);

    /// <summary>
    /// Increments active WebSocket subscriptions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IncrementWebSocketSubscriptions() => Interlocked.Increment(ref _activeWebSocketSubscriptions);

    /// <summary>
    /// Decrements active WebSocket subscriptions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DecrementWebSocketSubscriptions() => Interlocked.Decrement(ref _activeWebSocketSubscriptions);

    #endregion

    #region Helper Methods

    private static string GetCacheKeyPrefix(string cacheKey)
    {
        var colonIndex = cacheKey.IndexOf(':');
        return colonIndex > 0 ? cacheKey[..colonIndex] : cacheKey;
    }

    #endregion
}

/// <summary>
/// Extension methods for Activity to add common tags.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Adds symbol tag to the activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? WithSymbol(this Activity? activity, string symbol)
    {
        activity?.SetTag("mb.symbol", symbol);
        return activity;
    }

    /// <summary>
    /// Adds account ID tag to the activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? WithAccountId(this Activity? activity, string accountId)
    {
        // Only add first 8 chars for privacy
        activity?.SetTag("mb.account_id", accountId.Length > 8 ? accountId[..8] + "..." : accountId);
        return activity;
    }

    /// <summary>
    /// Adds order ID tag to the activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? WithOrderId(this Activity? activity, string orderId)
    {
        activity?.SetTag("mb.order_id", orderId);
        return activity;
    }

    /// <summary>
    /// Adds HTTP status code tag to the activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? WithStatusCode(this Activity? activity, int statusCode)
    {
        activity?.SetTag("http.status_code", statusCode);
        return activity;
    }

    /// <summary>
    /// Adds response size tag to the activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? WithResponseSize(this Activity? activity, long bytes)
    {
        activity?.SetTag("http.response.body.size", bytes);
        return activity;
    }

    /// <summary>
    /// Adds latency tag to the activity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? WithLatency(this Activity? activity, double milliseconds)
    {
        activity?.SetTag("mb.latency_ms", milliseconds);
        return activity;
    }

    /// <summary>
    /// Marks the activity as successful.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? MarkSuccess(this Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
        return activity;
    }

    /// <summary>
    /// Marks the activity as failed with an error message.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? MarkError(this Activity? activity, string message)
    {
        activity?.SetStatus(ActivityStatusCode.Error, message);
        return activity;
    }

    /// <summary>
    /// Marks the activity as failed with an exception.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? MarkError(this Activity? activity, Exception ex)
    {
        if (activity is not null)
        {
            MercadoBitcoinTelemetry.RecordException(activity, ex);
        }
        return activity;
    }
}
