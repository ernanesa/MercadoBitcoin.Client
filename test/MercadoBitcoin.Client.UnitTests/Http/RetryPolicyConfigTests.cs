using MercadoBitcoin.Client.Http;

namespace MercadoBitcoin.Client.UnitTests.Http;

[Trait("Category", "Unit")]
public class RetryPolicyConfigTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var config = new RetryPolicyConfig();

        // Assert
        config.MaxRetryAttempts.Should().Be(3);
        config.BaseDelaySeconds.Should().Be(1.0);
        config.BackoffMultiplier.Should().Be(2.0);
        config.MaxDelaySeconds.Should().Be(30.0);
        config.RetryOnTimeout.Should().BeTrue();
        config.RetryOnRateLimit.Should().BeTrue();
        config.RetryOnServerErrors.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 1.0, 2.0, 1.0)] // Primeira tentativa
    [InlineData(2, 1.0, 2.0, 2.0)] // Segunda tentativa
    [InlineData(3, 1.0, 2.0, 4.0)] // Terceira tentativa
    [InlineData(4, 1.0, 2.0, 8.0)] // Quarta tentativa
    public void CalculateDelay_ExponentialBackoff_ReturnsCorrectDelay(int retryAttempt, double baseDelay, double multiplier, double expectedSeconds)
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            BaseDelaySeconds = baseDelay,
            BackoffMultiplier = multiplier,
            MaxDelaySeconds = 60.0
        };

        // Act
        var delay = config.CalculateDelay(retryAttempt);

        // Assert
        delay.TotalSeconds.Should().Be(expectedSeconds);
    }

    [Fact]
    public void CalculateDelay_ExceedsMaxDelay_ReturnsMaxDelay()
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            BaseDelaySeconds = 10.0,
            BackoffMultiplier = 3.0,
            MaxDelaySeconds = 15.0
        };

        // Act
        var delay = config.CalculateDelay(3); // 10 * 3^2 = 90, mas max é 15

        // Assert
        delay.TotalSeconds.Should().Be(15.0);
    }

    [Theory]
    [InlineData(0.5, 1.5, 0.5)] // Primeira tentativa
    [InlineData(0.5, 1.5, 0.75)] // Segunda tentativa  
    [InlineData(0.5, 1.5, 1.125)] // Terceira tentativa
    public void CalculateDelay_CustomValues_ReturnsCorrectDelay(double baseDelay, double multiplier, double expectedSeconds)
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            BaseDelaySeconds = baseDelay,
            BackoffMultiplier = multiplier,
            MaxDelaySeconds = 30.0
        };
        var retryAttempt = expectedSeconds == 0.5 ? 1 : expectedSeconds == 0.75 ? 2 : 3;

        // Act
        var delay = config.CalculateDelay(retryAttempt);

        // Assert
        delay.TotalSeconds.Should().BeApproximately(expectedSeconds, 0.001);
    }

    [Fact]
    public void CalculateDelay_ZeroRetryAttempt_ReturnsBaseDelay()
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            BaseDelaySeconds = 2.0,
            BackoffMultiplier = 2.0
        };

        // Act
        var delay = config.CalculateDelay(0); // 2 * 2^(-1) = 1

        // Assert
        delay.TotalSeconds.Should().Be(1.0);
    }

    [Fact]
    public void CalculateDelay_NegativeRetryAttempt_ReturnsValidDelay()
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            BaseDelaySeconds = 1.0,
            BackoffMultiplier = 2.0
        };

        // Act
        var delay = config.CalculateDelay(-1); // 1 * 2^(-2) = 0.25

        // Assert
        delay.TotalSeconds.Should().Be(0.25);
    }

    [Theory]
    [InlineData(1, 0.1, 1.2, 5.0)]
    [InlineData(5, 0.5, 1.5, 10.0)]
    [InlineData(10, 2.0, 3.0, 60.0)]
    public void Properties_CanBeSetAndRetrieved(int maxRetries, double baseDelay, double multiplier, double maxDelay)
    {
        // Arrange & Act
        var config = new RetryPolicyConfig
        {
            MaxRetryAttempts = maxRetries,
            BaseDelaySeconds = baseDelay,
            BackoffMultiplier = multiplier,
            MaxDelaySeconds = maxDelay
        };

        // Assert
        config.MaxRetryAttempts.Should().Be(maxRetries);
        config.BaseDelaySeconds.Should().Be(baseDelay);
        config.BackoffMultiplier.Should().Be(multiplier);
        config.MaxDelaySeconds.Should().Be(maxDelay);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public void RetryFlags_CanBeSetAndRetrieved(bool retryOnTimeout, bool retryOnRateLimit, bool retryOnServerErrors)
    {
        // Arrange & Act
        var config = new RetryPolicyConfig
        {
            RetryOnTimeout = retryOnTimeout,
            RetryOnRateLimit = retryOnRateLimit,
            RetryOnServerErrors = retryOnServerErrors
        };

        // Assert
        config.RetryOnTimeout.Should().Be(retryOnTimeout);
        config.RetryOnRateLimit.Should().Be(retryOnRateLimit);
        config.RetryOnServerErrors.Should().Be(retryOnServerErrors);
    }

    [Fact]
    public void CalculateDelay_LargeRetryAttempt_DoesNotOverflow()
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            BaseDelaySeconds = 1.0,
            BackoffMultiplier = 2.0,
            MaxDelaySeconds = 300.0
        };

        // Act
        var delay = config.CalculateDelay(100); // Tentativa muito alta

        // Assert
        delay.TotalSeconds.Should().Be(300.0); // Deve ser limitado pelo MaxDelaySeconds
        delay.Should().NotBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(0.0)] // Zero delay
    [InlineData(-1.0)] // Delay negativo
    public void CalculateDelay_EdgeCaseDelays_HandlesGracefully(double baseDelay)
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            BaseDelaySeconds = baseDelay,
            BackoffMultiplier = 2.0,
            MaxDelaySeconds = 30.0
        };

        // Act
        var delay = config.CalculateDelay(1);

        // Assert
        delay.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }
}