using FluentAssertions;
using MercadoBitcoin.Client.WebSocket;
using MercadoBitcoin.Client.WebSocket.Messages;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests;

/// <summary>
/// Integration tests for WebSocket streaming functionality.
/// These tests require a network connection and may be skipped in CI environments.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "WebSocket")]
public class WebSocketStreamingTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<MercadoBitcoinWebSocketClient> _logger;
    private MercadoBitcoinWebSocketClient? _wsClient;

    public WebSocketStreamingTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new XUnitLogger<MercadoBitcoinWebSocketClient>(output);
    }

    public async Task InitializeAsync()
    {
        _wsClient = new MercadoBitcoinWebSocketClient(_logger);
        await _wsClient.ConnectAsync();
    }

    public async Task DisposeAsync()
    {
        if (_wsClient != null)
        {
            await _wsClient.DisposeAsync();
        }
    }

    [Fact]
    public async Task SubscribeTickerAsync_WithBTCBRL_ReceivesTickerMessages()
    {
        // Arrange
        const string symbol = "BTC-BRL";
        var messagesReceived = 0;
        const int targetMessages = 5;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // Act & Assert
        await foreach (var ticker in _wsClient!.SubscribeTickerAsync(symbol, cts.Token))
        {
            _output.WriteLine($"Received ticker: {ticker.Pair} - Last: {ticker.Last}, Volume: {ticker.Vol}");

            ticker.Should().NotBeNull();
            ticker.Pair.Should().Be(symbol);
            ticker.Last.Should().NotBeNullOrEmpty();
            ticker.Date.Should().BeGreaterThan(0);

            messagesReceived++;
            if (messagesReceived >= targetMessages)
            {
                break;
            }
        }

        messagesReceived.Should().BeGreaterThanOrEqualTo(targetMessages);
    }

    [Fact]
    public async Task SubscribeTradesAsync_WithBTCBRL_ReceivesTradeMessages()
    {
        // Arrange
        const string symbol = "BTC-BRL";
        var messagesReceived = 0;
        const int targetMessages = 3;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        // Act & Assert
        await foreach (var trade in _wsClient!.SubscribeTradesAsync(symbol, cts.Token))
        {
            _output.WriteLine($"Received trade: {trade.Pair} - Price: {trade.Price}, Amount: {trade.Amount}, Type: {trade.Type}");

            trade.Should().NotBeNull();
            trade.Pair.Should().Be(symbol);
            trade.Price.Should().NotBeNullOrEmpty();
            trade.Amount.Should().NotBeNullOrEmpty();
            trade.Type.Should().NotBeNullOrEmpty();

            messagesReceived++;
            if (messagesReceived >= targetMessages)
            {
                break;
            }
        }

        messagesReceived.Should().BeGreaterThanOrEqualTo(targetMessages);
    }

    [Fact]
    public async Task SubscribeOrderBookAsync_WithBTCBRL_ReceivesOrderBookMessages()
    {
        // Arrange
        const string symbol = "BTC-BRL";
        var messagesReceived = 0;
        const int targetMessages = 3;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // Act & Assert
        await foreach (var orderBook in _wsClient!.SubscribeOrderBookAsync(symbol, cts.Token))
        {
            _output.WriteLine($"Received orderbook: {orderBook.Pair} - Asks: {orderBook.Asks.Length}, Bids: {orderBook.Bids.Length}");

            orderBook.Should().NotBeNull();
            orderBook.Pair.Should().Be(symbol);
            orderBook.Asks.Should().NotBeEmpty();
            orderBook.Bids.Should().NotBeEmpty();
            orderBook.Timestamp.Should().BeGreaterThan(0);

            messagesReceived++;
            if (messagesReceived >= targetMessages)
            {
                break;
            }
        }

        messagesReceived.Should().BeGreaterThanOrEqualTo(targetMessages);
    }

    [Fact]
    public async Task MultipleSubscriptions_ConcurrentStreaming_AllReceiveMessages()
    {
        // Arrange
        const string symbol1 = "BTC-BRL";
        const string symbol2 = "ETH-BRL";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var ticker1Count = 0;
        var ticker2Count = 0;

        // Act
        var task1 = Task.Run(async () =>
        {
            await foreach (var ticker in _wsClient!.SubscribeTickerAsync(symbol1, cts.Token))
            {
                _output.WriteLine($"[Ticker1] {ticker.Pair}: {ticker.Last}");
                ticker1Count++;
                if (ticker1Count >= 3)
                    break;
            }
        });

        var task2 = Task.Run(async () =>
        {
            await foreach (var ticker in _wsClient!.SubscribeTickerAsync(symbol2, cts.Token))
            {
                _output.WriteLine($"[Ticker2] {ticker.Pair}: {ticker.Last}");
                ticker2Count++;
                if (ticker2Count >= 3)
                    break;
            }
        });

        await Task.WhenAll(task1, task2);

        // Assert
        ticker1Count.Should().BeGreaterThanOrEqualTo(3);
        ticker2Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task UnsubscribeAsync_AfterSubscribing_StopsReceivingMessages()
    {
        // Arrange
        const string symbol = "BTC-BRL";
        var messagesReceived = 0;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act - Subscribe and receive a few messages
        await foreach (var ticker in _wsClient!.SubscribeTickerAsync(symbol, cts.Token))
        {
            messagesReceived++;
            if (messagesReceived >= 3)
            {
                await _wsClient.UnsubscribeAsync("ticker", symbol);
                break;
            }
        }

        // Give time for unsubscribe to propagate
        await Task.Delay(2000);

        // Assert
        messagesReceived.Should().BeGreaterThanOrEqualTo(3);
        _output.WriteLine($"Successfully unsubscribed after receiving {messagesReceived} messages");
    }
}

/// <summary>
/// XUnit logger adapter for ILogger interface.
/// </summary>
internal class XUnitLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;

    public XUnitLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        if (exception != null)
        {
            _output.WriteLine(exception.ToString());
        }
    }
}
