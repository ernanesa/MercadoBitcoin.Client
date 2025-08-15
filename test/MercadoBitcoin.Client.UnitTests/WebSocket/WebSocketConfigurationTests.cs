using MercadoBitcoin.Client.UnitTests.Base;
using MercadoBitcoin.Client.WebSocket.Interfaces;
using System;

namespace MercadoBitcoin.Client.UnitTests.WebSocket;

[Trait("Category", "Unit")]
[Trait("Category", "WebSocket")]
public class WebSocketConfigurationTests : UnitTestBase
{
    [Fact]
    public void WebSocketConfiguration_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var config = new WebSocketConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.Url.Should().Be("wss://ws.mercadobitcoin.net/ws");
        config.UserAgent.Should().Be("MercadoBitcoin.Client/1.0");
        config.ConnectionTimeoutSeconds.Should().Be(30);
        config.PingIntervalSeconds.Should().Be(30);
        config.EnableAutoReconnect.Should().BeTrue();
        config.ReconnectIntervalSeconds.Should().Be(5);
        config.MaxReconnectAttempts.Should().Be(10);
        config.ReconnectBackoffMultiplier.Should().Be(1.5);
        config.Headers.Should().NotBeNull();
        config.Headers.Should().ContainKey("User-Agent");
        config.EnableVerboseLogging.Should().BeFalse();
        config.SendBufferSize.Should().Be(4096);
        config.ReceiveBufferSize.Should().Be(4096);
    }

    [Fact]
    public void CreateProduction_ReturnsProductionConfiguration()
    {
        // Act
        var config = WebSocketConfiguration.CreateProduction();

        // Assert
        config.Should().NotBeNull();
        config.Url.Should().Be("wss://ws.mercadobitcoin.net/ws");
        config.EnableAutoReconnect.Should().BeTrue();
        config.ReconnectIntervalSeconds.Should().Be(5);
        config.MaxReconnectAttempts.Should().Be(10);
        config.EnableVerboseLogging.Should().BeFalse();
    }

    [Fact]
    public void CreateDevelopment_ReturnsDevelopmentConfiguration()
    {
        // Act
        var config = WebSocketConfiguration.CreateDevelopment();

        // Assert
        config.Should().NotBeNull();
        config.Url.Should().Be("wss://ws.mercadobitcoin.net/ws");
        config.EnableAutoReconnect.Should().BeTrue();
        config.ReconnectIntervalSeconds.Should().Be(2);
        config.MaxReconnectAttempts.Should().Be(3);
        config.EnableVerboseLogging.Should().BeTrue();
    }

    [Fact]
    public void CreateTesting_ReturnsTestConfiguration()
    {
        // Act
        var config = WebSocketConfiguration.CreateTesting();

        // Assert
        config.Should().NotBeNull();
        config.Url.Should().Be("wss://ws.mercadobitcoin.net/ws");
        config.EnableAutoReconnect.Should().BeFalse();
        config.EnableVerboseLogging.Should().BeFalse();
        config.ConnectionTimeoutSeconds.Should().Be(10);
        config.PingIntervalSeconds.Should().Be(10);
    }

    [Fact]
    public void WebSocketConfiguration_CanSetCustomValues()
    {
        // Arrange
        var customUrl = "wss://custom.example.com/ws";
        var customTimeoutSeconds = 45;
        var customReconnectIntervalSeconds = 15;
        var customMaxAttempts = 10;
        var customBackoffMultiplier = 2.0;

        // Act
        var config = new WebSocketConfiguration
        {
            Url = customUrl,
            ConnectionTimeoutSeconds = customTimeoutSeconds,
            ReconnectIntervalSeconds = customReconnectIntervalSeconds,
            MaxReconnectAttempts = customMaxAttempts,
            ReconnectBackoffMultiplier = customBackoffMultiplier,
            EnableAutoReconnect = false,
            EnableVerboseLogging = true
        };

        // Assert
        config.Url.Should().Be(customUrl);
        config.ConnectionTimeoutSeconds.Should().Be(customTimeoutSeconds);
        config.ReconnectIntervalSeconds.Should().Be(customReconnectIntervalSeconds);
        config.MaxReconnectAttempts.Should().Be(customMaxAttempts);
        config.ReconnectBackoffMultiplier.Should().Be(customBackoffMultiplier);
        config.EnableAutoReconnect.Should().BeFalse();
        config.EnableVerboseLogging.Should().BeTrue();
    }

    [Fact]
    public void WebSocketConfiguration_Headers_CanBeModified()
    {
        // Arrange
        var config = new WebSocketConfiguration();
        var headerKey = "X-Custom-Header";
        var headerValue = "CustomValue";

        // Act
        config.Headers[headerKey] = headerValue;

        // Assert
        config.Headers.Should().ContainKey(headerKey);
        config.Headers[headerKey].Should().Be(headerValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WebSocketConfiguration_InvalidMaxReconnectAttempts_ShouldBeHandledGracefully(int invalidAttempts)
    {
        // Arrange & Act
        var config = new WebSocketConfiguration
        {
            MaxReconnectAttempts = invalidAttempts
        };

        // Assert
        // O valor é aceito, mas a lógica de reconexão deve tratar valores inválidos
        config.MaxReconnectAttempts.Should().Be(invalidAttempts);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    public void WebSocketConfiguration_ValidBackoffMultiplier_ShouldBeAccepted(double multiplier)
    {
        // Arrange & Act
        var config = new WebSocketConfiguration
        {
            ReconnectBackoffMultiplier = multiplier
        };

        // Assert
        config.ReconnectBackoffMultiplier.Should().Be(multiplier);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(16384)]
    public void WebSocketConfiguration_ValidBufferSizes_ShouldBeAccepted(int bufferSize)
    {
        // Arrange & Act
        var config = new WebSocketConfiguration
        {
            SendBufferSize = bufferSize,
            ReceiveBufferSize = bufferSize
        };

        // Assert
        config.SendBufferSize.Should().Be(bufferSize);
        config.ReceiveBufferSize.Should().Be(bufferSize);
    }
}