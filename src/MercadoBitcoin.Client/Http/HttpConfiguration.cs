using System;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// HTTP configurations for the MercadoBitcoin client
    /// </summary>
    public class HttpConfiguration
    {
        /// <summary>
        /// HTTP version to be used (default: 2.0 for HTTP/2)
        /// </summary>
        public Version HttpVersion { get; set; } = new Version(2, 0);

        /// <summary>
        /// HTTP version policy (default: RequestVersionOrLower)
        /// </summary>
        public HttpVersionPolicy VersionPolicy { get; set; } = HttpVersionPolicy.RequestVersionOrLower;

        /// <summary>
        /// Timeout for HTTP requests in seconds (default: 30)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Enable automatic compression (default: true)
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Enable HTTP/2 Server Push (default: true)
        /// </summary>
        public bool EnableHttp2ServerPush { get; set; } = true;

        /// <summary>
        /// Maximum connection pool size (default: 100)
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = 100;

        /// <summary>
        /// Connection lifetime in seconds (default: 300 - 5 minutes)
        /// </summary>
        public int ConnectionLifetimeSeconds { get; set; } = 300;

        /// <summary>
        /// Creates a default configuration optimized for HTTP/2
        /// </summary>
        /// <returns>Optimized HTTP configuration</returns>
        public static HttpConfiguration CreateHttp2Default()
        {
            return new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                TimeoutSeconds = 30,
                EnableCompression = true,
                EnableHttp2ServerPush = true,
                MaxConnectionsPerServer = 100,
                ConnectionLifetimeSeconds = 300
            };
        }

        /// <summary>
        /// Creates a configuration optimized for HTTP/3 (QUIC)
        /// </summary>
        /// <returns>Optimized HTTP/3 configuration</returns>
        public static HttpConfiguration CreateHttp3Default()
        {
            return new HttpConfiguration
            {
                HttpVersion = new Version(3, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                TimeoutSeconds = 30,
                EnableCompression = true,
                EnableHttp2ServerPush = false, // HTTP/3 does not use push in the same way as H2
                MaxConnectionsPerServer = 100,
                ConnectionLifetimeSeconds = 300
            };
        }

        /// <summary>
        /// Creates a configuration for HTTP/1.1 (compatibility)
        /// </summary>
        /// <returns>HTTP 1.1 configuration</returns>
        public static HttpConfiguration CreateHttp11Fallback()
        {
            return new HttpConfiguration
            {
                HttpVersion = new Version(1, 1),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                TimeoutSeconds = 30,
                EnableCompression = true,
                EnableHttp2ServerPush = false,
                MaxConnectionsPerServer = 50,
                ConnectionLifetimeSeconds = 120
            };
        }

        /// <summary>
        /// Creates a configuration optimized for trading (low latency)
        /// </summary>
        /// <returns>HTTP configuration for trading</returns>
        public static HttpConfiguration CreateTradingOptimized()
        {
            return new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                TimeoutSeconds = 15, // Lower timeout for trading
                EnableCompression = false, // Disable compression for lower latency
                EnableHttp2ServerPush = true,
                MaxConnectionsPerServer = 200, // More connections for trading
                ConnectionLifetimeSeconds = 600 // Longer lasting connections
            };
        }
    }
}