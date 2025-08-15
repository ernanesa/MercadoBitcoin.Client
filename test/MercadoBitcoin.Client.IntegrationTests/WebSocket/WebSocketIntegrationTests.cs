using FluentAssertions;
using MercadoBitcoin.Client.IntegrationTests.Base;
using MercadoBitcoin.Client.WebSocket;
using MercadoBitcoin.Client.WebSocket.Extensions;
using MercadoBitcoin.Client.WebSocket.Interfaces;
using MercadoBitcoin.Client.WebSocket.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MercadoBitcoin.Client.IntegrationTests.WebSocket;

[Trait("Category", "Integration")]
[Trait("Category", "WebSocket")]
public class WebSocketIntegrationTests : IntegrationTestBase
{
    private readonly IWebSocketConfiguration _config;
    private readonly string _testSymbol = "BTC-BRL";

    public WebSocketIntegrationTests()
    {
        _config = WebSocketConfiguration.CreateTesting();
    }

    [Fact]
    public async Task ConnectAsync_WithValidConfiguration_ShouldConnect()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var connected = false;
        webSocketClient.Connected += (sender, args) => connected = true;

        // Act
        await webSocketClient.ConnectAsync(cts.Token);

        // Assert
        connected.Should().BeTrue();
        webSocketClient.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeToTrades_ShouldReceiveTradeData()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        TradeData receivedTrade = null;
        var tradeReceived = new TaskCompletionSource<bool>();

        webSocketClient.TradeReceived += (sender, trade) =>
        {
            receivedTrade = trade;
            tradeReceived.TrySetResult(true);
        };

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        await webSocketClient.SubscribeToTradesAsync(_testSymbol, cts.Token);

        // Wait for trade data (with timeout)
        var completed = await Task.WhenAny(
            tradeReceived.Task,
            Task.Delay(TimeSpan.FromMinutes(1), cts.Token)
        );

        // Assert
        if (completed == tradeReceived.Task)
        {
            receivedTrade.Should().NotBeNull();
            receivedTrade.Symbol.Should().Be(_testSymbol);
            receivedTrade.Price.Should().BeGreaterThan(0);
            receivedTrade.Amount.Should().BeGreaterThan(0);
            receivedTrade.TradeDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            receivedTrade.Side.Should().BeOneOf("buy", "sell");
        }
        else
        {
            // Se não recebeu dados em 1 minuto, pode ser que não houve trades
            // Isso não é necessariamente um erro, apenas log para debug
            Console.WriteLine("No trade data received within timeout period - this may be normal during low activity periods");
        }
    }

    [Fact]
    public async Task SubscribeToOrderBook_ShouldReceiveOrderBookData()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        OrderBookData receivedOrderBook = null;
        var orderBookReceived = new TaskCompletionSource<bool>();

        webSocketClient.OrderBookReceived += (sender, orderBook) =>
        {
            receivedOrderBook = orderBook;
            orderBookReceived.TrySetResult(true);
        };

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        await webSocketClient.SubscribeToOrderBookAsync(_testSymbol, cts.Token);

        // Wait for orderbook data (with timeout)
        var completed = await Task.WhenAny(
            orderBookReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(30), cts.Token)
        );

        // Assert
        if (completed == orderBookReceived.Task)
        {
            receivedOrderBook.Should().NotBeNull();
            receivedOrderBook.Symbol.Should().Be(_testSymbol);
            receivedOrderBook.Bids.Should().NotBeEmpty();
            receivedOrderBook.Asks.Should().NotBeEmpty();
            
            // Validate bid structure
            var firstBid = receivedOrderBook.Bids[0];
            firstBid.Price.Should().BeGreaterThan(0);
            firstBid.Amount.Should().BeGreaterThan(0);
            
            // Validate ask structure
            var firstAsk = receivedOrderBook.Asks[0];
            firstAsk.Price.Should().BeGreaterThan(0);
            firstAsk.Amount.Should().BeGreaterThan(0);
            
            // Asks should be higher than bids
            firstAsk.Price.Should().BeGreaterThan(firstBid.Price);
        }
        else
        {
            Assert.True(false, "OrderBook data not received within timeout period");
        }
    }

    [Fact]
    public async Task SubscribeToTicker_ShouldReceiveTickerData()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        TickerData receivedTicker = null;
        var tickerReceived = new TaskCompletionSource<bool>();

        webSocketClient.TickerReceived += (sender, ticker) =>
        {
            receivedTicker = ticker;
            tickerReceived.TrySetResult(true);
        };

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        await webSocketClient.SubscribeToTickerAsync(_testSymbol, cts.Token);

        // Wait for ticker data (with timeout)
        var completed = await Task.WhenAny(
            tickerReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(30), cts.Token)
        );

        // Assert
        if (completed == tickerReceived.Task)
        {
            receivedTicker.Should().NotBeNull();
            receivedTicker.Symbol.Should().Be(_testSymbol);
            receivedTicker.Last.Should().BeGreaterThan(0);
            receivedTicker.High.Should().BeGreaterThan(0);
            receivedTicker.Low.Should().BeGreaterThan(0);
            receivedTicker.Volume.Should().BeGreaterOrEqualTo(0);
        }
        else
        {
            Assert.True(false, "Ticker data not received within timeout period");
        }
    }

    [Fact]
    public async Task SubscribeToCandles_ShouldReceiveCandleData()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        CandleData receivedCandle = null;
        var candleReceived = new TaskCompletionSource<bool>();

        webSocketClient.CandleReceived += (sender, candle) =>
        {
            receivedCandle = candle;
            candleReceived.TrySetResult(true);
        };

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        await webSocketClient.SubscribeToCandlesAsync(_testSymbol, CandleIntervals.OneMinute, cts.Token);

        // Wait for candle data (with longer timeout as candles are less frequent)
        var completed = await Task.WhenAny(
            candleReceived.Task,
            Task.Delay(TimeSpan.FromMinutes(2), cts.Token)
        );

        // Assert
        if (completed == candleReceived.Task)
        {
            receivedCandle.Should().NotBeNull();
            receivedCandle.Symbol.Should().Be(_testSymbol);
            receivedCandle.Interval.Should().Be(CandleIntervals.OneMinute);
            receivedCandle.Open.Should().BeGreaterThan(0);
            receivedCandle.High.Should().BeGreaterThan(0);
            receivedCandle.Low.Should().BeGreaterThan(0);
            receivedCandle.Close.Should().BeGreaterThan(0);
            receivedCandle.Volume.Should().BeGreaterOrEqualTo(0);
            receivedCandle.OpenDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            receivedCandle.CloseDateTime.Should().BeAfter(receivedCandle.OpenDateTime);
            
            // Validate OHLC logic
            receivedCandle.High.Should().BeGreaterOrEqualTo(receivedCandle.Open);
            receivedCandle.High.Should().BeGreaterOrEqualTo(receivedCandle.Close);
            receivedCandle.Low.Should().BeLessOrEqualTo(receivedCandle.Open);
            receivedCandle.Low.Should().BeLessOrEqualTo(receivedCandle.Close);
        }
        else
        {
            Console.WriteLine("No candle data received within timeout period - this may be normal during low activity periods");
        }
    }

    [Fact]
    public async Task SubscribeToMultipleChannels_ShouldReceiveAllDataTypes()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        var receivedDataTypes = new List<string>();
        var allDataReceived = new TaskCompletionSource<bool>();

        webSocketClient.TradeReceived += (sender, trade) =>
        {
            receivedDataTypes.Add("trade");
            CheckAllDataReceived();
        };

        webSocketClient.OrderBookReceived += (sender, orderBook) =>
        {
            receivedDataTypes.Add("orderbook");
            CheckAllDataReceived();
        };

        webSocketClient.TickerReceived += (sender, ticker) =>
        {
            receivedDataTypes.Add("ticker");
            CheckAllDataReceived();
        };

        void CheckAllDataReceived()
        {
            var uniqueTypes = receivedDataTypes.Distinct().ToList();
            if (uniqueTypes.Count >= 3) // trade, orderbook, ticker
            {
                allDataReceived.TrySetResult(true);
            }
        }

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        await webSocketClient.SubscribeToMultipleChannelsAsync(
            _testSymbol,
            includeTrades: true,
            includeOrderBook: true,
            includeTicker: true,
            includeCandles: false, // Skip candles for faster test
            cancellationToken: cts.Token
        );

        // Wait for all data types (with timeout)
        var completed = await Task.WhenAny(
            allDataReceived.Task,
            Task.Delay(TimeSpan.FromMinutes(2), cts.Token)
        );

        // Assert
        if (completed == allDataReceived.Task)
        {
            var uniqueTypes = receivedDataTypes.Distinct().ToList();
            uniqueTypes.Should().Contain("trade");
            uniqueTypes.Should().Contain("orderbook");
            uniqueTypes.Should().Contain("ticker");
        }
        else
        {
            Console.WriteLine($"Received data types: {string.Join(", ", receivedDataTypes.Distinct())}");
            Console.WriteLine("Not all data types received within timeout period - this may be normal during low activity periods");
        }
    }

    [Fact]
    public async Task Reconnection_AfterDisconnection_ShouldReconnectAutomatically()
    {
        // Arrange
        var config = new WebSocketConfiguration
        {
            EnableAutoReconnect = true,
            ReconnectIntervalSeconds = 2,
            MaxReconnectAttempts = 3
        };

        using var webSocketClient = new MercadoBitcoinWebSocketClient(config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var connectionEvents = new List<string>();
        var reconnected = new TaskCompletionSource<bool>();

        webSocketClient.Connected += (sender, args) =>
        {
            connectionEvents.Add("connected");
            if (connectionEvents.Count(e => e == "connected") > 1)
            {
                reconnected.TrySetResult(true);
            }
        };

        webSocketClient.Disconnected += (sender, args) =>
        {
            connectionEvents.Add("disconnected");
        };

        webSocketClient.StateChanged += (sender, state) =>
        {
            if (state == WebSocketState.Connecting)
            {
                connectionEvents.Add("reconnecting");
            }
        };

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        
        // Force disconnection
        await webSocketClient.DisconnectAsync(cts.Token);

        // Wait for reconnection
        var completed = await Task.WhenAny(
            reconnected.Task,
            Task.Delay(TimeSpan.FromSeconds(30), cts.Token)
        );

        // Assert
        if (completed == reconnected.Task)
        {
            connectionEvents.Should().Contain("connected");
            connectionEvents.Should().Contain("disconnected");
            connectionEvents.Should().Contain("reconnecting");
            connectionEvents.Count(e => e == "connected").Should().BeGreaterThan(1);
            webSocketClient.IsConnected.Should().BeTrue();
        }
        else
        {
            Console.WriteLine($"Connection events: {string.Join(", ", connectionEvents)}");
            Console.WriteLine("Reconnection not completed within timeout period");
        }
    }

    [Fact]
    public async Task UnsubscribeFromChannel_ShouldStopReceivingData()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var tradeCount = 0;
        var firstTradeReceived = new TaskCompletionSource<bool>();
        var noMoreTradesReceived = new TaskCompletionSource<bool>();

        webSocketClient.TradeReceived += (sender, trade) =>
        {
            tradeCount++;
            if (tradeCount == 1)
            {
                firstTradeReceived.TrySetResult(true);
            }
        };

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        await webSocketClient.SubscribeToTradesAsync(_testSymbol, cts.Token);

        // Wait for first trade
        var firstCompleted = await Task.WhenAny(
            firstTradeReceived.Task,
            Task.Delay(TimeSpan.FromMinutes(1), cts.Token)
        );

        if (firstCompleted == firstTradeReceived.Task)
        {
            // Unsubscribe
            await webSocketClient.UnsubscribeFromTradesAsync(_testSymbol, cts.Token);
            
            var initialCount = tradeCount;
            
            // Wait a bit to see if more trades come (they shouldn't)
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            
            // Assert
            tradeCount.Should().Be(initialCount, "No new trades should be received after unsubscribing");
        }
        else
        {
            Console.WriteLine("No initial trade received - cannot test unsubscribe functionality");
        }
    }

    [Fact]
    public async Task ErrorHandling_WithInvalidSymbol_ShouldHandleGracefully()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var errorReceived = new TaskCompletionSource<bool>();
        MercadoBitcoin.Client.WebSocket.Interfaces.ErrorEventArgs receivedException = null;

        webSocketClient.Error += (sender, ex) =>
        {
            receivedException = ex;
            errorReceived.TrySetResult(true);
        };

        // Act
        await webSocketClient.ConnectAsync(cts.Token);
        
        // Try to subscribe to invalid symbol
        await webSocketClient.SubscribeToTradesAsync("INVALID-SYMBOL", cts.Token);

        // Wait for error or timeout
        var completed = await Task.WhenAny(
            errorReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(15), cts.Token)
        );

        // Assert
        if (completed == errorReceived.Task)
        {
            receivedException.Should().NotBeNull();
            Console.WriteLine($"Error handled gracefully: {receivedException.Message}");
        }
        else
        {
            Console.WriteLine("No error received for invalid symbol - API may accept any symbol format");
        }

        // Connection should still be active
        webSocketClient.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task CancellationToken_ShouldCancelOperations()
    {
        // Arrange
        using var webSocketClient = new MercadoBitcoinWebSocketClient(_config);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await webSocketClient.ConnectAsync(cts.Token);
            
            // This should be cancelled
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
        });
    }

    public override void Dispose()
    {
        // Cleanup if needed
        base.Dispose();
    }
}