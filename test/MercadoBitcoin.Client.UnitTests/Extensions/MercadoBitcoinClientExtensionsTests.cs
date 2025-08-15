using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Http;

namespace MercadoBitcoin.Client.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class MercadoBitcoinClientExtensionsTests
{
    public MercadoBitcoinClientExtensionsTests()
    {
    }

    [Fact]
    public void CreateWithRetryPolicies_WithoutLogger_ReturnsValidClient()
    {
        // Act
        var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<MercadoBitcoinClient>();
        
        client.Dispose();
    }



    [Fact]
    public void CreateTradingRetryConfig_ReturnsOptimizedConfig()
    {
        // Act
        var config = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();

        // Assert
        config.Should().NotBeNull();
        config.MaxRetryAttempts.Should().Be(5);
        config.BaseDelaySeconds.Should().Be(0.5);
        config.BackoffMultiplier.Should().Be(1.5);
        config.MaxDelaySeconds.Should().Be(10.0);
        config.RetryOnTimeout.Should().BeTrue();
        config.RetryOnRateLimit.Should().BeTrue();
        config.RetryOnServerErrors.Should().BeTrue();
    }

    [Fact]
    public void CreatePublicDataRetryConfig_ReturnsConservativeConfig()
    {
        // Act
        var config = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();

        // Assert
        config.Should().NotBeNull();
        config.MaxRetryAttempts.Should().Be(2);
        config.BaseDelaySeconds.Should().Be(2.0);
        config.BackoffMultiplier.Should().Be(2.0);
        config.MaxDelaySeconds.Should().Be(30.0);
        config.RetryOnTimeout.Should().BeTrue();
        config.RetryOnRateLimit.Should().BeFalse(); // Dados públicos não têm rate limit
        config.RetryOnServerErrors.Should().BeTrue();
    }

    [Fact]
    public void CreateTradingRetryConfig_DelayCalculation_IsOptimized()
    {
        // Arrange
        var config = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();

        // Act & Assert
        var delay1 = config.CalculateDelay(1);
        var delay2 = config.CalculateDelay(2);
        var delay3 = config.CalculateDelay(3);

        // Verifica que os delays são menores e crescem mais suavemente
        delay1.TotalSeconds.Should().Be(0.5); // 0.5 * 1.5^0
        delay2.TotalSeconds.Should().Be(0.75); // 0.5 * 1.5^1
        delay3.TotalSeconds.Should().BeApproximately(1.125, 0.001); // 0.5 * 1.5^2
    }

    [Fact]
    public void CreatePublicDataRetryConfig_DelayCalculation_IsConservative()
    {
        // Arrange
        var config = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();

        // Act & Assert
        var delay1 = config.CalculateDelay(1);
        var delay2 = config.CalculateDelay(2);

        // Verifica que os delays são maiores
        delay1.TotalSeconds.Should().Be(2.0); // 2.0 * 2^0
        delay2.TotalSeconds.Should().Be(4.0); // 2.0 * 2^1
    }

    [Fact]
    public void CreateTradingRetryConfig_MaxDelay_IsLimited()
    {
        // Arrange
        var config = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();

        // Act
        var largeDelay = config.CalculateDelay(10); // Tentativa muito alta

        // Assert
        largeDelay.TotalSeconds.Should().Be(10.0); // Limitado pelo MaxDelaySeconds
    }

    [Fact]
    public void CreatePublicDataRetryConfig_MaxDelay_IsLimited()
    {
        // Arrange
        var config = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();

        // Act
        var largeDelay = config.CalculateDelay(10); // Tentativa muito alta

        // Assert
        largeDelay.TotalSeconds.Should().Be(30.0); // Limitado pelo MaxDelaySeconds
    }

    [Fact]
    public void TradingConfig_Vs_PublicConfig_HasCorrectDifferences()
    {
        // Arrange
        var tradingConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();
        var publicConfig = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();

        // Assert - Trading é mais agressivo
        tradingConfig.MaxRetryAttempts.Should().BeGreaterThan(publicConfig.MaxRetryAttempts);
        tradingConfig.BaseDelaySeconds.Should().BeLessThan(publicConfig.BaseDelaySeconds);
        tradingConfig.BackoffMultiplier.Should().BeLessThan(publicConfig.BackoffMultiplier);
        tradingConfig.MaxDelaySeconds.Should().BeLessThan(publicConfig.MaxDelaySeconds);
        
        // Trading faz retry em rate limit, público não
        tradingConfig.RetryOnRateLimit.Should().BeTrue();
        publicConfig.RetryOnRateLimit.Should().BeFalse();
        
        // Ambos fazem retry em timeout e server errors
        tradingConfig.RetryOnTimeout.Should().Be(publicConfig.RetryOnTimeout);
        tradingConfig.RetryOnServerErrors.Should().Be(publicConfig.RetryOnServerErrors);
    }

    [Fact]
    public void CreateWithRetryPolicies_MultipleInstances_AreIndependent()
    {
        // Act
        var client1 = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
        var client2 = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

        // Assert
        client1.Should().NotBeSameAs(client2);
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        
        client1.Dispose();
        client2.Dispose();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void TradingConfig_DelayProgression_IsReasonable(int retryAttempt)
    {
        // Arrange
        var config = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();

        // Act
        var delay = config.CalculateDelay(retryAttempt);

        // Assert
        delay.Should().BeGreaterThan(TimeSpan.Zero);
        delay.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(config.MaxDelaySeconds));
        
        // Para trading, delays devem ser relativamente pequenos
        if (retryAttempt <= 3)
        {
            delay.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(2.0));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PublicDataConfig_DelayProgression_IsReasonable(int retryAttempt)
    {
        // Arrange
        var config = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();

        // Act
        var delay = config.CalculateDelay(retryAttempt);

        // Assert
        delay.Should().BeGreaterThan(TimeSpan.Zero);
        delay.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(config.MaxDelaySeconds));
        
        // Para dados públicos, delays podem ser maiores
        delay.Should().BeGreaterOrEqualTo(TimeSpan.FromSeconds(2.0));
    }
}