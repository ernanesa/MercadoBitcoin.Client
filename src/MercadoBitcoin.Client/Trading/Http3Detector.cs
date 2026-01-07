using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// Provides HTTP/3 (QUIC) support detection and auto-configuration.
/// HTTP/3 can provide lower latency for trading operations when supported.
/// </summary>
public sealed class Http3Detector
{
    private readonly ILogger<Http3Detector>? _logger;
    private readonly Http3DetectorOptions _options;
    private readonly SemaphoreSlim _detectionLock = new(1, 1);

    private bool _detected;
    private bool _supportsHttp3;
    private DateTime _lastDetection;
    private int _detectionAttempts;

    /// <summary>
    /// Event raised when HTTP/3 support status changes.
    /// </summary>
    public event EventHandler<Http3StatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Creates a new instance of Http3Detector.
    /// </summary>
    /// <param name="options">Detection options.</param>
    /// <param name="logger">Optional logger.</param>
    public Http3Detector(Http3DetectorOptions? options = null, ILogger<Http3Detector>? logger = null)
    {
        _options = options ?? new Http3DetectorOptions();
        _logger = logger;
    }

    /// <summary>
    /// Gets whether HTTP/3 is supported by the server.
    /// </summary>
    public bool SupportsHttp3 => _supportsHttp3 && _detected;

    /// <summary>
    /// Gets whether detection has been performed.
    /// </summary>
    public bool HasDetected => _detected;

    /// <summary>
    /// Gets the time of the last detection attempt.
    /// </summary>
    public DateTime LastDetection => _lastDetection;

    /// <summary>
    /// Gets the number of detection attempts made.
    /// </summary>
    public int DetectionAttempts => Volatile.Read(ref _detectionAttempts);

    /// <summary>
    /// Detects whether the server supports HTTP/3.
    /// </summary>
    /// <param name="baseUrl">The base URL to test (defaults to MercadoBitcoin API).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if HTTP/3 is supported.</returns>
    public async Task<bool> DetectAsync(string? baseUrl = null, CancellationToken cancellationToken = default)
    {
        // Check if we should skip (already detected and cache valid)
        if (_detected && DateTime.UtcNow - _lastDetection < _options.CacheDuration)
        {
            _logger?.LogDebug("Using cached HTTP/3 detection result: {SupportsHttp3}", _supportsHttp3);
            return _supportsHttp3;
        }

        var acquired = await _detectionLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        if (!acquired)
        {
            _logger?.LogWarning("HTTP/3 detection already in progress");
            return _supportsHttp3;
        }

        try
        {
            Interlocked.Increment(ref _detectionAttempts);
            _lastDetection = DateTime.UtcNow;

            baseUrl ??= _options.DefaultBaseUrl;
            var testUrl = $"{baseUrl.TrimEnd('/')}/symbols?symbols=BTC-BRL";

            _logger?.LogInformation("Detecting HTTP/3 support at {Url}", testUrl);

            var previousSupport = _supportsHttp3;
            _supportsHttp3 = await TestHttp3SupportAsync(testUrl, cancellationToken);
            _detected = true;

            _logger?.LogInformation("HTTP/3 detection complete. Supported: {SupportsHttp3}", _supportsHttp3);

            // Raise event if status changed
            if (previousSupport != _supportsHttp3)
            {
                OnStatusChanged(new Http3StatusChangedEventArgs
                {
                    SupportsHttp3 = _supportsHttp3,
                    DetectionTime = _lastDetection,
                    AttemptNumber = _detectionAttempts
                });
            }

            return _supportsHttp3;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "HTTP/3 detection failed");
            _detected = true;
            _supportsHttp3 = false;
            return false;
        }
        finally
        {
            _detectionLock.Release();
        }
    }

    /// <summary>
    /// Tests HTTP/3 support by attempting a request.
    /// </summary>
    private async Task<bool> TestHttp3SupportAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = 1,
                ConnectTimeout = _options.ConnectionTimeout
            };

            using var client = new HttpClient(handler)
            {
                Timeout = _options.RequestTimeout
            };

            // Try HTTP/3 first
            client.DefaultRequestVersion = HttpVersion.Version30;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            var sw = Stopwatch.StartNew();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            sw.Stop();

            var usedVersion = response.Version;
            var isHttp3 = usedVersion == HttpVersion.Version30;

            _logger?.LogDebug(
                "HTTP request completed. Version: {Version}, Status: {StatusCode}, Duration: {Duration}ms",
                usedVersion,
                response.StatusCode,
                sw.ElapsedMilliseconds);

            return isHttp3;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogDebug(ex, "HTTP/3 test request failed (might not be supported)");
            return false;
        }
    }

    /// <summary>
    /// Creates an HttpClient configured with optimal HTTP version based on detection.
    /// </summary>
    /// <param name="handler">The underlying handler to use.</param>
    /// <returns>Configured HttpClient.</returns>
    public HttpClient CreateOptimizedClient(HttpMessageHandler? handler = null)
    {
        handler ??= new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 50,
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests
        };

        var client = new HttpClient(handler);

        if (_supportsHttp3 && _detected)
        {
            client.DefaultRequestVersion = HttpVersion.Version30;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            _logger?.LogDebug("Created HttpClient with HTTP/3 preference");
        }
        else
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            _logger?.LogDebug("Created HttpClient with HTTP/2 preference");
        }

        return client;
    }

    /// <summary>
    /// Gets the recommended HTTP version based on detection results.
    /// </summary>
    /// <returns>Recommended HTTP version.</returns>
    public Version GetRecommendedVersion()
    {
        if (_supportsHttp3 && _detected)
        {
            return HttpVersion.Version30;
        }

        return HttpVersion.Version20;
    }

    /// <summary>
    /// Gets the recommended HTTP version policy.
    /// </summary>
    /// <returns>Recommended version policy.</returns>
    public HttpVersionPolicy GetRecommendedVersionPolicy()
    {
        // Always use RequestVersionOrLower for graceful fallback
        return HttpVersionPolicy.RequestVersionOrLower;
    }

    /// <summary>
    /// Forces a re-detection on the next call to DetectAsync.
    /// </summary>
    public void InvalidateCache()
    {
        _detected = false;
        _logger?.LogDebug("HTTP/3 detection cache invalidated");
    }

    /// <summary>
    /// Gets detection status information.
    /// </summary>
    /// <returns>Current detection status.</returns>
    public Http3DetectionStatus GetStatus()
    {
        return new Http3DetectionStatus
        {
            HasDetected = _detected,
            SupportsHttp3 = _supportsHttp3,
            LastDetection = _lastDetection,
            AttemptCount = _detectionAttempts,
            CacheValid = _detected && DateTime.UtcNow - _lastDetection < _options.CacheDuration,
            RecommendedVersion = GetRecommendedVersion().ToString()
        };
    }

    private void OnStatusChanged(Http3StatusChangedEventArgs args)
    {
        StatusChanged?.Invoke(this, args);
    }
}

/// <summary>
/// Configuration options for HTTP/3 detection.
/// </summary>
public sealed class Http3DetectorOptions
{
    /// <summary>
    /// Default base URL for detection. Default: MercadoBitcoin API.
    /// </summary>
    public string DefaultBaseUrl { get; set; } = "https://api.mercadobitcoin.net/api/v4";

    /// <summary>
    /// How long to cache detection results. Default: 1 hour.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Timeout for the test request. Default: 10 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Timeout for establishing connection. Default: 5 seconds.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether to automatically detect on first use. Default: false.
    /// </summary>
    public bool AutoDetect { get; set; } = false;
}

/// <summary>
/// Event arguments for HTTP/3 status changes.
/// </summary>
public sealed class Http3StatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Whether HTTP/3 is now supported.
    /// </summary>
    public required bool SupportsHttp3 { get; init; }

    /// <summary>
    /// When the detection was performed.
    /// </summary>
    public required DateTime DetectionTime { get; init; }

    /// <summary>
    /// Which attempt number this was.
    /// </summary>
    public required int AttemptNumber { get; init; }
}

/// <summary>
/// Current HTTP/3 detection status.
/// </summary>
public sealed class Http3DetectionStatus
{
    /// <summary>
    /// Whether detection has been performed.
    /// </summary>
    public required bool HasDetected { get; init; }

    /// <summary>
    /// Whether HTTP/3 is supported.
    /// </summary>
    public required bool SupportsHttp3 { get; init; }

    /// <summary>
    /// When the last detection was performed.
    /// </summary>
    public required DateTime LastDetection { get; init; }

    /// <summary>
    /// Number of detection attempts.
    /// </summary>
    public required int AttemptCount { get; init; }

    /// <summary>
    /// Whether the cached result is still valid.
    /// </summary>
    public required bool CacheValid { get; init; }

    /// <summary>
    /// The recommended HTTP version string.
    /// </summary>
    public required string RecommendedVersion { get; init; }
}

/// <summary>
/// Extension methods for HTTP/3 detection.
/// </summary>
public static class Http3DetectorExtensions
{
    /// <summary>
    /// Ensures HTTP/3 detection has been performed, running detection if necessary.
    /// </summary>
    /// <param name="detector">The detector instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if HTTP/3 is supported.</returns>
    public static async Task<bool> EnsureDetectedAsync(
        this Http3Detector detector,
        CancellationToken cancellationToken = default)
    {
        if (detector.HasDetected)
        {
            return detector.SupportsHttp3;
        }

        return await detector.DetectAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Configures an HttpClient based on HTTP/3 detection results.
    /// </summary>
    /// <param name="detector">The detector instance.</param>
    /// <param name="client">The HttpClient to configure.</param>
    public static void ConfigureClient(this Http3Detector detector, HttpClient client)
    {
        client.DefaultRequestVersion = detector.GetRecommendedVersion();
        client.DefaultVersionPolicy = detector.GetRecommendedVersionPolicy();
    }
}
