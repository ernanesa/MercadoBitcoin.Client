using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Internal.Optimization;

/// <summary>
/// Provides connection warm-up capabilities to reduce latency for initial requests.
/// Pre-establishes HTTP connections and performs TLS handshakes before they are needed.
/// </summary>
public sealed class ConnectionWarmUp : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConnectionWarmUp>? _logger;
    private readonly ConnectionWarmUpOptions _options;
    private readonly SemaphoreSlim _warmUpLock = new(1, 1);

    private bool _isWarmedUp;
    private DateTime _lastWarmUpTime;
    private int _warmUpAttempts;
    private bool _disposed;

    /// <summary>
    /// Event raised when warm-up completes successfully.
    /// </summary>
    public event EventHandler<WarmUpCompletedEventArgs>? WarmUpCompleted;

    /// <summary>
    /// Event raised when warm-up fails.
    /// </summary>
    public event EventHandler<WarmUpFailedEventArgs>? WarmUpFailed;

    /// <summary>
    /// Creates a new instance of ConnectionWarmUp.
    /// </summary>
    /// <param name="httpClient">The HTTP client to warm up.</param>
    /// <param name="options">Warm-up options.</param>
    /// <param name="logger">Optional logger.</param>
    public ConnectionWarmUp(
        HttpClient httpClient,
        ConnectionWarmUpOptions? options = null,
        ILogger<ConnectionWarmUp>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? new ConnectionWarmUpOptions();
        _logger = logger;
    }

    /// <summary>
    /// Gets whether the connections have been warmed up.
    /// </summary>
    public bool IsWarmedUp => Volatile.Read(ref _isWarmedUp);

    /// <summary>
    /// Gets the time of the last successful warm-up.
    /// </summary>
    public DateTime LastWarmUpTime => _lastWarmUpTime;

    /// <summary>
    /// Gets the number of warm-up attempts made.
    /// </summary>
    public int WarmUpAttempts => Volatile.Read(ref _warmUpAttempts);

    /// <summary>
    /// Warms up the HTTP connections by making lightweight requests to the API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if warm-up was successful.</returns>
    public async Task<bool> WarmUpAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var acquired = await _warmUpLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        if (!acquired)
        {
            _logger?.LogDebug("Warm-up already in progress");
            return _isWarmedUp;
        }

        try
        {
            Interlocked.Increment(ref _warmUpAttempts);

            _logger?.LogInformation("Starting connection warm-up (attempt #{Attempt})", _warmUpAttempts);
            var sw = Stopwatch.StartNew();
            var results = new List<WarmUpEndpointResult>();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.Timeout);

            // Warm up each endpoint in parallel
            var tasks = _options.WarmUpEndpoints.Select(endpoint =>
                WarmUpEndpointAsync(endpoint, cts.Token));

            var endpointResults = await Task.WhenAll(tasks);
            results.AddRange(endpointResults);

            sw.Stop();
            var successCount = results.Count(r => r.Success);
            var totalCount = results.Count;

            // Consider warm-up successful if at least one endpoint succeeded
            var success = successCount > 0;

            if (success)
            {
                Volatile.Write(ref _isWarmedUp, true);
                _lastWarmUpTime = DateTime.UtcNow;

                _logger?.LogInformation(
                    "Connection warm-up completed in {Duration}ms. {Success}/{Total} endpoints successful",
                    sw.ElapsedMilliseconds, successCount, totalCount);

                OnWarmUpCompleted(new WarmUpCompletedEventArgs
                {
                    Duration = sw.Elapsed,
                    SuccessfulEndpoints = successCount,
                    TotalEndpoints = totalCount,
                    Results = results
                });
            }
            else
            {
                _logger?.LogWarning(
                    "Connection warm-up failed. 0/{Total} endpoints successful",
                    totalCount);

                OnWarmUpFailed(new WarmUpFailedEventArgs
                {
                    Duration = sw.Elapsed,
                    Results = results,
                    Error = "All warm-up endpoints failed"
                });
            }

            return success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogDebug("Warm-up cancelled");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Warm-up timed out after {Timeout}", _options.Timeout);
            OnWarmUpFailed(new WarmUpFailedEventArgs
            {
                Duration = _options.Timeout,
                Results = new List<WarmUpEndpointResult>(),
                Error = "Warm-up timed out"
            });
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Warm-up failed with exception");
            OnWarmUpFailed(new WarmUpFailedEventArgs
            {
                Duration = TimeSpan.Zero,
                Results = new List<WarmUpEndpointResult>(),
                Error = ex.Message,
                Exception = ex
            });
            return false;
        }
        finally
        {
            _warmUpLock.Release();
        }
    }

    /// <summary>
    /// Warms up a single endpoint.
    /// </summary>
    private async Task<WarmUpEndpointResult> WarmUpEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger?.LogDebug("Warming up endpoint: {Endpoint}", endpoint);

            // Use ResponseHeadersRead to minimize data transfer
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            // Add cache-bust parameter to ensure fresh connection
            var uriBuilder = new UriBuilder(_httpClient.BaseAddress ?? new Uri("https://api.mercadobitcoin.net/api/v4/"))
            {
                Path = endpoint,
                Query = $"_warmup={DateTime.UtcNow.Ticks}"
            };
            request.RequestUri = uriBuilder.Uri;

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            sw.Stop();

            // We don't care about the status code for warm-up -
            // we just want to establish the connection
            var success = response.IsSuccessStatusCode ||
                         (int)response.StatusCode < 500;

            _logger?.LogDebug(
                "Endpoint {Endpoint} warm-up {Status} in {Duration}ms (HTTP {StatusCode})",
                endpoint,
                success ? "succeeded" : "failed",
                sw.ElapsedMilliseconds,
                (int)response.StatusCode);

            return new WarmUpEndpointResult
            {
                Endpoint = endpoint,
                Success = success,
                Duration = sw.Elapsed,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogDebug(ex, "Endpoint {Endpoint} warm-up failed", endpoint);

            return new WarmUpEndpointResult
            {
                Endpoint = endpoint,
                Success = false,
                Duration = sw.Elapsed,
                StatusCode = 0,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Performs warm-up if not already warmed up or if the warm-up has expired.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connections are warmed up.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> EnsureWarmedUpAsync(CancellationToken cancellationToken = default)
    {
        if (_isWarmedUp && DateTime.UtcNow - _lastWarmUpTime < _options.WarmUpExpiration)
        {
            return true;
        }

        return await WarmUpAsync(cancellationToken);
    }

    /// <summary>
    /// Resets the warm-up state, allowing re-warming.
    /// </summary>
    public void Reset()
    {
        Volatile.Write(ref _isWarmedUp, false);
        _logger?.LogDebug("Warm-up state reset");
    }

    private void OnWarmUpCompleted(WarmUpCompletedEventArgs args)
    {
        WarmUpCompleted?.Invoke(this, args);
    }

    private void OnWarmUpFailed(WarmUpFailedEventArgs args)
    {
        WarmUpFailed?.Invoke(this, args);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _warmUpLock.Dispose();
    }
}

/// <summary>
/// Configuration options for connection warm-up.
/// </summary>
public sealed class ConnectionWarmUpOptions
{
    /// <summary>
    /// Endpoints to use for warm-up. Default: tickers and symbols.
    /// </summary>
    public IReadOnlyList<string> WarmUpEndpoints { get; set; } = new[]
    {
        "tickers?symbols=BTC-BRL",
        "symbols?symbols=BTC-BRL"
    };

    /// <summary>
    /// Timeout for the entire warm-up operation. Default: 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long the warm-up remains valid before needing refresh. Default: 5 minutes.
    /// </summary>
    public TimeSpan WarmUpExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to automatically warm up on first request. Default: true.
    /// </summary>
    public bool AutoWarmUp { get; set; } = true;

    /// <summary>
    /// Number of parallel warm-up requests. Default: 2.
    /// </summary>
    public int ParallelRequests { get; set; } = 2;
}

/// <summary>
/// Result of warming up a single endpoint.
/// </summary>
public sealed class WarmUpEndpointResult
{
    /// <summary>
    /// The endpoint that was warmed up.
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// Whether the warm-up was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// How long the warm-up took.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// The HTTP status code returned.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Event arguments for successful warm-up completion.
/// </summary>
public sealed class WarmUpCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Total duration of the warm-up.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of successful endpoints.
    /// </summary>
    public required int SuccessfulEndpoints { get; init; }

    /// <summary>
    /// Total number of endpoints.
    /// </summary>
    public required int TotalEndpoints { get; init; }

    /// <summary>
    /// Results for each endpoint.
    /// </summary>
    public required IReadOnlyList<WarmUpEndpointResult> Results { get; init; }
}

/// <summary>
/// Event arguments for failed warm-up.
/// </summary>
public sealed class WarmUpFailedEventArgs : EventArgs
{
    /// <summary>
    /// How long was spent before failure.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Results for endpoints that were attempted.
    /// </summary>
    public required IReadOnlyList<WarmUpEndpointResult> Results { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Exception if one occurred.
    /// </summary>
    public Exception? Exception { get; init; }
}

/// <summary>
/// Extension methods for ConnectionWarmUp.
/// </summary>
public static class ConnectionWarmUpExtensions
{
    /// <summary>
    /// Creates a ConnectionWarmUp with optimized settings for trading.
    /// </summary>
    public static ConnectionWarmUp CreateForTrading(
        HttpClient httpClient,
        ILogger<ConnectionWarmUp>? logger = null)
    {
        return new ConnectionWarmUp(httpClient, new ConnectionWarmUpOptions
        {
            WarmUpEndpoints = new[]
            {
                "tickers?symbols=BTC-BRL",
                "symbols?symbols=BTC-BRL",
                "BTC-BRL/orderbook?limit=1"
            },
            Timeout = TimeSpan.FromSeconds(5),
            WarmUpExpiration = TimeSpan.FromMinutes(2),
            AutoWarmUp = true,
            ParallelRequests = 3
        }, logger);
    }

    /// <summary>
    /// Creates a ConnectionWarmUp with minimal settings for quick startup.
    /// </summary>
    public static ConnectionWarmUp CreateMinimal(
        HttpClient httpClient,
        ILogger<ConnectionWarmUp>? logger = null)
    {
        return new ConnectionWarmUp(httpClient, new ConnectionWarmUpOptions
        {
            WarmUpEndpoints = new[]
            {
                "tickers?symbols=BTC-BRL"
            },
            Timeout = TimeSpan.FromSeconds(3),
            WarmUpExpiration = TimeSpan.FromMinutes(5),
            AutoWarmUp = true,
            ParallelRequests = 1
        }, logger);
    }
}
