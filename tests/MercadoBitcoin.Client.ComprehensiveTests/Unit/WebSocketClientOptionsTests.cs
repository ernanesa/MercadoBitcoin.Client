using FluentAssertions;
using MercadoBitcoin.Client.WebSocket;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests.Unit;

/// <summary>
/// Unit tests for WebSocketClientOptions and related WebSocket classes.
/// </summary>
public class WebSocketClientOptionsTests
{
    private readonly ITestOutputHelper _output;

    public WebSocketClientOptionsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region WebSocketClientOptions Default Values Tests

    [Fact]
    public void WebSocketClientOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new WebSocketClientOptions();

        // Assert
        options.Should().NotBeNull();
        options.WebSocketUrl.Should().Be(WebSocketClientOptions.DefaultWebSocketUrl);
        options.AutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(10);
        options.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(30));
        options.KeepAliveTimeout.Should().Be(TimeSpan.FromSeconds(10));
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(10));
        options.InitialReconnectDelay.Should().Be(TimeSpan.FromSeconds(1));
        options.MaxReconnectDelay.Should().Be(TimeSpan.FromSeconds(30));
        options.ReceiveBufferSize.Should().Be(8 * 1024);
        options.SendBufferSize.Should().Be(4 * 1024);

        _output.WriteLine("WebSocketClientOptions initialized successfully with default values");
    }

    [Fact]
    public void WebSocketClientOptions_DefaultWebSocketUrl_ShouldBeCorrect()
    {
        // Assert
        WebSocketClientOptions.DefaultWebSocketUrl.Should().Be("wss://ws.mercadobitcoin.net/ws");
    }

    #endregion

    #region WebSocketClientOptions Setter Tests

    [Fact]
    public void WebSocketClientOptions_SetWebSocketUrl_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var customUrl = "wss://custom.websocket.server/stream";

        // Act
        options.WebSocketUrl = customUrl;

        // Assert
        options.WebSocketUrl.Should().Be(customUrl);
    }

    [Fact]
    public void WebSocketClientOptions_SetEmptyWebSocketUrl_ShouldUseDefault()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.WebSocketUrl = "";

        // Assert
        options.WebSocketUrl.Should().Be(WebSocketClientOptions.DefaultWebSocketUrl);
    }

    [Fact]
    public void WebSocketClientOptions_SetWhitespaceWebSocketUrl_ShouldUseDefault()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.WebSocketUrl = "   ";

        // Assert
        options.WebSocketUrl.Should().Be(WebSocketClientOptions.DefaultWebSocketUrl);
    }

    [Fact]
    public void WebSocketClientOptions_SetAutoReconnect_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.AutoReconnect = false;

        // Assert
        options.AutoReconnect.Should().BeFalse();
    }

    [Fact]
    public void WebSocketClientOptions_SetMaxReconnectAttempts_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.MaxReconnectAttempts = 20;

        // Assert
        options.MaxReconnectAttempts.Should().Be(20);
    }

    [Fact]
    public void WebSocketClientOptions_SetNegativeMaxReconnectAttempts_ShouldBeCoercedToZero()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.MaxReconnectAttempts = -5;

        // Assert
        options.MaxReconnectAttempts.Should().Be(0);
    }

    [Fact]
    public void WebSocketClientOptions_SetKeepAliveInterval_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var interval = TimeSpan.FromSeconds(60);

        // Act
        options.KeepAliveInterval = interval;

        // Assert
        options.KeepAliveInterval.Should().Be(interval);
    }

    [Fact]
    public void WebSocketClientOptions_SetKeepAliveInterval_BelowMinimum_ShouldBeCoerced()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.KeepAliveInterval = TimeSpan.FromSeconds(1);

        // Assert - Should be coerced to minimum of 5 seconds
        options.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void WebSocketClientOptions_SetKeepAliveTimeout_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var timeout = TimeSpan.FromSeconds(20);

        // Act
        options.KeepAliveTimeout = timeout;

        // Assert
        options.KeepAliveTimeout.Should().Be(timeout);
    }

    [Fact]
    public void WebSocketClientOptions_SetKeepAliveTimeout_BelowMinimum_ShouldBeCoerced()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.KeepAliveTimeout = TimeSpan.FromMilliseconds(100);

        // Assert - Should be coerced to minimum of 1 second
        options.KeepAliveTimeout.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void WebSocketClientOptions_SetConnectionTimeout_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        options.ConnectionTimeout = timeout;

        // Assert
        options.ConnectionTimeout.Should().Be(timeout);
    }

    [Fact]
    public void WebSocketClientOptions_SetConnectionTimeout_BelowMinimum_ShouldBeCoerced()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.ConnectionTimeout = TimeSpan.FromMilliseconds(100);

        // Assert - Should be coerced to minimum of 1 second
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void WebSocketClientOptions_SetInitialReconnectDelay_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var delay = TimeSpan.FromSeconds(5);

        // Act
        options.InitialReconnectDelay = delay;

        // Assert
        options.InitialReconnectDelay.Should().Be(delay);
    }

    [Fact]
    public void WebSocketClientOptions_SetNegativeInitialReconnectDelay_ShouldBeCoercedToZero()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.InitialReconnectDelay = TimeSpan.FromSeconds(-5);

        // Assert
        options.InitialReconnectDelay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void WebSocketClientOptions_SetMaxReconnectDelay_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var delay = TimeSpan.FromMinutes(1);

        // Act
        options.MaxReconnectDelay = delay;

        // Assert
        options.MaxReconnectDelay.Should().Be(delay);
    }

    [Fact]
    public void WebSocketClientOptions_SetReceiveBufferSize_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.ReceiveBufferSize = 16 * 1024;

        // Assert
        options.ReceiveBufferSize.Should().Be(16 * 1024);
    }

    [Fact]
    public void WebSocketClientOptions_SetReceiveBufferSize_BelowMinimum_ShouldBeCoerced()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.ReceiveBufferSize = 100;

        // Assert - Should be coerced to minimum of 1024
        options.ReceiveBufferSize.Should().Be(1024);
    }

    [Fact]
    public void WebSocketClientOptions_SetSendBufferSize_ShouldUpdateValue()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.SendBufferSize = 8 * 1024;

        // Assert
        options.SendBufferSize.Should().Be(8 * 1024);
    }

    [Fact]
    public void WebSocketClientOptions_SetSendBufferSize_BelowMinimum_ShouldBeCoerced()
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.SendBufferSize = 100;

        // Assert - Should be coerced to minimum of 512
        options.SendBufferSize.Should().Be(512);
    }

    #endregion

    #region WebSocketClientOptions Configuration Scenarios

    [Fact]
    public void WebSocketClientOptions_HighFrequencyTradingConfig_ShouldWork()
    {
        // Arrange & Act
        var options = new WebSocketClientOptions
        {
            AutoReconnect = true,
            MaxReconnectAttempts = 100,
            InitialReconnectDelay = TimeSpan.FromMilliseconds(100),
            ConnectionTimeout = TimeSpan.FromSeconds(5),
            KeepAliveInterval = TimeSpan.FromSeconds(10),
            ReceiveBufferSize = 32 * 1024,
            SendBufferSize = 16 * 1024
        };

        // Assert
        options.AutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(100);
        options.InitialReconnectDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(5));
        options.KeepAliveInterval.Should().Be(TimeSpan.FromSeconds(10));
        options.ReceiveBufferSize.Should().Be(32 * 1024);
        options.SendBufferSize.Should().Be(16 * 1024);

        _output.WriteLine("High-frequency trading configuration verified");
    }

    [Fact]
    public void WebSocketClientOptions_LowBandwidthConfig_ShouldWork()
    {
        // Arrange & Act
        var options = new WebSocketClientOptions
        {
            AutoReconnect = true,
            MaxReconnectAttempts = 5,
            InitialReconnectDelay = TimeSpan.FromSeconds(5),
            MaxReconnectDelay = TimeSpan.FromMinutes(2),
            ConnectionTimeout = TimeSpan.FromSeconds(30),
            KeepAliveInterval = TimeSpan.FromMinutes(1)
        };

        // Assert
        options.AutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(5);
        options.InitialReconnectDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxReconnectDelay.Should().Be(TimeSpan.FromMinutes(2));
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.KeepAliveInterval.Should().Be(TimeSpan.FromMinutes(1));

        _output.WriteLine("Low bandwidth configuration verified");
    }

    [Fact]
    public void WebSocketClientOptions_NoAutoReconnectConfig_ShouldWork()
    {
        // Arrange & Act
        var options = new WebSocketClientOptions
        {
            AutoReconnect = false,
            MaxReconnectAttempts = 0
        };

        // Assert
        options.AutoReconnect.Should().BeFalse();
        options.MaxReconnectAttempts.Should().Be(0);

        _output.WriteLine("No auto-reconnect configuration verified");
    }

    #endregion

    #region WebSocketConnectionState Enum Tests

    [Fact]
    public void WebSocketConnectionState_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.IsDefined(typeof(WebSocketConnectionState), WebSocketConnectionState.Disconnected).Should().BeTrue();
        Enum.IsDefined(typeof(WebSocketConnectionState), WebSocketConnectionState.Connecting).Should().BeTrue();
        Enum.IsDefined(typeof(WebSocketConnectionState), WebSocketConnectionState.Connected).Should().BeTrue();
        Enum.IsDefined(typeof(WebSocketConnectionState), WebSocketConnectionState.Reconnecting).Should().BeTrue();
        Enum.IsDefined(typeof(WebSocketConnectionState), WebSocketConnectionState.Closed).Should().BeTrue();
        Enum.IsDefined(typeof(WebSocketConnectionState), WebSocketConnectionState.Failed).Should().BeTrue();

        _output.WriteLine("All WebSocketConnectionState values verified");
    }

    [Fact]
    public void WebSocketConnectionState_ShouldHaveCorrectNumberOfValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<WebSocketConnectionState>();

        // Assert
        values.Length.Should().Be(6);
    }

    [Theory]
    [InlineData(WebSocketConnectionState.Disconnected)]
    [InlineData(WebSocketConnectionState.Connecting)]
    [InlineData(WebSocketConnectionState.Connected)]
    [InlineData(WebSocketConnectionState.Reconnecting)]
    [InlineData(WebSocketConnectionState.Closed)]
    [InlineData(WebSocketConnectionState.Failed)]
    public void WebSocketConnectionState_AllValues_ShouldBeConvertibleToString(WebSocketConnectionState state)
    {
        // Act
        var stateString = state.ToString();

        // Assert
        stateString.Should().NotBeNullOrEmpty();
        _output.WriteLine($"State: {stateString}");
    }

    #endregion

    #region WebSocketChannel Constants Tests

    [Fact]
    public void WebSocketChannel_Ticker_ShouldHaveCorrectValue()
    {
        // Assert
        WebSocketChannel.Ticker.Should().Be("ticker");
    }

    [Fact]
    public void WebSocketChannel_Trades_ShouldHaveCorrectValue()
    {
        // Assert
        WebSocketChannel.Trades.Should().Be("trade");
    }

    [Fact]
    public void WebSocketChannel_OrderBook_ShouldHaveCorrectValue()
    {
        // Assert
        WebSocketChannel.OrderBook.Should().Be("orderbook");
    }

    #endregion

    #region Multiple Instance Independence Tests

    [Fact]
    public void WebSocketClientOptions_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange
        var options1 = new WebSocketClientOptions { AutoReconnect = true };
        var options2 = new WebSocketClientOptions { AutoReconnect = false };

        // Act
        options1.MaxReconnectAttempts = 10;
        options2.MaxReconnectAttempts = 5;

        // Assert
        options1.AutoReconnect.Should().BeTrue();
        options2.AutoReconnect.Should().BeFalse();
        options1.MaxReconnectAttempts.Should().Be(10);
        options2.MaxReconnectAttempts.Should().Be(5);
    }

    [Fact]
    public void WebSocketClientOptions_ModifyingOne_ShouldNotAffectOther()
    {
        // Arrange
        var options1 = new WebSocketClientOptions();
        var options2 = new WebSocketClientOptions();
        var originalUrl = options2.WebSocketUrl;

        // Act
        options1.WebSocketUrl = "wss://modified.url/ws";

        // Assert
        options2.WebSocketUrl.Should().Be(originalUrl);
        options1.WebSocketUrl.Should().Be("wss://modified.url/ws");
    }

    #endregion

    #region URL Format Tests

    [Theory]
    [InlineData("wss://api.mercadobitcoin.net/ws/v2/stream")]
    [InlineData("wss://localhost:8080/ws")]
    [InlineData("ws://localhost:8080/ws")]
    [InlineData("wss://api.example.com/websocket")]
    public void WebSocketClientOptions_VariousUrlFormats_ShouldBeAccepted(string url)
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.WebSocketUrl = url;

        // Assert
        options.WebSocketUrl.Should().Be(url);
        _output.WriteLine($"URL accepted: {url}");
    }

    [Fact]
    public void WebSocketClientOptions_UrlWithQueryString_ShouldBeAccepted()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var urlWithQuery = "wss://api.example.com/ws?token=abc123&version=2";

        // Act
        options.WebSocketUrl = urlWithQuery;

        // Assert
        options.WebSocketUrl.Should().Be(urlWithQuery);
        options.WebSocketUrl.Should().Contain("token=abc123");
    }

    [Fact]
    public void WebSocketClientOptions_UrlWithPort_ShouldBeAccepted()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var urlWithPort = "wss://api.example.com:9443/ws";

        // Act
        options.WebSocketUrl = urlWithPort;

        // Assert
        options.WebSocketUrl.Should().Be(urlWithPort);
        options.WebSocketUrl.Should().Contain(":9443");
    }

    #endregion

    #region TimeSpan Boundary Tests

    [Fact]
    public void WebSocketClientOptions_LargeTimeSpanValues_ShouldBeAccepted()
    {
        // Arrange
        var options = new WebSocketClientOptions();
        var largeValue = TimeSpan.FromHours(24);

        // Act
        options.KeepAliveInterval = largeValue;
        options.ConnectionTimeout = largeValue;
        options.MaxReconnectDelay = largeValue;

        // Assert
        options.KeepAliveInterval.Should().Be(largeValue);
        options.ConnectionTimeout.Should().Be(largeValue);
        options.MaxReconnectDelay.Should().Be(largeValue);
    }

    #endregion

    #region Integer Boundary Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void WebSocketClientOptions_MaxReconnectAttempts_VariousValues_ShouldBeAccepted(int value)
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.MaxReconnectAttempts = value;

        // Assert
        options.MaxReconnectAttempts.Should().Be(value);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(65536)]
    public void WebSocketClientOptions_ReceiveBufferSize_VariousValues_ShouldBeAccepted(int value)
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.ReceiveBufferSize = value;

        // Assert
        options.ReceiveBufferSize.Should().Be(value);
    }

    [Theory]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(4096)]
    [InlineData(32768)]
    public void WebSocketClientOptions_SendBufferSize_VariousValues_ShouldBeAccepted(int value)
    {
        // Arrange
        var options = new WebSocketClientOptions();

        // Act
        options.SendBufferSize = value;

        // Assert
        options.SendBufferSize.Should().Be(value);
    }

    #endregion
}
