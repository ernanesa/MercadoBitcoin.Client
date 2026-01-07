using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Diagnostics;
using MercadoBitcoin.Client.Internal.Optimization;
using MercadoBitcoin.Client.Internal.Security;
using MercadoBitcoin.Client.Models;
using MercadoBitcoin.Client.Trading;
using MercadoBitcoin.Client.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MercadoBitcoin.Client.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up MercadoBitcoinClient in an IServiceCollection.
    /// </summary>
    public static class MercadoBitcoinServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the MercadoBitcoinClient to the service collection.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMercadoBitcoinClient(
            this IServiceCollection services,
            Action<MercadoBitcoinClientOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);

            return AddMercadoBitcoinClientCore(services);
        }

        /// <summary>
        /// Adds the MercadoBitcoinClient to the service collection using IConfiguration.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configuration">The configuration section to bind to MercadoBitcoinClientOptions.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Binding strongly typed objects to configuration values may require generating dynamic code at runtime.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Binding strongly typed objects to configuration values may require generating dynamic code at runtime.")]
        public static IHttpClientBuilder AddMercadoBitcoinClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.Configure<MercadoBitcoinClientOptions>(configuration);

            return AddMercadoBitcoinClientCore(services);
        }

        /// <summary>
        /// Adds the MercadoBitcoinWebSocketClient to the service collection for real-time data streaming.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Action to configure WebSocket options (optional).</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinWebSocketClient(
            this IServiceCollection services,
            Action<WebSocketClientOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.TryAddSingleton(Microsoft.Extensions.Options.Options.Create(new WebSocketClientOptions()));
            }

            services.TryAddSingleton<MercadoBitcoinWebSocketClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<WebSocketClientOptions>>().Value;
                var logger = sp.GetService<ILogger<MercadoBitcoinWebSocketClient>>();
                return new MercadoBitcoinWebSocketClient(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds both MercadoBitcoinClient and MercadoBitcoinWebSocketClient to the service collection.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureRestOptions">Action to configure REST client options.</param>
        /// <param name="configureWebSocketOptions">Action to configure WebSocket options (optional).</param>
        /// <returns>The IHttpClientBuilder for further REST client configuration.</returns>
        public static IHttpClientBuilder AddMercadoBitcoinClients(
            this IServiceCollection services,
            Action<MercadoBitcoinClientOptions> configureRestOptions,
            Action<WebSocketClientOptions>? configureWebSocketOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureRestOptions == null) throw new ArgumentNullException(nameof(configureRestOptions));

            services.AddMercadoBitcoinWebSocketClient(configureWebSocketOptions);
            return services.AddMercadoBitcoinClient(configureRestOptions);
        }

        /// <summary>
        /// Adds high-performance trading components to the service collection.
        /// Includes HighPerformanceMarketData, HighPerformanceOrderManager, and RateLimitBudget.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Optional action to configure high-performance options.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinHighPerformanceTrading(
            this IServiceCollection services,
            Action<HighPerformanceTradingOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var options = new HighPerformanceTradingOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

            // Add Rate Limit Budget (singleton - shared across all trading)
            services.TryAddSingleton<RateLimitBudget>(sp =>
            {
                var logger = sp.GetService<ILogger<RateLimitBudget>>();
                return new RateLimitBudget(logger);
            });

            // Add High Performance Market Data (singleton)
            if (options.EnableMarketData)
            {
                services.TryAddSingleton<HighPerformanceMarketData>(sp =>
                {
                    var wsOptions = sp.GetService<IOptions<WebSocketClientOptions>>()?.Value;
                    var logger = sp.GetService<ILogger<HighPerformanceMarketData>>();
                    return new HighPerformanceMarketData(wsOptions, logger);
                });
            }

            // Add High Performance Order Manager factory (scoped - per request/user)
            services.TryAddScoped<Func<string, HighPerformanceOrderManager>>(sp =>
            {
                return accountId =>
                {
                    var client = sp.GetRequiredService<MercadoBitcoinClient>();
                    var hpOptions = sp.GetRequiredService<IOptions<HighPerformanceTradingOptions>>().Value;
                    var logger = sp.GetService<ILogger<HighPerformanceOrderManager>>();
                    return new HighPerformanceOrderManager(client, accountId, hpOptions.OrderPoolSize, logger);
                };
            });

            return services;
        }

        /// <summary>
        /// Adds OpenTelemetry tracing for MercadoBitcoinClient.
        /// </summary>
        /// <param name="builder">The TracerProviderBuilder.</param>
        /// <returns>The TracerProviderBuilder for chaining.</returns>
        public static TracerProviderBuilder AddMercadoBitcoinInstrumentation(
            this TracerProviderBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return builder.AddSource(MercadoBitcoinTelemetry.ActivitySourceName);
        }

        /// <summary>
        /// Adds connection warm-up service for proactive connection establishment.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Optional action to configure warm-up options.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinConnectionWarmUp(
            this IServiceCollection services,
            Action<ConnectionWarmUpOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var options = new ConnectionWarmUpOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

            services.TryAddSingleton<ConnectionWarmUp>(sp =>
            {
                var client = sp.GetRequiredService<MercadoBitcoinClient>();
                var httpClient = GetHttpClientFromMercadoBitcoinClient(client);
                var warmUpOptions = sp.GetRequiredService<IOptions<ConnectionWarmUpOptions>>().Value;
                var logger = sp.GetService<ILogger<ConnectionWarmUp>>();
                return new ConnectionWarmUp(httpClient, warmUpOptions, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds proactive token refresh service.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Optional action to configure refresh options.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinProactiveTokenRefresh(
            this IServiceCollection services,
            Action<ProactiveTokenRefreshOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var options = new ProactiveTokenRefreshOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

            services.TryAddScoped<ProactiveTokenRefresher>(sp =>
            {
                var tokenStore = sp.GetRequiredService<TokenStore>();
                var refreshOptions = sp.GetRequiredService<IOptions<ProactiveTokenRefreshOptions>>().Value;
                var logger = sp.GetService<ILogger<ProactiveTokenRefresher>>();
                return new ProactiveTokenRefresher(tokenStore, refreshOptions.RefreshBefore, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds HTTP/3 auto-detection service for optimal protocol selection.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Optional action to configure HTTP/3 detection options.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinHttp3Detection(
            this IServiceCollection services,
            Action<Http3DetectorOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var options = new Http3DetectorOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

            services.TryAddSingleton<Http3Detector>(sp =>
            {
                var detectorOptions = sp.GetRequiredService<IOptions<Http3DetectorOptions>>().Value;
                var logger = sp.GetService<ILogger<Http3Detector>>();
                return new Http3Detector(detectorOptions, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds order tracking service for monitoring order execution status.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Optional action to configure order tracker options.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinOrderTracker(
            this IServiceCollection services,
            Action<OrderTrackerOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var options = new OrderTrackerOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

            services.TryAddScoped<OrderTracker>(sp =>
            {
                var client = sp.GetRequiredService<MercadoBitcoinClient>();
                var trackerOptions = sp.GetRequiredService<IOptions<OrderTrackerOptions>>().Value;
                var logger = sp.GetService<ILogger<OrderTracker>>();
                return new OrderTracker(client, trackerOptions, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds performance monitoring service for latency measurements.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Optional action to configure performance monitor options.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinPerformanceMonitor(
            this IServiceCollection services,
            Action<PerformanceMonitorOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var options = new PerformanceMonitorOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

            services.TryAddSingleton<PerformanceMonitor>(sp =>
            {
                var monitorOptions = sp.GetRequiredService<IOptions<PerformanceMonitorOptions>>().Value;
                var logger = sp.GetService<ILogger<PerformanceMonitor>>();
                return new PerformanceMonitor(monitorOptions, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds incremental order book service for a specific symbol.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="symbol">The trading symbol (e.g., "BTC-BRL").</param>
        /// <param name="configureOptions">Optional action to configure order book options.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddMercadoBitcoinOrderBook(
            this IServiceCollection services,
            string symbol,
            Action<IncrementalOrderBookOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentNullException(nameof(symbol));

            var options = new IncrementalOrderBookOptions();
            configureOptions?.Invoke(options);

            services.AddKeyedSingleton(symbol, (sp, key) =>
            {
                var logger = sp.GetService<ILogger<IncrementalOrderBook>>();
                return new IncrementalOrderBook((string)key!, options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds OpenTelemetry metrics for MercadoBitcoinClient.
        /// </summary>
        /// <param name="builder">The MeterProviderBuilder.</param>
        /// <returns>The MeterProviderBuilder for chaining.</returns>
        public static MeterProviderBuilder AddMercadoBitcoinMetrics(
            this MeterProviderBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return builder.AddMeter(MercadoBitcoinTelemetry.MeterName);
        }

        /// <summary>
        /// Adds all MercadoBitcoin services with full feature set.
        /// Includes REST client, WebSocket, high-performance trading, health checks, and telemetry.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Action to configure client options.</param>
        /// <param name="configureHealthCheck">Optional action to configure health check options.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddMercadoBitcoinFullStack(
            this IServiceCollection services,
            Action<MercadoBitcoinClientOptions> configureOptions,
            Action<MercadoBitcoinHealthCheckOptions>? configureHealthCheck = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            // Add core client
            var builder = services.AddMercadoBitcoinClient(configureOptions);

            // Add WebSocket client
            services.AddMercadoBitcoinWebSocketClient();

            // Add high-performance trading
            services.AddMercadoBitcoinHighPerformanceTrading();

            // Add connection warm-up
            services.AddMercadoBitcoinConnectionWarmUp();

            // Add proactive token refresh
            services.AddMercadoBitcoinProactiveTokenRefresh();

            // Add health checks
            services.AddHealthChecks().AddMercadoBitcoin(configureOptions: configureHealthCheck);

            // Add order tracking
            services.AddMercadoBitcoinOrderTracker();

            // Add performance monitoring
            services.AddMercadoBitcoinPerformanceMonitor();

            // Add HTTP/3 detection
            services.AddMercadoBitcoinHttp3Detection();

            return builder;
        }

        private static IHttpClientBuilder AddMercadoBitcoinClientCore(IServiceCollection services)
        {
            // Register TokenStore as Scoped to support multi-user scenarios (per-user credentials)
            services.TryAddScoped<TokenStore>();

            // Register Credential Provider
            services.TryAddScoped<IMercadoBitcoinCredentialProvider, DefaultMercadoBitcoinCredentialProvider>();

            // Register Handlers
            services.TryAddTransient<RateLimitingHandler>();
            services.TryAddTransient<AuthenticationHandler>();
            services.TryAddTransient<RetryHandler>();
            services.TryAddTransient<AuthHttpClient>();

            // Register the Client
            var builder = services.AddHttpClient<MercadoBitcoinClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

                // Set default headers for performance
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

                // Custom User-Agent
                var userAgent = Environment.GetEnvironmentVariable("MB_USER_AGENT")
                    ?? $"MercadoBitcoin.Client/{MercadoBitcoinTelemetry.Version} (.NET)";
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;
                var httpConfig = options.HttpConfiguration;

                return new SocketsHttpHandler
                {
                    // Connection pooling
                    PooledConnectionLifetime = TimeSpan.FromSeconds(httpConfig.ConnectionLifetimeSeconds),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = httpConfig.MaxConnectionsPerServer,

                    // HTTP/2 multiplexing
                    EnableMultipleHttp2Connections = true,

                    // Keep-alive
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,

                    // Timeouts
                    ConnectTimeout = TimeSpan.FromSeconds(5),
                    Expect100ContinueTimeout = TimeSpan.FromSeconds(1),
                    ResponseDrainTimeout = TimeSpan.FromSeconds(2),

                    // Compression
                    AutomaticDecompression = httpConfig.EnableCompression
                        ? System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
                        : System.Net.DecompressionMethods.None,

                    // Security
                    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                    },

                    // Disable unused features for performance
                    UseProxy = false,
                    UseCookies = false
                };
            })
            // Handlers order: RateLimitingHandler -> AuthHttpClient -> RetryHandler -> AuthenticationHandler
            .AddHttpMessageHandler<RateLimitingHandler>()
            .AddHttpMessageHandler<AuthHttpClient>()
            .AddHttpMessageHandler<RetryHandler>()
            .AddHttpMessageHandler<AuthenticationHandler>();

            return builder;
        }

        /// <summary>
        /// Helper to extract HttpClient from MercadoBitcoinClient.
        /// Uses reflection as a fallback since HttpClient is internal.
        /// </summary>
        private static HttpClient GetHttpClientFromMercadoBitcoinClient(MercadoBitcoinClient client)
        {
            // Try to get HttpClient via reflection
            var field = client.GetType().GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field?.GetValue(client) is HttpClient httpClient)
            {
                return httpClient;
            }

            // Fallback: create a new HttpClient with default settings
            return new HttpClient
            {
                BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }

    /// <summary>
    /// Options for high-performance trading configuration.
    /// </summary>
    public sealed class HighPerformanceTradingOptions
    {
        /// <summary>
        /// Whether to enable high-performance market data aggregator. Default: true.
        /// </summary>
        public bool EnableMarketData { get; set; } = true;

        /// <summary>
        /// Initial size of the order request pool. Default: 100.
        /// </summary>
        public int OrderPoolSize { get; set; } = 100;

        /// <summary>
        /// Symbols to automatically subscribe to for market data. Default: empty.
        /// </summary>
        public IReadOnlyList<string> AutoSubscribeSymbols { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Whether to subscribe to ticker updates. Default: true.
        /// </summary>
        public bool SubscribeToTickers { get; set; } = true;

        /// <summary>
        /// Whether to subscribe to trade updates. Default: true.
        /// </summary>
        public bool SubscribeToTrades { get; set; } = true;

        /// <summary>
        /// Whether to subscribe to order book updates. Default: true.
        /// </summary>
        public bool SubscribeToOrderBook { get; set; } = true;
    }
}
