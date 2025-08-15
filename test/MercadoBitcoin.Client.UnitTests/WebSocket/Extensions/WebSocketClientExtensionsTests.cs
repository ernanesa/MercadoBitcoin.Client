using MercadoBitcoin.Client.UnitTests.Base;
using MercadoBitcoin.Client.WebSocket.Extensions;
using MercadoBitcoin.Client.WebSocket.Interfaces;
using MercadoBitcoin.Client.WebSocket.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.UnitTests.WebSocket.Extensions;

[Trait("Category", "Unit")]
[Trait("Category", "WebSocket")]
[Trait("Category", "Extensions")]
public class WebSocketClientExtensionsTests : UnitTestBase
{
    private readonly Mock<IWebSocketClient> _mockWebSocketClient;

    public WebSocketClientExtensionsTests()
    {
        _mockWebSocketClient = new Mock<IWebSocketClient>();
    }

    [Fact]
    public async Task SubscribeToTradesAsync_WithSymbol_CallsSubscribeAsync()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToTradesAsync(symbol, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(
                "trades",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SubscribeToOrderBookAsync_WithSymbol_CallsSubscribeAsync()
    {
        // Arrange
        var symbol = "ETH-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToOrderBookAsync(symbol, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(
                "orderbook",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SubscribeToTickerAsync_WithSymbol_CallsSubscribeAsync()
    {
        // Arrange
        var symbol = "LTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToTickerAsync(symbol, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(
                "ticker",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SubscribeToCandlesAsync_WithSymbolAndInterval_CallsSubscribeAsync()
    {
        // Arrange
        var symbol = "XRP-BRL";
        var interval = CandleIntervals.FiveMinutes;
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToCandlesAsync(symbol, interval, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(
                $"candles_{interval}",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UnsubscribeFromTradesAsync_WithSymbol_CallsUnsubscribeAsync()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.UnsubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.UnsubscribeFromTradesAsync(symbol, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.UnsubscribeAsync(
                "trades",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UnsubscribeFromOrderBookAsync_WithSymbol_CallsUnsubscribeAsync()
    {
        // Arrange
        var symbol = "ETH-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.UnsubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.UnsubscribeFromOrderBookAsync(symbol, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.UnsubscribeAsync(
                "orderbook",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UnsubscribeFromTickerAsync_WithSymbol_CallsUnsubscribeAsync()
    {
        // Arrange
        var symbol = "LTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.UnsubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.UnsubscribeFromTickerAsync(symbol, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.UnsubscribeAsync(
                "ticker",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UnsubscribeFromCandlesAsync_WithSymbolAndInterval_CallsUnsubscribeAsync()
    {
        // Arrange
        var symbol = "XRP-BRL";
        var interval = CandleIntervals.OneHour;
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.UnsubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.UnsubscribeFromCandlesAsync(symbol, interval, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.UnsubscribeAsync(
                $"candles_{interval}",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SubscribeToMultipleChannelsAsync_WithAllChannels_CallsMultipleSubscribeAsync()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToMultipleChannelsAsync(
            symbol,
            includeTrades: true,
            includeOrderBook: true,
            includeTicker: true,
            includeCandles: true,
            candleInterval: CandleIntervals.OneMinute,
            cancellationToken: cancellationToken
        );

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(It.IsAny<string>(), symbol, cancellationToken),
            Times.Exactly(4) // trades, orderbook, ticker, candles
        );
    }

    [Fact]
    public async Task SubscribeToMultipleChannelsAsync_WithSelectiveChannels_CallsCorrectNumberOfSubscribeAsync()
    {
        // Arrange
        var symbol = "ETH-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToMultipleChannelsAsync(
            symbol, 
            includeTrades: true,
            includeOrderBook: false,
            includeTicker: true,
            includeCandles: false,
            cancellationToken: cancellationToken
        );

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(It.IsAny<string>(), symbol, cancellationToken),
            Times.Exactly(2) // apenas trades e ticker
        );
    }

    [Fact]
    public async Task ConnectAndSubscribeAsync_ConnectsAndSubscribes()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.ConnectAndSubscribeAsync(
            symbol,
            includeTrades: true,
            includeOrderBook: true,
            includeTicker: false,
            includeCandles: false,
            cancellationToken: cancellationToken
        );

        // Assert
        _mockWebSocketClient.Verify(
            x => x.ConnectAsync(cancellationToken),
            Times.Once
        );
        
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(It.IsAny<string>(), symbol, cancellationToken),
            Times.Exactly(2) // trades e orderbook
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SubscribeToTradesAsync_WithInvalidSymbol_ThrowsArgumentException(string invalidSymbol)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _mockWebSocketClient.Object.SubscribeToTradesAsync(invalidSymbol, cancellationToken)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SubscribeToOrderBookAsync_WithInvalidSymbol_ThrowsArgumentException(string invalidSymbol)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _mockWebSocketClient.Object.SubscribeToOrderBookAsync(invalidSymbol, cancellationToken)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SubscribeToTickerAsync_WithInvalidSymbol_ThrowsArgumentException(string invalidSymbol)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _mockWebSocketClient.Object.SubscribeToTickerAsync(invalidSymbol, cancellationToken)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SubscribeToCandlesAsync_WithInvalidSymbol_ThrowsArgumentException(string invalidSymbol)
    {
        // Arrange
        var interval = CandleIntervals.OneMinute;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _mockWebSocketClient.Object.SubscribeToCandlesAsync(invalidSymbol, interval, cancellationToken)
        );
    }

    [Fact]
    public async Task SubscribeToCandlesAsync_WithNullInterval_ThrowsArgumentNullException()
    {
        // Arrange
        var symbol = "BTC-BRL";
        string nullInterval = null;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _mockWebSocketClient.Object.SubscribeToCandlesAsync(symbol, nullInterval, cancellationToken)
        );
    }

    [Theory]
    [InlineData(CandleIntervals.OneMinute)]
    [InlineData(CandleIntervals.FiveMinutes)]
    [InlineData(CandleIntervals.FifteenMinutes)]
    [InlineData(CandleIntervals.ThirtyMinutes)]
    [InlineData(CandleIntervals.OneHour)]
    [InlineData(CandleIntervals.FourHours)]

    [InlineData(CandleIntervals.OneDay)]
    public async Task SubscribeToCandlesAsync_WithValidIntervals_CallsSubscribeAsync(string interval)
    {
        // Arrange
        var symbol = "BTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToCandlesAsync(symbol, interval, cancellationToken);

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(
                $"candles_{interval}",
                symbol,
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SubscribeToMultipleChannelsAsync_WithNoChannelsSelected_DoesNotCallSubscribeAsync()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var cancellationToken = CancellationToken.None;
        
        _mockWebSocketClient
            .Setup(x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockWebSocketClient.Object.SubscribeToMultipleChannelsAsync(
            symbol, 
            includeTrades: false,
            includeOrderBook: false,
            includeTicker: false,
            includeCandles: false,
            cancellationToken: cancellationToken
        );

        // Assert
        _mockWebSocketClient.Verify(
            x => x.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken),
            Times.Never
        );
    }

    public override void Dispose()
    {
        _mockWebSocketClient?.Reset();
        base.Dispose();
    }
}