using FluentAssertions;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Http;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests.Unit;

/// <summary>
/// Unit tests for MercadoBitcoinClientOptions and related configuration classes.
/// </summary>
public class ConfigurationTests
{
    private readonly ITestOutputHelper _output;

    public ConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region MercadoBitcoinClientOptions Tests

    [Fact]
    public void MercadoBitcoinClientOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new MercadoBitcoinClientOptions();

        // Assert
        options.RequestsPerSecond.Should().Be(5);
        options.BaseUrl.Should().Be("https://api.mercadobitcoin.net/api/v4");
        options.TimeoutSeconds.Should().Be(30);
        options.MaxRetryAttempts.Should().Be(3);
        options.BaseDelaySeconds.Should().Be(1);
        options.BackoffMultiplier.Should().Be(2);
        options.MaxDelaySeconds.Should().Be(30);
        options.RetryOnTimeout.Should().BeTrue();
        options.RetryOnRateLimit.Should().BeTrue();
        options.RetryOnServerErrors.Should().BeTrue();
        options.ApiLogin.Should().BeNull();
        options.ApiPassword.Should().BeNull();
        options.JsonSerializerContext.Should().BeNull();
        options.ConfigureJsonOptions.Should().BeNull();

        _output.WriteLine("All default values verified successfully");
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetRequestsPerSecond_ShouldUpdateValue()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.RequestsPerSecond = 10;

        // Assert
        options.RequestsPerSecond.Should().Be(10);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetBaseUrl_ShouldUpdateValue()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var customUrl = "https://custom.api.com/v1";

        // Act
        options.BaseUrl = customUrl;

        // Assert
        options.BaseUrl.Should().Be(customUrl);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetTimeoutSeconds_ShouldUpdateHttpConfiguration()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.TimeoutSeconds = 60;

        // Assert
        options.TimeoutSeconds.Should().Be(60);
        options.HttpConfiguration.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetHttpVersion_ShouldUpdateHttpConfiguration()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var version = System.Net.HttpVersion.Version11;

        // Act
        options.HttpVersion = version;

        // Assert
        options.HttpVersion.Should().Be(version);
        options.HttpConfiguration.HttpVersion.Should().Be(version);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetVersionPolicy_ShouldUpdateHttpConfiguration()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var policy = HttpVersionPolicy.RequestVersionExact;

        // Act
        options.VersionPolicy = policy;

        // Assert
        options.VersionPolicy.Should().Be(policy);
        options.HttpConfiguration.VersionPolicy.Should().Be(policy);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetMaxRetryAttempts_ShouldUpdateRetryPolicyConfig()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.MaxRetryAttempts = 5;

        // Assert
        options.MaxRetryAttempts.Should().Be(5);
        options.RetryPolicyConfig.MaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetBaseDelaySeconds_ShouldUpdateRetryPolicyConfig()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.BaseDelaySeconds = 2.5;

        // Assert
        options.BaseDelaySeconds.Should().Be(2.5);
        options.RetryPolicyConfig.BaseDelaySeconds.Should().Be(2.5);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetBackoffMultiplier_ShouldUpdateRetryPolicyConfig()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.BackoffMultiplier = 3.0;

        // Assert
        options.BackoffMultiplier.Should().Be(3.0);
        options.RetryPolicyConfig.BackoffMultiplier.Should().Be(3.0);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetMaxDelaySeconds_ShouldUpdateRetryPolicyConfig()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.MaxDelaySeconds = 60;

        // Assert
        options.MaxDelaySeconds.Should().Be(60);
        options.RetryPolicyConfig.MaxDelaySeconds.Should().Be(60);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetRetryOnTimeout_ShouldUpdateRetryPolicyConfig()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.RetryOnTimeout = false;

        // Assert
        options.RetryOnTimeout.Should().BeFalse();
        options.RetryPolicyConfig.RetryOnTimeout.Should().BeFalse();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetRetryOnRateLimit_ShouldUpdateRetryPolicyConfig()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.RetryOnRateLimit = false;

        // Assert
        options.RetryOnRateLimit.Should().BeFalse();
        options.RetryPolicyConfig.RetryOnRateLimit.Should().BeFalse();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetRetryOnServerErrors_ShouldUpdateRetryPolicyConfig()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.RetryOnServerErrors = false;

        // Assert
        options.RetryOnServerErrors.Should().BeFalse();
        options.RetryPolicyConfig.RetryOnServerErrors.Should().BeFalse();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_SetApiCredentials_ShouldStoreValues()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var apiLogin = "test-login";
        var apiPassword = "test-password";

        // Act
        options.ApiLogin = apiLogin;
        options.ApiPassword = apiPassword;

        // Assert
        options.ApiLogin.Should().Be(apiLogin);
        options.ApiPassword.Should().Be(apiPassword);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_HttpConfiguration_ShouldBeInitialized()
    {
        // Arrange & Act
        var options = new MercadoBitcoinClientOptions();

        // Assert
        options.HttpConfiguration.Should().NotBeNull();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_RetryPolicyConfig_ShouldBeInitialized()
    {
        // Arrange & Act
        var options = new MercadoBitcoinClientOptions();

        // Assert
        options.RetryPolicyConfig.Should().NotBeNull();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_RateLimiterConfig_ShouldBeInitialized()
    {
        // Arrange & Act
        var options = new MercadoBitcoinClientOptions();

        // Assert
        options.RateLimiterConfig.Should().NotBeNull();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_CacheConfig_ShouldBeInitialized()
    {
        // Arrange & Act
        var options = new MercadoBitcoinClientOptions();

        // Assert
        options.CacheConfig.Should().NotBeNull();
    }

    #endregion

    #region CacheConfig Tests

    [Fact]
    public void CacheConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new CacheConfig();

        // Assert
        config.EnableL1Cache.Should().BeTrue();
        config.DefaultL1Expiration.Should().Be(TimeSpan.FromSeconds(5));
        config.EnableRequestCoalescing.Should().BeTrue();
        config.EnableNegativeCaching.Should().BeTrue();
        config.NegativeCacheExpiration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void CacheConfig_SetEnableL1Cache_ShouldUpdateValue()
    {
        // Arrange
        var config = new CacheConfig();

        // Act
        config.EnableL1Cache = false;

        // Assert
        config.EnableL1Cache.Should().BeFalse();
    }

    [Fact]
    public void CacheConfig_SetDefaultL1Expiration_ShouldUpdateValue()
    {
        // Arrange
        var config = new CacheConfig();
        var expiration = TimeSpan.FromSeconds(30);

        // Act
        config.DefaultL1Expiration = expiration;

        // Assert
        config.DefaultL1Expiration.Should().Be(expiration);
    }

    [Fact]
    public void CacheConfig_SetEnableRequestCoalescing_ShouldUpdateValue()
    {
        // Arrange
        var config = new CacheConfig();

        // Act
        config.EnableRequestCoalescing = false;

        // Assert
        config.EnableRequestCoalescing.Should().BeFalse();
    }

    [Fact]
    public void CacheConfig_SetEnableNegativeCaching_ShouldUpdateValue()
    {
        // Arrange
        var config = new CacheConfig();

        // Act
        config.EnableNegativeCaching = false;

        // Assert
        config.EnableNegativeCaching.Should().BeFalse();
    }

    [Fact]
    public void CacheConfig_SetNegativeCacheExpiration_ShouldUpdateValue()
    {
        // Arrange
        var config = new CacheConfig();
        var expiration = TimeSpan.FromMinutes(30);

        // Act
        config.NegativeCacheExpiration = expiration;

        // Assert
        config.NegativeCacheExpiration.Should().Be(expiration);
    }

    #endregion

    #region RateLimiterConfig Tests

    [Fact]
    public void RateLimiterConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new RateLimiterConfig();

        // Assert
        config.PermitLimit.Should().Be(100);
        config.QueueLimit.Should().Be(10);
        config.ReplenishmentPeriod.Should().Be(TimeSpan.FromSeconds(1));
        config.TokensPerPeriod.Should().Be(10);
        config.AutoReplenishment.Should().BeTrue();
    }

    [Fact]
    public void RateLimiterConfig_SetPermitLimit_ShouldUpdateValue()
    {
        // Arrange
        var config = new RateLimiterConfig();

        // Act
        config.PermitLimit = 200;

        // Assert
        config.PermitLimit.Should().Be(200);
    }

    [Fact]
    public void RateLimiterConfig_SetQueueLimit_ShouldUpdateValue()
    {
        // Arrange
        var config = new RateLimiterConfig();

        // Act
        config.QueueLimit = 20;

        // Assert
        config.QueueLimit.Should().Be(20);
    }

    [Fact]
    public void RateLimiterConfig_SetReplenishmentPeriod_ShouldUpdateValue()
    {
        // Arrange
        var config = new RateLimiterConfig();
        var period = TimeSpan.FromSeconds(2);

        // Act
        config.ReplenishmentPeriod = period;

        // Assert
        config.ReplenishmentPeriod.Should().Be(period);
    }

    [Fact]
    public void RateLimiterConfig_SetTokensPerPeriod_ShouldUpdateValue()
    {
        // Arrange
        var config = new RateLimiterConfig();

        // Act
        config.TokensPerPeriod = 20;

        // Assert
        config.TokensPerPeriod.Should().Be(20);
    }

    [Fact]
    public void RateLimiterConfig_SetAutoReplenishment_ShouldUpdateValue()
    {
        // Arrange
        var config = new RateLimiterConfig();

        // Act
        config.AutoReplenishment = false;

        // Assert
        config.AutoReplenishment.Should().BeFalse();
    }

    #endregion

    #region RetryPolicyConfig Tests

    [Fact]
    public void RetryPolicyConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new RetryPolicyConfig();

        // Assert
        config.MaxRetryAttempts.Should().Be(3);
        config.BaseDelaySeconds.Should().Be(1);
        config.BackoffMultiplier.Should().Be(2);
        config.MaxDelaySeconds.Should().Be(30);
        config.RetryOnTimeout.Should().BeTrue();
        config.RetryOnRateLimit.Should().BeTrue();
        config.RetryOnServerErrors.Should().BeTrue();
    }

    [Fact]
    public void RetryPolicyConfig_SetMaxRetryAttempts_ShouldUpdateValue()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.MaxRetryAttempts = 5;

        // Assert
        config.MaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public void RetryPolicyConfig_SetBaseDelaySeconds_ShouldUpdateValue()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.BaseDelaySeconds = 2.0;

        // Assert
        config.BaseDelaySeconds.Should().Be(2.0);
    }

    [Fact]
    public void RetryPolicyConfig_SetBackoffMultiplier_ShouldUpdateValue()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.BackoffMultiplier = 3.0;

        // Assert
        config.BackoffMultiplier.Should().Be(3.0);
    }

    [Fact]
    public void RetryPolicyConfig_SetMaxDelaySeconds_ShouldUpdateValue()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.MaxDelaySeconds = 60;

        // Assert
        config.MaxDelaySeconds.Should().Be(60);
    }

    [Fact]
    public void RetryPolicyConfig_SetRetryOnTimeout_ShouldUpdateValue()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.RetryOnTimeout = false;

        // Assert
        config.RetryOnTimeout.Should().BeFalse();
    }

    [Fact]
    public void RetryPolicyConfig_SetRetryOnRateLimit_ShouldUpdateValue()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.RetryOnRateLimit = false;

        // Assert
        config.RetryOnRateLimit.Should().BeFalse();
    }

    [Fact]
    public void RetryPolicyConfig_SetRetryOnServerErrors_ShouldUpdateValue()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.RetryOnServerErrors = false;

        // Assert
        config.RetryOnServerErrors.Should().BeFalse();
    }

    #endregion

    #region HttpConfiguration Tests

    [Fact]
    public void HttpConfiguration_CreateHttp2Default_ShouldReturnCorrectConfig()
    {
        // Arrange & Act
        var config = HttpConfiguration.CreateHttp2Default();

        // Assert
        config.HttpVersion.Should().Be(System.Net.HttpVersion.Version20);
        config.VersionPolicy.Should().Be(HttpVersionPolicy.RequestVersionOrLower);
    }

    [Fact]
    public void HttpConfiguration_DefaultTimeout_ShouldBe30Seconds()
    {
        // Arrange & Act
        var config = HttpConfiguration.CreateHttp2Default();

        // Assert
        config.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void HttpConfiguration_SetTimeoutSeconds_ShouldUpdateValue()
    {
        // Arrange
        var config = HttpConfiguration.CreateHttp2Default();

        // Act
        config.TimeoutSeconds = 60;

        // Assert
        config.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void HttpConfiguration_SetHttpVersion_ShouldUpdateValue()
    {
        // Arrange
        var config = HttpConfiguration.CreateHttp2Default();
        var version = System.Net.HttpVersion.Version11;

        // Act
        config.HttpVersion = version;

        // Assert
        config.HttpVersion.Should().Be(version);
    }

    [Fact]
    public void HttpConfiguration_SetVersionPolicy_ShouldUpdateValue()
    {
        // Arrange
        var config = HttpConfiguration.CreateHttp2Default();
        var policy = HttpVersionPolicy.RequestVersionExact;

        // Act
        config.VersionPolicy = policy;

        // Assert
        config.VersionPolicy.Should().Be(policy);
    }

    #endregion

    #region Integration Tests for Configuration Interaction

    [Fact]
    public void MercadoBitcoinClientOptions_ConfigureCacheForHighFrequency_ShouldWorkCorrectly()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act - Configure for high-frequency trading
        options.CacheConfig.EnableL1Cache = true;
        options.CacheConfig.DefaultL1Expiration = TimeSpan.FromMilliseconds(100);
        options.CacheConfig.EnableRequestCoalescing = true;
        options.CacheConfig.EnableNegativeCaching = false;

        // Assert
        options.CacheConfig.EnableL1Cache.Should().BeTrue();
        options.CacheConfig.DefaultL1Expiration.Should().Be(TimeSpan.FromMilliseconds(100));
        options.CacheConfig.EnableRequestCoalescing.Should().BeTrue();
        options.CacheConfig.EnableNegativeCaching.Should().BeFalse();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_ConfigureForLowLatency_ShouldWorkCorrectly()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act - Configure for low latency
        options.TimeoutSeconds = 5;
        options.MaxRetryAttempts = 1;
        options.BaseDelaySeconds = 0.1;
        options.RateLimiterConfig.PermitLimit = 500;
        options.RateLimiterConfig.TokensPerPeriod = 50;

        // Assert
        options.TimeoutSeconds.Should().Be(5);
        options.MaxRetryAttempts.Should().Be(1);
        options.BaseDelaySeconds.Should().Be(0.1);
        options.RateLimiterConfig.PermitLimit.Should().Be(500);
        options.RateLimiterConfig.TokensPerPeriod.Should().Be(50);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_ConfigureForReliability_ShouldWorkCorrectly()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act - Configure for reliability
        options.TimeoutSeconds = 120;
        options.MaxRetryAttempts = 10;
        options.BaseDelaySeconds = 2;
        options.BackoffMultiplier = 2.5;
        options.MaxDelaySeconds = 60;
        options.RetryOnTimeout = true;
        options.RetryOnRateLimit = true;
        options.RetryOnServerErrors = true;

        // Assert
        options.TimeoutSeconds.Should().Be(120);
        options.MaxRetryAttempts.Should().Be(10);
        options.BaseDelaySeconds.Should().Be(2);
        options.BackoffMultiplier.Should().Be(2.5);
        options.MaxDelaySeconds.Should().Be(60);
        options.RetryOnTimeout.Should().BeTrue();
        options.RetryOnRateLimit.Should().BeTrue();
        options.RetryOnServerErrors.Should().BeTrue();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_ReplaceHttpConfiguration_ShouldWork()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var newConfig = new HttpConfiguration
        {
            TimeoutSeconds = 45,
            HttpVersion = System.Net.HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        // Act
        options.HttpConfiguration = newConfig;

        // Assert
        options.HttpConfiguration.Should().BeSameAs(newConfig);
        options.TimeoutSeconds.Should().Be(45);
        options.HttpVersion.Should().Be(System.Net.HttpVersion.Version11);
        options.VersionPolicy.Should().Be(HttpVersionPolicy.RequestVersionExact);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_ReplaceRetryPolicyConfig_ShouldWork()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var newConfig = new RetryPolicyConfig
        {
            MaxRetryAttempts = 7,
            BaseDelaySeconds = 3,
            BackoffMultiplier = 3,
            MaxDelaySeconds = 90,
            RetryOnTimeout = false,
            RetryOnRateLimit = false,
            RetryOnServerErrors = false
        };

        // Act
        options.RetryPolicyConfig = newConfig;

        // Assert
        options.RetryPolicyConfig.Should().BeSameAs(newConfig);
        options.MaxRetryAttempts.Should().Be(7);
        options.BaseDelaySeconds.Should().Be(3);
        options.BackoffMultiplier.Should().Be(3);
        options.MaxDelaySeconds.Should().Be(90);
        options.RetryOnTimeout.Should().BeFalse();
        options.RetryOnRateLimit.Should().BeFalse();
        options.RetryOnServerErrors.Should().BeFalse();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_ReplaceCacheConfig_ShouldWork()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var newConfig = new CacheConfig
        {
            EnableL1Cache = false,
            DefaultL1Expiration = TimeSpan.FromMinutes(1),
            EnableRequestCoalescing = false,
            EnableNegativeCaching = false,
            NegativeCacheExpiration = TimeSpan.FromMinutes(30)
        };

        // Act
        options.CacheConfig = newConfig;

        // Assert
        options.CacheConfig.Should().BeSameAs(newConfig);
        options.CacheConfig.EnableL1Cache.Should().BeFalse();
        options.CacheConfig.DefaultL1Expiration.Should().Be(TimeSpan.FromMinutes(1));
        options.CacheConfig.EnableRequestCoalescing.Should().BeFalse();
        options.CacheConfig.EnableNegativeCaching.Should().BeFalse();
        options.CacheConfig.NegativeCacheExpiration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void MercadoBitcoinClientOptions_ReplaceRateLimiterConfig_ShouldWork()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var newConfig = new RateLimiterConfig
        {
            PermitLimit = 500,
            QueueLimit = 50,
            ReplenishmentPeriod = TimeSpan.FromSeconds(2),
            TokensPerPeriod = 100,
            AutoReplenishment = false
        };

        // Act
        options.RateLimiterConfig = newConfig;

        // Assert
        options.RateLimiterConfig.Should().BeSameAs(newConfig);
        options.RateLimiterConfig.PermitLimit.Should().Be(500);
        options.RateLimiterConfig.QueueLimit.Should().Be(50);
        options.RateLimiterConfig.ReplenishmentPeriod.Should().Be(TimeSpan.FromSeconds(2));
        options.RateLimiterConfig.TokensPerPeriod.Should().Be(100);
        options.RateLimiterConfig.AutoReplenishment.Should().BeFalse();
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void MercadoBitcoinClientOptions_SetRequestsPerSecond_ShouldAcceptVariousValues(int value)
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.RequestsPerSecond = value;

        // Assert
        options.RequestsPerSecond.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(300)]
    [InlineData(3600)]
    public void MercadoBitcoinClientOptions_SetTimeoutSeconds_ShouldAcceptVariousValues(int value)
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.TimeoutSeconds = value;

        // Assert
        options.TimeoutSeconds.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void MercadoBitcoinClientOptions_SetMaxRetryAttempts_ShouldAcceptVariousValues(int value)
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.MaxRetryAttempts = value;

        // Assert
        options.MaxRetryAttempts.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.1)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    public void MercadoBitcoinClientOptions_SetBaseDelaySeconds_ShouldAcceptVariousValues(double value)
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.BaseDelaySeconds = value;

        // Assert
        options.BaseDelaySeconds.Should().Be(value);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(10.0)]
    public void MercadoBitcoinClientOptions_SetBackoffMultiplier_ShouldAcceptVariousValues(double value)
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.BackoffMultiplier = value;

        // Assert
        options.BackoffMultiplier.Should().Be(value);
    }

    [Fact]
    public void MercadoBitcoinClientOptions_EmptyBaseUrl_ShouldAccept()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.BaseUrl = "";

        // Assert
        options.BaseUrl.Should().BeEmpty();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_NullBaseUrl_ShouldAccept()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.BaseUrl = null!;

        // Assert
        options.BaseUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("test-api-key")]
    [InlineData("very-long-api-key-that-is-used-for-authentication-purposes-1234567890")]
    public void MercadoBitcoinClientOptions_SetApiLogin_ShouldAcceptVariousValues(string value)
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.ApiLogin = value;

        // Assert
        options.ApiLogin.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("test-api-secret")]
    [InlineData("very-long-api-secret-that-is-used-for-authentication-purposes-1234567890")]
    public void MercadoBitcoinClientOptions_SetApiPassword_ShouldAcceptVariousValues(string value)
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.ApiPassword = value;

        // Assert
        options.ApiPassword.Should().Be(value);
    }

    [Fact]
    public void CacheConfig_ZeroExpiration_ShouldAccept()
    {
        // Arrange
        var config = new CacheConfig();

        // Act
        config.DefaultL1Expiration = TimeSpan.Zero;

        // Assert
        config.DefaultL1Expiration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CacheConfig_NegativeExpiration_ShouldAccept()
    {
        // Arrange
        var config = new CacheConfig();

        // Act
        config.DefaultL1Expiration = TimeSpan.FromSeconds(-1);

        // Assert
        config.DefaultL1Expiration.Should().Be(TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void RateLimiterConfig_ZeroPermitLimit_ShouldAccept()
    {
        // Arrange
        var config = new RateLimiterConfig();

        // Act
        config.PermitLimit = 0;

        // Assert
        config.PermitLimit.Should().Be(0);
    }

    [Fact]
    public void RateLimiterConfig_ZeroQueueLimit_ShouldAccept()
    {
        // Arrange
        var config = new RateLimiterConfig();

        // Act
        config.QueueLimit = 0;

        // Assert
        config.QueueLimit.Should().Be(0);
    }

    [Fact]
    public void RetryPolicyConfig_ZeroDelay_ShouldAccept()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        config.BaseDelaySeconds = 0;
        config.MaxDelaySeconds = 0;

        // Assert
        config.BaseDelaySeconds.Should().Be(0);
        config.MaxDelaySeconds.Should().Be(0);
    }

    #endregion

    #region ConfigureJsonOptions Tests

    [Fact]
    public void MercadoBitcoinClientOptions_ConfigureJsonOptions_ShouldBeCallable()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();
        var callbackInvoked = false;

        // Act
        options.ConfigureJsonOptions = (opts) =>
        {
            callbackInvoked = true;
            opts.WriteIndented = true;
        };

        // Invoke the callback manually (simulating what the client would do)
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();
        options.ConfigureJsonOptions?.Invoke(jsonOptions);

        // Assert
        callbackInvoked.Should().BeTrue();
        jsonOptions.WriteIndented.Should().BeTrue();
    }

    [Fact]
    public void MercadoBitcoinClientOptions_ConfigureJsonOptions_NullCallback_ShouldBeAllowed()
    {
        // Arrange
        var options = new MercadoBitcoinClientOptions();

        // Act
        options.ConfigureJsonOptions = null;

        // Assert
        options.ConfigureJsonOptions.Should().BeNull();
    }

    #endregion
}
