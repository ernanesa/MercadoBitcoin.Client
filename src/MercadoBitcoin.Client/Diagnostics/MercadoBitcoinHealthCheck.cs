using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Diagnostics;

/// <summary>
/// Health check for MercadoBitcoin API connectivity and performance.
/// </summary>
public sealed class MercadoBitcoinHealthCheck : IHealthCheck
{
    private readonly MercadoBitcoinClient _client;
    private readonly ILogger<MercadoBitcoinHealthCheck>? _logger;
    private readonly MercadoBitcoinHealthCheckOptions _options;

    /// <summary>
    /// Creates a new instance of the health check.
    /// </summary>
    /// <param name="client">The MercadoBitcoin client to check.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="options">Health check options.</param>
    public MercadoBitcoinHealthCheck(
        MercadoBitcoinClient client,
        ILogger<MercadoBitcoinHealthCheck>? logger = null,
        MercadoBitcoinHealthCheckOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger;
        _options = options ?? new MercadoBitcoinHealthCheckOptions();
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var startTime = DateTime.UtcNow;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.Timeout);

            // Test 1: Public API connectivity (tickers)
            var tickerResult = await CheckTickersAsync(cts.Token);
            data["ticker_check"] = tickerResult.Success ? "passed" : "failed";
            data["ticker_latency_ms"] = tickerResult.LatencyMs;

            if (!tickerResult.Success)
            {
                _logger?.LogWarning("Health check failed: Ticker endpoint unreachable. Error: {Error}", tickerResult.Error);
                return HealthCheckResult.Unhealthy(
                    "Ticker endpoint unreachable",
                    tickerResult.Exception,
                    data);
            }

            // Test 2: Check latency threshold
            if (tickerResult.LatencyMs > _options.DegradedLatencyThresholdMs)
            {
                _logger?.LogWarning("Health check degraded: High latency detected ({Latency}ms)", tickerResult.LatencyMs);
                data["status_reason"] = $"High latency: {tickerResult.LatencyMs}ms > {_options.DegradedLatencyThresholdMs}ms threshold";
                return HealthCheckResult.Degraded(
                    $"API responding with high latency ({tickerResult.LatencyMs}ms)",
                    data: data);
            }

            // Test 3: Optional authenticated check
            if (_options.CheckAuthentication)
            {
                var authResult = await CheckAuthenticationAsync(cts.Token);
                data["auth_check"] = authResult.Success ? "passed" : "failed";

                if (!authResult.Success)
                {
                    _logger?.LogWarning("Health check warning: Authentication check failed. Error: {Error}", authResult.Error);
                    data["auth_error"] = authResult.Error ?? "Unknown authentication error";
                    // Auth failure is degraded, not unhealthy (public API still works)
                    return HealthCheckResult.Degraded(
                        "Authentication check failed",
                        data: data);
                }
            }

            // Test 4: Optional WebSocket check
            if (_options.CheckWebSocket)
            {
                var wsResult = await CheckWebSocketAsync(cts.Token);
                data["websocket_check"] = wsResult.Success ? "passed" : "failed";
                data["websocket_latency_ms"] = wsResult.LatencyMs;

                if (!wsResult.Success)
                {
                    _logger?.LogWarning("Health check degraded: WebSocket check failed. Error: {Error}", wsResult.Error);
                    data["websocket_error"] = wsResult.Error ?? "Unknown WebSocket error";
                    return HealthCheckResult.Degraded(
                        "WebSocket check failed",
                        data: data);
                }
            }

            // All checks passed
            var totalLatency = (DateTime.UtcNow - startTime).TotalMilliseconds;
            data["total_check_time_ms"] = totalLatency;
            data["api_version"] = "v4";
            data["client_version"] = MercadoBitcoinTelemetry.Version;

            _logger?.LogDebug("Health check passed. Total time: {TotalTime}ms", totalLatency);

            return HealthCheckResult.Healthy(
                "API is responsive and healthy",
                data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Propagate cancellation
        }
        catch (OperationCanceledException)
        {
            // Timeout
            _logger?.LogWarning("Health check timed out after {Timeout}ms", _options.Timeout.TotalMilliseconds);
            data["timeout_ms"] = _options.Timeout.TotalMilliseconds;
            return HealthCheckResult.Unhealthy(
                $"Health check timed out after {_options.Timeout.TotalMilliseconds}ms",
                data: data);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Health check failed with exception");
            data["exception_type"] = ex.GetType().Name;
            data["exception_message"] = ex.Message;
            return HealthCheckResult.Unhealthy(
                $"Health check failed: {ex.Message}",
                ex,
                data);
        }
    }

    private async Task<CheckResult> CheckTickersAsync(CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var tickers = await _client.GetTickersAsync(_options.TestSymbol, ct);
            sw.Stop();

            if (tickers == null || !tickers.Any())
            {
                return new CheckResult
                {
                    Success = false,
                    LatencyMs = sw.ElapsedMilliseconds,
                    Error = "Empty response from ticker endpoint"
                };
            }

            return new CheckResult
            {
                Success = true,
                LatencyMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckResult
            {
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

    private async Task<CheckResult> CheckAuthenticationAsync(CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // Try to get accounts - this requires valid authentication
            var accounts = await _client.GetAccountsAsync(ct);
            sw.Stop();

            if (accounts == null || !accounts.Any())
            {
                return new CheckResult
                {
                    Success = false,
                    LatencyMs = sw.ElapsedMilliseconds,
                    Error = "No accounts returned - authentication may have failed"
                };
            }

            return new CheckResult
            {
                Success = true,
                LatencyMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckResult
            {
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

    private async Task<CheckResult> CheckWebSocketAsync(CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await using var wsClient = new WebSocket.MercadoBitcoinWebSocketClient(
                new WebSocket.WebSocketClientOptions
                {
                    ConnectionTimeout = TimeSpan.FromSeconds(5),
                    AutoReconnect = false
                });

            await wsClient.ConnectAsync(ct);
            sw.Stop();

            var isConnected = wsClient.ConnectionState == WebSocket.WebSocketConnectionState.Connected;

            await wsClient.DisconnectAsync(ct);

            return new CheckResult
            {
                Success = isConnected,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = isConnected ? null : "WebSocket connection failed"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckResult
            {
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message,
                Exception = ex
            };
        }
    }

    private readonly record struct CheckResult
    {
        public required bool Success { get; init; }
        public required long LatencyMs { get; init; }
        public string? Error { get; init; }
        public Exception? Exception { get; init; }
    }
}

/// <summary>
/// Configuration options for MercadoBitcoin health checks.
/// </summary>
public sealed class MercadoBitcoinHealthCheckOptions
{
    /// <summary>
    /// The timeout for health check operations. Default: 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The symbol to use for testing public endpoints. Default: "BTC-BRL".
    /// </summary>
    public string TestSymbol { get; set; } = "BTC-BRL";

    /// <summary>
    /// Latency threshold in milliseconds above which the service is considered degraded. Default: 1000ms.
    /// </summary>
    public long DegradedLatencyThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Whether to check authentication status. Default: false.
    /// </summary>
    public bool CheckAuthentication { get; set; } = false;

    /// <summary>
    /// Whether to check WebSocket connectivity. Default: false.
    /// </summary>
    public bool CheckWebSocket { get; set; } = false;
}

/// <summary>
/// Extensions for registering MercadoBitcoin health checks.
/// </summary>
public static class MercadoBitcoinHealthCheckExtensions
{
    /// <summary>
    /// Adds the MercadoBitcoin health check to the health check builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Default: "mercadobitcoin".</param>
    /// <param name="configureOptions">Optional action to configure health check options.</param>
    /// <param name="failureStatus">The failure status. Default: Unhealthy.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <param name="timeout">Optional timeout for this specific health check.</param>
    /// <returns>The health checks builder.</returns>
    public static IHealthChecksBuilder AddMercadoBitcoin(
        this IHealthChecksBuilder builder,
        string name = "mercadobitcoin",
        Action<MercadoBitcoinHealthCheckOptions>? configureOptions = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        var options = new MercadoBitcoinHealthCheckOptions();
        configureOptions?.Invoke(options);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var client = sp.GetRequiredService<MercadoBitcoinClient>();
                var logger = sp.GetService<ILogger<MercadoBitcoinHealthCheck>>();
                return new MercadoBitcoinHealthCheck(client, logger, options);
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Adds a lightweight MercadoBitcoin health check that only tests public API connectivity.
    /// </summary>
    public static IHealthChecksBuilder AddMercadoBitcoinLite(
        this IHealthChecksBuilder builder,
        string name = "mercadobitcoin-lite",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddMercadoBitcoin(
            name,
            options =>
            {
                options.Timeout = TimeSpan.FromSeconds(5);
                options.CheckAuthentication = false;
                options.CheckWebSocket = false;
                options.DegradedLatencyThresholdMs = 500;
            },
            failureStatus,
            tags ?? new[] { "ready", "live" },
            TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Adds a comprehensive MercadoBitcoin health check that tests all components.
    /// </summary>
    public static IHealthChecksBuilder AddMercadoBitcoinFull(
        this IHealthChecksBuilder builder,
        string name = "mercadobitcoin-full",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddMercadoBitcoin(
            name,
            options =>
            {
                options.Timeout = TimeSpan.FromSeconds(15);
                options.CheckAuthentication = true;
                options.CheckWebSocket = true;
                options.DegradedLatencyThresholdMs = 2000;
            },
            failureStatus,
            tags ?? new[] { "ready" },
            TimeSpan.FromSeconds(15));
    }

    private static T? GetService<T>(this IServiceProvider sp) where T : class
    {
        return sp.GetService(typeof(T)) as T;
    }

    private static T GetRequiredService<T>(this IServiceProvider sp) where T : class
    {
        return sp.GetService(typeof(T)) as T
            ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered. Call AddMercadoBitcoinClient() first.");
    }
}
