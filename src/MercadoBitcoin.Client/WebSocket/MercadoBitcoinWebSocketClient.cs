using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using MercadoBitcoin.Client.WebSocket.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MercadoBitcoin.Client.WebSocket;

/// <summary>
/// High-performance WebSocket client for real-time market data streaming from Mercado Bitcoin.
/// Supports ticker, trades, and order book channels with automatic reconnection.
/// </summary>
public sealed class MercadoBitcoinWebSocketClient : IAsyncDisposable
{
    private readonly WebSocketClientOptions _options;
    private readonly ILogger<MercadoBitcoinWebSocketClient>? _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly HashSet<string> _activeSubscriptions = new();
    private readonly object _subscriptionLock = new();

    private ClientWebSocket? _webSocket;
    private Task? _receiveTask;
    private int _reconnectAttempts;
    private WebSocketConnectionState _connectionState = WebSocketConnectionState.Disconnected;

    // Channels for message distribution
    private Channel<TickerMessage>? _tickerChannel;
    private Channel<TradeMessage>? _tradeChannel;
    private Channel<OrderBookMessage>? _orderBookChannel;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public WebSocketConnectionState ConnectionState => _connectionState;

    /// <summary>
    /// Event raised when the connection state changes.
    /// </summary>
    public event EventHandler<WebSocketConnectionState>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Creates a new WebSocket client with default options.
    /// </summary>
    public MercadoBitcoinWebSocketClient()
        : this(new WebSocketClientOptions(), null)
    {
    }

    /// <summary>
    /// Creates a new WebSocket client with the specified options.
    /// </summary>
    public MercadoBitcoinWebSocketClient(WebSocketClientOptions options, ILogger<MercadoBitcoinWebSocketClient>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// Creates a new WebSocket client using IOptions pattern for DI.
    /// </summary>
    public MercadoBitcoinWebSocketClient(IOptions<WebSocketClientOptions> options, ILogger<MercadoBitcoinWebSocketClient>? logger = null)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)), logger)
    {
    }

    /// <summary>
    /// Connects to the WebSocket server.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connectionState == WebSocketConnectionState.Connected)
            {
                _logger?.LogDebug("Already connected to WebSocket server");
                return;
            }

            await ConnectInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        SetConnectionState(WebSocketConnectionState.Connecting);

        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        // Configure WebSocket options
        _webSocket.Options.KeepAliveInterval = _options.KeepAliveInterval;
        _webSocket.Options.SetBuffer(_options.ReceiveBufferSize, _options.SendBufferSize);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
            timeoutCts.CancelAfter(_options.ConnectionTimeout);

            _logger?.LogInformation("Connecting to WebSocket server at {Url}", _options.WebSocketUrl);

            await _webSocket.ConnectAsync(new Uri(_options.WebSocketUrl), timeoutCts.Token).ConfigureAwait(false);

            SetConnectionState(WebSocketConnectionState.Connected);
            _reconnectAttempts = 0;

            _logger?.LogInformation("Successfully connected to WebSocket server");

            // Start receive loop
            _receiveTask = ReceiveLoopAsync(_disposeCts.Token);

            // Resubscribe to active channels
            await ResubscribeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to connect to WebSocket server");
            SetConnectionState(WebSocketConnectionState.Failed);
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the WebSocket server.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                _logger?.LogInformation("Disconnecting from WebSocket server");

                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client requested disconnect",
                    cancellationToken).ConfigureAwait(false);
            }

            SetConnectionState(WebSocketConnectionState.Closed);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Subscribes to ticker updates for the specified instrument and streams messages.
    /// </summary>
    /// <param name="instrument">Trading pair (e.g., "BTC-BRL").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of ticker messages.</returns>
    public async IAsyncEnumerable<TickerMessage> SubscribeTickerAsync(
        string instrument,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        _tickerChannel ??= Channel.CreateUnbounded<TickerMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });

        await SubscribeToChannelAsync(WebSocketChannel.Ticker, instrument, cancellationToken).ConfigureAwait(false);

        await foreach (var message in _tickerChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(message.Instrument, instrument, StringComparison.OrdinalIgnoreCase))
            {
                yield return message;
            }
        }
    }

    /// <summary>
    /// Subscribes to trade updates for the specified instrument and streams messages.
    /// </summary>
    /// <param name="instrument">Trading pair (e.g., "BTC-BRL").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of trade messages.</returns>
    public async IAsyncEnumerable<TradeMessage> SubscribeTradesAsync(
        string instrument,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        _tradeChannel ??= Channel.CreateUnbounded<TradeMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });

        await SubscribeToChannelAsync(WebSocketChannel.Trades, instrument, cancellationToken).ConfigureAwait(false);

        await foreach (var message in _tradeChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(message.Instrument, instrument, StringComparison.OrdinalIgnoreCase))
            {
                yield return message;
            }
        }
    }

    /// <summary>
    /// Subscribes to order book updates for the specified instrument and streams messages.
    /// </summary>
    /// <param name="instrument">Trading pair (e.g., "BTC-BRL").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of order book messages.</returns>
    public async IAsyncEnumerable<OrderBookMessage> SubscribeOrderBookAsync(
        string instrument,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        _orderBookChannel ??= Channel.CreateUnbounded<OrderBookMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });

        await SubscribeToChannelAsync(WebSocketChannel.OrderBook, instrument, cancellationToken).ConfigureAwait(false);

        await foreach (var message in _orderBookChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(message.Instrument, instrument, StringComparison.OrdinalIgnoreCase))
            {
                yield return message;
            }
        }
    }

    /// <summary>
    /// Unsubscribes from a specific channel and instrument.
    /// </summary>
    public async Task UnsubscribeAsync(string channel, string instrument, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(instrument);

        var subscriptionKey = $"{channel}:{instrument}";

        lock (_subscriptionLock)
        {
            _activeSubscriptions.Remove(subscriptionKey);
        }

        if (_webSocket?.State == WebSocketState.Open)
        {
            var request = new SubscriptionRequest
            {
                Type = "unsubscribe",
                Subscription = new SubscriptionDetails
                {
                    Channel = channel,
                    Instrument = instrument
                }
            };

            await SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("Unsubscribed from {Channel} for {Instrument}", channel, instrument);
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connectionState != WebSocketConnectionState.Connected)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SubscribeToChannelAsync(string channel, string instrument, CancellationToken cancellationToken)
    {
        var subscriptionKey = $"{channel}:{instrument}";

        lock (_subscriptionLock)
        {
            if (!_activeSubscriptions.Add(subscriptionKey))
            {
                // Already subscribed
                return;
            }
        }

        var request = new SubscriptionRequest
        {
            Type = "subscribe",
            Subscription = new SubscriptionDetails
            {
                Channel = channel,
                Instrument = instrument
            }
        };

        await SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("Subscribed to {Channel} for {Instrument}", channel, instrument);
    }

    private async Task SendMessageAsync(SubscriptionRequest message, CancellationToken cancellationToken)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        // Use pooled buffer for serialization - AOT compatible using source generators
        var buffer = ArrayPool<byte>.Shared.Rent(_options.SendBufferSize);
        try
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(message,
                MercadoBitcoinJsonSerializerContext.Default.SubscriptionRequest);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(json),
                WebSocketMessageType.Text,
                true,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);
        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger?.LogInformation("Server initiated close: {Status} - {Description}",
                            result.CloseStatus, result.CloseStatusDescription);

                        await HandleDisconnectionAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                    {
                        await ProcessMessageAsync(buffer.AsMemory(0, result.Count)).ConfigureAwait(false);
                    }
                }
                catch (WebSocketException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogWarning(ex, "WebSocket error in receive loop");
                    await HandleDisconnectionAsync(cancellationToken).ConfigureAwait(false);
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task ProcessMessageAsync(ReadOnlyMemory<byte> messageData)
    {
        try
        {
            // First, determine the message type by parsing just the "type" field
            using var document = JsonDocument.Parse(messageData);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
            {
                _logger?.LogWarning("Received message without type field");
                return;
            }

            var messageType = typeElement.GetString();

            switch (messageType)
            {
                case "ticker":
                    var ticker = JsonSerializer.Deserialize(messageData.Span,
                        MercadoBitcoinJsonSerializerContext.Default.TickerMessage);
                    if (ticker != null)
                    {
                        _tickerChannel?.Writer.TryWrite(ticker);
                    }
                    break;

                case "trades":
                case "trade":
                    var trade = JsonSerializer.Deserialize(messageData.Span,
                        MercadoBitcoinJsonSerializerContext.Default.TradeMessage);
                    if (trade != null)
                    {
                        _tradeChannel?.Writer.TryWrite(trade);
                    }
                    break;

                case "orderbook":
                    var orderBook = JsonSerializer.Deserialize(messageData.Span,
                        MercadoBitcoinJsonSerializerContext.Default.OrderBookMessage);
                    if (orderBook != null)
                    {
                        _orderBookChannel?.Writer.TryWrite(orderBook);
                    }
                    break;

                case "subscribed":
                case "unsubscribed":
                    _logger?.LogDebug("Subscription status: {Type}", messageType);
                    break;

                case "error":
                    if (root.TryGetProperty("message", out var errorMessage))
                    {
                        _logger?.LogError("Server error: {Message}", errorMessage.GetString());
                        ErrorOccurred?.Invoke(this, new InvalidOperationException(errorMessage.GetString()));
                    }
                    break;

                case "pong":
                    _logger?.LogTrace("Received pong");
                    break;

                default:
                    _logger?.LogDebug("Unknown message type: {Type}", messageType);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Failed to parse WebSocket message");
        }
    }

    private async Task HandleDisconnectionAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoReconnect || cancellationToken.IsCancellationRequested)
        {
            SetConnectionState(WebSocketConnectionState.Disconnected);
            return;
        }

        SetConnectionState(WebSocketConnectionState.Reconnecting);

        while (_reconnectAttempts < _options.MaxReconnectAttempts && !cancellationToken.IsCancellationRequested)
        {
            _reconnectAttempts++;
            var delay = CalculateBackoffDelay(_reconnectAttempts);

            _logger?.LogInformation("Attempting reconnection {Attempt}/{MaxAttempts} in {Delay}ms",
                _reconnectAttempts, _options.MaxReconnectAttempts, delay.TotalMilliseconds);

            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                await ConnectInternalAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "Reconnection attempt {Attempt} failed", _reconnectAttempts);
            }
        }

        _logger?.LogError("Max reconnection attempts reached. Giving up.");
        SetConnectionState(WebSocketConnectionState.Failed);
    }

    private async Task ResubscribeAsync(CancellationToken cancellationToken)
    {
        string[] subscriptions;
        lock (_subscriptionLock)
        {
            subscriptions = _activeSubscriptions.ToArray();
        }

        foreach (var subscription in subscriptions)
        {
            var parts = subscription.Split(':');
            if (parts.Length == 2)
            {
                var request = new SubscriptionRequest
                {
                    Type = "subscribe",
                    Subscription = new SubscriptionDetails
                    {
                        Channel = parts[0],
                        Instrument = parts[1]
                    }
                };

                await SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
                _logger?.LogDebug("Resubscribed to {Channel} for {Instrument}", parts[0], parts[1]);
            }
        }
    }

    private TimeSpan CalculateBackoffDelay(int attempt)
    {
        // Exponential backoff with jitter
        var baseDelay = _options.InitialReconnectDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var maxDelay = _options.MaxReconnectDelay.TotalMilliseconds;
        var actualDelay = Math.Min(baseDelay, maxDelay);

        // Add jitter (±25%)
        var jitter = actualDelay * 0.25 * (Random.Shared.NextDouble() * 2 - 1);
        return TimeSpan.FromMilliseconds(actualDelay + jitter);
    }

    private void SetConnectionState(WebSocketConnectionState newState)
    {
        if (_connectionState != newState)
        {
            var previousState = _connectionState;
            _connectionState = newState;
            _logger?.LogDebug("Connection state changed: {Previous} -> {New}", previousState, newState);
            ConnectionStateChanged?.Invoke(this, newState);
        }
    }

    /// <summary>
    /// Disposes the WebSocket client and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposeCts.IsCancellationRequested)
        {
            return;
        }

        _disposeCts.Cancel();

        // Complete all channels
        _tickerChannel?.Writer.TryComplete();
        _tradeChannel?.Writer.TryComplete();
        _orderBookChannel?.Writer.TryComplete();

        // Wait for receive task
        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        // Close WebSocket
        if (_webSocket?.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disposed",
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        _webSocket?.Dispose();
        _connectionLock.Dispose();
        _disposeCts.Dispose();

        SetConnectionState(WebSocketConnectionState.Closed);
    }
}
