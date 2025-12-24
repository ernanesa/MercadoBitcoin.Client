using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Advanced HTTP client configuration for Beast Mode optimizations.
    /// Configures SocketsHttpHandler with aggressive connection pooling, HTTP/2 multiplexing,
    /// optional HTTP/3 (QUIC) support, and aggressive keep-alive policies for HFT scenarios.
    /// </summary>
    public static class HttpClientConfiguration
    {
        /// <summary>
        /// Creates an optimized SocketsHttpHandler for high-frequency trading scenarios.
        /// </summary>
        /// <param name="enableHttp3">Enable HTTP/3 (QUIC) for lower latency on UDP-friendly networks.</param>
        /// <param name="enableCompression">Enable automatic decompression of responses. Disabled by default for small payloads (Tickers).</param>
        /// <returns>Configured SocketsHttpHandler ready for HFT workloads.</returns>
        public static SocketsHttpHandler CreateBeastModeHttpHandler(
            bool enableHttp3 = false,
            bool enableCompression = false)
        {
            var handler = new SocketsHttpHandler
            {
                // ==== CONNECTION POOLING ====
                // Periodically refresh connections (2-5 min) to respect DNS changes in cloud environments
                // while maintaining warm connections for bursts.
                PooledConnectionLifetime = TimeSpan.FromSeconds(180), // 3 minutes

                // Aggressively close idle connections to free OS resources
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(60), // 1 minute

                // Increase per-server connections for parallel batch operations (fan-out)
                MaxConnectionsPerServer = 100,

                // ==== HTTP/2 OPTIMIZATION ====
                // Force HTTP/2 as default (vastly superior to HTTP/1.1)
                // SocketsHttpHandler defaults to HTTP/1.1 on non-Windows platforms without this
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 3,

                // ==== TIMEOUT CONFIGURATION ====
                // Fail fast is better than waiting in HFT
                ConnectTimeout = TimeSpan.FromSeconds(5),

                // ==== COMPRESSION ====
                // Disable compression for small payloads like Tickers (<1KB)
                // CPU cost of decompression exceeds network latency savings for small responses
                AutomaticDecompression = enableCompression
                    ? (DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip)
                    : DecompressionMethods.None,

                // ==== KEEP-ALIVE ====
                // Detect and recycle dead connections proactively before critical trading operations fail
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30), // Ping every 30s on idle connections
                KeepAlivePingDelay = TimeSpan.FromSeconds(10),   // Initial delay before first ping

                // ==== SSL/TLS ====
                // Use modern TLS with proper session resumption (already handled by OS)
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls13 |
                                         System.Security.Authentication.SslProtocols.Tls12,
                },

                // ==== HTTP VERSION PREFERENCE ====
                // Default to HTTP/2.0; optionally enable HTTP/3 (QUIC) if available
                // HTTP/3 eliminates Head-of-Line blocking at transport layer, crucial for unreliable networks
            };

            // HTTP/3 (QUIC) configuration - eliminates Head-of-Line Blocking
            // Note: Version and VersionPolicy are set on the HttpClient or HttpRequestMessage, not SocketsHttpHandler.

            return handler;
        }

        /// <summary>
        /// Creates an optimized HttpClient with Beast Mode handler and sensible timeout defaults.
        /// </summary>
        /// <param name="enableHttp3">Enable HTTP/3 (QUIC) support.</param>
        /// <param name="enableCompression">Enable gzip/brotli compression for responses.</param>
        /// <param name="timeout">Global request timeout. Defaults to 30 seconds.</param>
        /// <returns>Configured HttpClient optimized for high-frequency trading.</returns>
        public static HttpClient CreateBeastModeHttpClient(
            bool enableHttp3 = false,
            bool enableCompression = false,
            TimeSpan? timeout = null)
        {
            var handler = CreateBeastModeHttpHandler(enableHttp3, enableCompression);
            var client = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30),
                DefaultRequestVersion = enableHttp3 ? HttpVersion.Version30 : HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            // Set sensible default headers
            client.DefaultRequestHeaders.Add("User-Agent", "MercadoBitcoin.Client/5.0 (Beast Mode; .NET 10)");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "identity"); // Disable transparent decompression unless explicitly configured

            return client;
        }

        /// <summary>
        /// Configures an existing HttpClientBuilder (from IHttpClientFactory) with Beast Mode settings.
        /// Use this when integrating with Microsoft.Extensions.Http.
        /// </summary>
        public static IHttpClientBuilder ConfigureBeastMode(
            this IHttpClientBuilder builder,
            bool enableHttp3 = false,
            bool enableCompression = false)
        {
            builder.ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestVersion = enableHttp3 ? HttpVersion.Version30 : HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                client.DefaultRequestHeaders.Add("User-Agent", "MercadoBitcoin.Client/5.0 (Beast Mode; .NET 10)");
            });

            builder.ConfigurePrimaryHttpMessageHandler(_ =>
                CreateBeastModeHttpHandler(enableHttp3, enableCompression));

            return builder;
        }
    }
}
