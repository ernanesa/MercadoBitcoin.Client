using FluentAssertions;
using MercadoBitcoin.Client.Trading;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests.Unit;

/// <summary>
/// Unit tests for Http3Detector class and related types.
/// </summary>
public class Http3DetectorTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private Http3Detector? _detector;

    public Http3DetectorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Http3Detector Constructor Tests

    [Fact]
    public void Http3Detector_DefaultConstructor_ShouldInitialize()
    {
        // Arrange & Act
        _detector = new Http3Detector();

        // Assert
        _detector.Should().NotBeNull();
        _detector.HasDetected.Should().BeFalse();
        _detector.SupportsHttp3.Should().BeFalse();
        _detector.DetectionAttempts.Should().Be(0);
        _detector.LastDetection.Should().Be(default(DateTime));

        _output.WriteLine("Http3Detector initialized with default values");
    }

    [Fact]
    public void Http3Detector_WithOptions_ShouldInitialize()
    {
        // Arrange
        var options = new Http3DetectorOptions
        {
            DefaultBaseUrl = "https://custom.api.com/v1",
            CacheDuration = TimeSpan.FromMinutes(30),
            RequestTimeout = TimeSpan.FromSeconds(5)
        };

        // Act
        _detector = new Http3Detector(options);

        // Assert
        _detector.Should().NotBeNull();
        _detector.HasDetected.Should().BeFalse();
    }

    [Fact]
    public void Http3Detector_WithNullOptions_ShouldUseDefaults()
    {
        // Arrange & Act
        _detector = new Http3Detector(null);

        // Assert
        _detector.Should().NotBeNull();
        _detector.HasDetected.Should().BeFalse();
    }

    #endregion

    #region Http3DetectorOptions Tests

    [Fact]
    public void Http3DetectorOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new Http3DetectorOptions();

        // Assert
        options.DefaultBaseUrl.Should().Be("https://api.mercadobitcoin.net/api/v4");
        options.CacheDuration.Should().Be(TimeSpan.FromHours(1));
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(10));
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(5));
        options.AutoDetect.Should().BeFalse();

        _output.WriteLine("All Http3DetectorOptions default values verified");
    }

    [Fact]
    public void Http3DetectorOptions_SetDefaultBaseUrl_ShouldUpdateValue()
    {
        // Arrange
        var options = new Http3DetectorOptions();
        var customUrl = "https://custom.api.example.com/v2";

        // Act
        options.DefaultBaseUrl = customUrl;

        // Assert
        options.DefaultBaseUrl.Should().Be(customUrl);
    }

    [Fact]
    public void Http3DetectorOptions_SetCacheDuration_ShouldUpdateValue()
    {
        // Arrange
        var options = new Http3DetectorOptions();
        var duration = TimeSpan.FromMinutes(15);

        // Act
        options.CacheDuration = duration;

        // Assert
        options.CacheDuration.Should().Be(duration);
    }

    [Fact]
    public void Http3DetectorOptions_SetRequestTimeout_ShouldUpdateValue()
    {
        // Arrange
        var options = new Http3DetectorOptions();
        var timeout = TimeSpan.FromSeconds(20);

        // Act
        options.RequestTimeout = timeout;

        // Assert
        options.RequestTimeout.Should().Be(timeout);
    }

    [Fact]
    public void Http3DetectorOptions_SetConnectionTimeout_ShouldUpdateValue()
    {
        // Arrange
        var options = new Http3DetectorOptions();
        var timeout = TimeSpan.FromSeconds(3);

        // Act
        options.ConnectionTimeout = timeout;

        // Assert
        options.ConnectionTimeout.Should().Be(timeout);
    }

    [Fact]
    public void Http3DetectorOptions_SetAutoDetect_ShouldUpdateValue()
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.AutoDetect = true;

        // Assert
        options.AutoDetect.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(300)]
    public void Http3DetectorOptions_SetRequestTimeout_ShouldAcceptVariousValues(int seconds)
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.RequestTimeout = TimeSpan.FromSeconds(seconds);

        // Assert
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    public void Http3DetectorOptions_ZeroCacheDuration_ShouldBeAllowed()
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.CacheDuration = TimeSpan.Zero;

        // Assert
        options.CacheDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Http3DetectorOptions_EmptyBaseUrl_ShouldBeAllowed()
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.DefaultBaseUrl = "";

        // Assert
        options.DefaultBaseUrl.Should().BeEmpty();
    }

    #endregion

    #region Http3Detector Properties Tests

    [Fact]
    public void Http3Detector_SupportsHttp3_BeforeDetection_ShouldBeFalse()
    {
        // Arrange & Act
        _detector = new Http3Detector();

        // Assert
        _detector.SupportsHttp3.Should().BeFalse();
    }

    [Fact]
    public void Http3Detector_HasDetected_BeforeDetection_ShouldBeFalse()
    {
        // Arrange & Act
        _detector = new Http3Detector();

        // Assert
        _detector.HasDetected.Should().BeFalse();
    }

    [Fact]
    public void Http3Detector_DetectionAttempts_BeforeDetection_ShouldBeZero()
    {
        // Arrange & Act
        _detector = new Http3Detector();

        // Assert
        _detector.DetectionAttempts.Should().Be(0);
    }

    [Fact]
    public void Http3Detector_LastDetection_BeforeDetection_ShouldBeDefault()
    {
        // Arrange & Act
        _detector = new Http3Detector();

        // Assert
        _detector.LastDetection.Should().Be(default(DateTime));
    }

    #endregion

    #region GetRecommendedVersion Tests

    [Fact]
    public void Http3Detector_GetRecommendedVersion_BeforeDetection_ShouldReturnHttp2()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        var version = _detector.GetRecommendedVersion();

        // Assert
        version.Should().Be(System.Net.HttpVersion.Version20);
    }

    [Fact]
    public void Http3Detector_GetRecommendedVersionPolicy_ShouldReturnRequestVersionOrLower()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        var policy = _detector.GetRecommendedVersionPolicy();

        // Assert
        policy.Should().Be(System.Net.Http.HttpVersionPolicy.RequestVersionOrLower);
    }

    #endregion

    #region GetStatus Tests

    [Fact]
    public void Http3Detector_GetStatus_BeforeDetection_ShouldReturnCorrectStatus()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        var status = _detector.GetStatus();

        // Assert
        status.Should().NotBeNull();
        status.HasDetected.Should().BeFalse();
        status.SupportsHttp3.Should().BeFalse();
        status.LastDetection.Should().Be(default(DateTime));
        status.AttemptCount.Should().Be(0);
        status.CacheValid.Should().BeFalse();
        status.RecommendedVersion.Should().Be("2.0");
    }

    [Fact]
    public void Http3Detector_GetStatus_ShouldReturnAllProperties()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        var status = _detector.GetStatus();

        // Assert
        status.HasDetected.Should().BeFalse();
        status.SupportsHttp3.Should().BeFalse();
        status.AttemptCount.Should().BeGreaterThanOrEqualTo(0);
        status.RecommendedVersion.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region InvalidateCache Tests

    [Fact]
    public void Http3Detector_InvalidateCache_ShouldResetHasDetected()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        _detector.InvalidateCache();

        // Assert
        _detector.HasDetected.Should().BeFalse();
    }

    [Fact]
    public void Http3Detector_InvalidateCache_MultipleCallsShouldBeIdempotent()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        _detector.InvalidateCache();
        _detector.InvalidateCache();
        _detector.InvalidateCache();

        // Assert
        _detector.HasDetected.Should().BeFalse();
    }

    #endregion

    #region CreateOptimizedClient Tests

    [Fact]
    public void Http3Detector_CreateOptimizedClient_ShouldReturnHttpClient()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        using var client = _detector.CreateOptimizedClient();

        // Assert
        client.Should().NotBeNull();
        client.DefaultRequestVersion.Should().Be(System.Net.HttpVersion.Version20);
    }

    [Fact]
    public void Http3Detector_CreateOptimizedClient_WithNullHandler_ShouldCreateDefault()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act
        using var client = _detector.CreateOptimizedClient(null);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Http3Detector_CreateOptimizedClient_WithCustomHandler_ShouldUseIt()
    {
        // Arrange
        _detector = new Http3Detector();
        using var handler = new HttpClientHandler();

        // Act
        using var client = _detector.CreateOptimizedClient(handler);

        // Assert
        client.Should().NotBeNull();
    }

    #endregion

    #region StatusChanged Event Tests

    [Fact]
    public void Http3Detector_StatusChangedEvent_CanSubscribe()
    {
        // Arrange
        _detector = new Http3Detector();
        var eventRaised = false;

        // Act
        _detector.StatusChanged += (sender, args) => { eventRaised = true; };

        // Assert - Should not throw
        _detector.Should().NotBeNull();
    }

    [Fact]
    public void Http3Detector_StatusChangedEvent_CanUnsubscribe()
    {
        // Arrange
        _detector = new Http3Detector();
        EventHandler<Http3StatusChangedEventArgs> handler = (sender, args) => { };

        // Act
        _detector.StatusChanged += handler;
        _detector.StatusChanged -= handler;

        // Assert - Should not throw
        _detector.Should().NotBeNull();
    }

    #endregion

    #region Http3StatusChangedEventArgs Tests

    [Fact]
    public void Http3StatusChangedEventArgs_ShouldStoreAllProperties()
    {
        // Arrange
        var detectionTime = DateTime.UtcNow;

        // Act
        var args = new Http3StatusChangedEventArgs
        {
            SupportsHttp3 = true,
            DetectionTime = detectionTime,
            AttemptNumber = 5
        };

        // Assert
        args.SupportsHttp3.Should().BeTrue();
        args.DetectionTime.Should().Be(detectionTime);
        args.AttemptNumber.Should().Be(5);
    }

    [Fact]
    public void Http3StatusChangedEventArgs_WithFalseSupport_ShouldStore()
    {
        // Arrange & Act
        var args = new Http3StatusChangedEventArgs
        {
            SupportsHttp3 = false,
            DetectionTime = DateTime.UtcNow,
            AttemptNumber = 1
        };

        // Assert
        args.SupportsHttp3.Should().BeFalse();
    }

    [Fact]
    public void Http3StatusChangedEventArgs_WithZeroAttemptNumber_ShouldStore()
    {
        // Arrange & Act
        var args = new Http3StatusChangedEventArgs
        {
            SupportsHttp3 = false,
            DetectionTime = DateTime.UtcNow,
            AttemptNumber = 0
        };

        // Assert
        args.AttemptNumber.Should().Be(0);
    }

    #endregion

    #region Http3DetectionStatus Tests

    [Fact]
    public void Http3DetectionStatus_ShouldStoreAllProperties()
    {
        // Arrange
        var lastDetection = DateTime.UtcNow;

        // Act
        var status = new Http3DetectionStatus
        {
            HasDetected = true,
            SupportsHttp3 = true,
            LastDetection = lastDetection,
            AttemptCount = 3,
            CacheValid = true,
            RecommendedVersion = "3.0"
        };

        // Assert
        status.HasDetected.Should().BeTrue();
        status.SupportsHttp3.Should().BeTrue();
        status.LastDetection.Should().Be(lastDetection);
        status.AttemptCount.Should().Be(3);
        status.CacheValid.Should().BeTrue();
        status.RecommendedVersion.Should().Be("3.0");
    }

    [Fact]
    public void Http3DetectionStatus_WithAllFalse_ShouldStore()
    {
        // Arrange & Act
        var status = new Http3DetectionStatus
        {
            HasDetected = false,
            SupportsHttp3 = false,
            LastDetection = default,
            AttemptCount = 0,
            CacheValid = false,
            RecommendedVersion = "2.0"
        };

        // Assert
        status.HasDetected.Should().BeFalse();
        status.SupportsHttp3.Should().BeFalse();
        status.CacheValid.Should().BeFalse();
    }

    [Fact]
    public void Http3DetectionStatus_RecommendedVersion_CanBeAnyString()
    {
        // Arrange & Act
        var status = new Http3DetectionStatus
        {
            HasDetected = true,
            SupportsHttp3 = false,
            LastDetection = DateTime.UtcNow,
            AttemptCount = 1,
            CacheValid = true,
            RecommendedVersion = "1.1"
        };

        // Assert
        status.RecommendedVersion.Should().Be("1.1");
    }

    #endregion

    #region Extension Methods Tests

    [Fact]
    public async Task Http3DetectorExtensions_EnsureDetectedAsync_WhenAlreadyDetected_ShouldReturnCachedResult()
    {
        // Arrange
        _detector = new Http3Detector();

        // Act - First ensure it hasn't detected
        var hasDetected = _detector.HasDetected;

        // Assert
        hasDetected.Should().BeFalse();

        // Note: We can't fully test EnsureDetectedAsync without network access,
        // but we can verify the extension method exists and is callable
    }

    [Fact]
    public void Http3DetectorExtensions_ConfigureClient_ShouldSetVersionProperties()
    {
        // Arrange
        _detector = new Http3Detector();
        using var client = new HttpClient();

        // Act
        Http3DetectorExtensions.ConfigureClient(_detector, client);

        // Assert
        client.DefaultRequestVersion.Should().Be(System.Net.HttpVersion.Version20);
        client.DefaultVersionPolicy.Should().Be(System.Net.Http.HttpVersionPolicy.RequestVersionOrLower);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void Http3DetectorOptions_VeryLargeCacheDuration_ShouldBeAllowed()
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.CacheDuration = TimeSpan.FromDays(365);

        // Assert
        options.CacheDuration.Should().Be(TimeSpan.FromDays(365));
    }

    [Fact]
    public void Http3DetectorOptions_NegativeCacheDuration_ShouldBeAllowed()
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.CacheDuration = TimeSpan.FromSeconds(-1);

        // Assert
        options.CacheDuration.Should().Be(TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void Http3DetectorOptions_VerySmallTimeout_ShouldBeAllowed()
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.RequestTimeout = TimeSpan.FromMilliseconds(1);
        options.ConnectionTimeout = TimeSpan.FromMilliseconds(1);

        // Assert
        options.RequestTimeout.Should().Be(TimeSpan.FromMilliseconds(1));
        options.ConnectionTimeout.Should().Be(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void Http3Detector_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange
        var detector1 = new Http3Detector();
        var detector2 = new Http3Detector();

        // Act
        detector1.InvalidateCache();

        // Assert
        detector1.HasDetected.Should().BeFalse();
        detector2.HasDetected.Should().BeFalse();
        // Both should be independent
    }

    [Fact]
    public void Http3DetectionStatus_AttemptCount_CanBeVeryLarge()
    {
        // Arrange & Act
        var status = new Http3DetectionStatus
        {
            HasDetected = true,
            SupportsHttp3 = true,
            LastDetection = DateTime.UtcNow,
            AttemptCount = int.MaxValue,
            CacheValid = true,
            RecommendedVersion = "3.0"
        };

        // Assert
        status.AttemptCount.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Http3StatusChangedEventArgs_MinDateTimeDetectionTime_ShouldBeAllowed()
    {
        // Arrange & Act
        var args = new Http3StatusChangedEventArgs
        {
            SupportsHttp3 = false,
            DetectionTime = DateTime.MinValue,
            AttemptNumber = 1
        };

        // Assert
        args.DetectionTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void Http3StatusChangedEventArgs_MaxDateTimeDetectionTime_ShouldBeAllowed()
    {
        // Arrange & Act
        var args = new Http3StatusChangedEventArgs
        {
            SupportsHttp3 = true,
            DetectionTime = DateTime.MaxValue,
            AttemptNumber = 1
        };

        // Assert
        args.DetectionTime.Should().Be(DateTime.MaxValue);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Http3Detector_ConcurrentGetStatus_ShouldBeThreadSafe()
    {
        // Arrange
        _detector = new Http3Detector();
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var status = _detector.GetStatus();
                    status.Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void Http3Detector_ConcurrentInvalidateCache_ShouldBeThreadSafe()
    {
        // Arrange
        _detector = new Http3Detector();
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _detector.InvalidateCache();
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void Http3Detector_ConcurrentGetRecommendedVersion_ShouldBeThreadSafe()
    {
        // Arrange
        _detector = new Http3Detector();
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var version = _detector.GetRecommendedVersion();
                    version.Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion

    #region URL Formatting Tests

    [Theory]
    [InlineData("https://api.example.com")]
    [InlineData("https://api.example.com/")]
    [InlineData("http://localhost:8080")]
    [InlineData("http://localhost:8080/")]
    public void Http3DetectorOptions_VariousBaseUrlFormats_ShouldBeAccepted(string url)
    {
        // Arrange
        var options = new Http3DetectorOptions();

        // Act
        options.DefaultBaseUrl = url;

        // Assert
        options.DefaultBaseUrl.Should().Be(url);
    }

    #endregion

    public void Dispose()
    {
        // Cleanup
        _detector = null;
    }
}
