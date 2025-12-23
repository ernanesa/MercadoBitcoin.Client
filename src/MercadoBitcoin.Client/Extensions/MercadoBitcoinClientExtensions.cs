using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Http;

namespace MercadoBitcoin.Client.Extensions
{
    /// <summary>
    /// Extensions to facilitate MercadoBitcoinClient configuration
    /// </summary>
    public static class MercadoBitcoinClientExtensions
    {
        /// <summary>
        /// Creates a MercadoBitcoinClient instance with configured retry policies and HTTP/2 enabled
        /// </summary>
        /// <param name="retryConfig">Custom retry configuration (optional)</param>
        /// <returns>Configured MercadoBitcoinClient instance with HTTP/2</returns>
        public static MercadoBitcoinClient CreateWithRetryPolicies(
            RetryPolicyConfig? retryConfig = null)
        {
            var options = new MercadoBitcoinClientOptions
            {
                RetryPolicyConfig = retryConfig ?? new RetryPolicyConfig(),
                HttpConfiguration = HttpConfiguration.CreateHttp2Default()
            };
            return new MercadoBitcoinClient(options);
        }

        /// <summary>
        /// Creates a retry configuration optimized for trading (more aggressive) with HTTP/2
        /// </summary>
        /// <returns>Retry configuration for trading operations</returns>
        public static RetryPolicyConfig CreateTradingRetryConfig()
        {
            return new RetryPolicyConfig
            {
                MaxRetryAttempts = 5,
                BaseDelaySeconds = 0.5,
                BackoffMultiplier = 1.5,
                MaxDelaySeconds = 10.0,
                RetryOnTimeout = true,
                RetryOnRateLimit = true,
                RetryOnServerErrors = true
            };
        }

        /// <summary>
        /// Creates a MercadoBitcoinClient instance optimized for HTTP/2
        /// </summary>
        /// <param name="retryConfig">Custom retry configuration (optional)</param>
        /// <param name="httpConfig">Custom HTTP configuration (optional)</param>
        /// <returns>Configured MercadoBitcoinClient instance with optimized HTTP/2</returns>
        public static MercadoBitcoinClient CreateWithHttp2(
            RetryPolicyConfig? retryConfig = null,
            HttpConfiguration? httpConfig = null)
        {
            var options = new MercadoBitcoinClientOptions
            {
                RetryPolicyConfig = retryConfig ?? new RetryPolicyConfig
                {
                    MaxRetryAttempts = 3,
                    BaseDelaySeconds = 0.5,
                    BackoffMultiplier = 1.5,
                    MaxDelaySeconds = 15.0,
                    RetryOnTimeout = true,
                    RetryOnRateLimit = true,
                    RetryOnServerErrors = true
                },
                HttpConfiguration = httpConfig ?? HttpConfiguration.CreateHttp2Default()
            };
            return new MercadoBitcoinClient(options);
        }

        /// <summary>
        /// Creates a conservative retry configuration for public queries
        /// </summary>
        /// <returns>Retry configuration for public queries</returns>
        public static RetryPolicyConfig CreatePublicDataRetryConfig()
        {
            return new RetryPolicyConfig
            {
                MaxRetryAttempts = 2,
                BaseDelaySeconds = 2.0,
                BackoffMultiplier = 2.0,
                MaxDelaySeconds = 30.0,
                RetryOnTimeout = true,
                RetryOnRateLimit = false, // Public data usually doesn't have rate limit
                RetryOnServerErrors = true
            };
        }

        /// <summary>
        /// Creates a MercadoBitcoinClient instance optimized for trading with HTTP/2
        /// </summary>
        /// <param name="retryConfig">Custom retry configuration (optional)</param>
        /// <returns>Configured MercadoBitcoinClient instance</returns>
        public static MercadoBitcoinClient CreateForTrading(RetryPolicyConfig? retryConfig = null)
        {
            var tradingRetryConfig = retryConfig ?? CreateTradingRetryConfig();
            var httpConfig = HttpConfiguration.CreateTradingOptimized();
            return CreateWithHttp2(tradingRetryConfig, httpConfig);
        }

        /// <summary>
        /// Creates a MercadoBitcoinClient instance optimized for development with HTTP/2
        /// </summary>
        /// <param name="retryConfig">Custom retry configuration (optional)</param>
        /// <returns>Configured MercadoBitcoinClient instance</returns>
        public static MercadoBitcoinClient CreateForDevelopment(RetryPolicyConfig? retryConfig = null)
        {
            var publicRetryConfig = retryConfig ?? CreatePublicDataRetryConfig();
            var httpConfig = HttpConfiguration.CreateHttp2Default();
            return CreateWithHttp2(publicRetryConfig, httpConfig);
        }
    }
}