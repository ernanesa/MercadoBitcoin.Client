using MercadoBitcoin.Client.Http;
using System;
using System.Net.Http;

namespace MercadoBitcoin.Client.Configuration
{
    /// <summary>
    /// Configuration options for MercadoBitcoinClient
    /// </summary>
    public class MercadoBitcoinClientOptions
    {
        /// <summary>
        /// Requests per second limit (client-side rate limit). Default: 5 req/s.
        /// </summary>
        public int RequestsPerSecond { get; set; } = 5;
        /// <summary>
        /// API Base URL (default: https://api.mercadobitcoin.net/api/v4)
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.mercadobitcoin.net/api/v4";

        /// <summary>
        /// HTTP configuration for the client
        /// </summary>
        public HttpConfiguration HttpConfiguration { get; set; } = HttpConfiguration.CreateHttp2Default();

        /// <summary>
        /// Retry policy configuration
        /// </summary>
        public RetryPolicyConfig RetryPolicyConfig { get; set; } = new RetryPolicyConfig();

        /// <summary>
        /// HTTP request timeout in seconds (default: 30)
        /// </summary>
        public int TimeoutSeconds
        {
            get => HttpConfiguration.TimeoutSeconds;
            set => HttpConfiguration.TimeoutSeconds = value;
        }

        /// <summary>
        /// HTTP version to be used (default: 2.0 for HTTP/2)
        /// </summary>
        public Version HttpVersion
        {
            get => HttpConfiguration.HttpVersion;
            set => HttpConfiguration.HttpVersion = value;
        }

        /// <summary>
        /// HTTP version policy (default: RequestVersionOrLower)
        /// </summary>
        public HttpVersionPolicy VersionPolicy
        {
            get => HttpConfiguration.VersionPolicy;
            set => HttpConfiguration.VersionPolicy = value;
        }

        /// <summary>
        /// Maximum number of retry attempts (default: 3)
        /// </summary>
        public int MaxRetryAttempts
        {
            get => RetryPolicyConfig.MaxRetryAttempts;
            set => RetryPolicyConfig.MaxRetryAttempts = value;
        }

        /// <summary>
        /// Base delay in seconds for exponential backoff (default: 1)
        /// </summary>
        public double BaseDelaySeconds
        {
            get => RetryPolicyConfig.BaseDelaySeconds;
            set => RetryPolicyConfig.BaseDelaySeconds = value;
        }

        /// <summary>
        /// Multiplier for exponential backoff (default: 2)
        /// </summary>
        public double BackoffMultiplier
        {
            get => RetryPolicyConfig.BackoffMultiplier;
            set => RetryPolicyConfig.BackoffMultiplier = value;
        }

        /// <summary>
        /// Maximum delay in seconds (default: 30)
        /// </summary>
        public double MaxDelaySeconds
        {
            get => RetryPolicyConfig.MaxDelaySeconds;
            set => RetryPolicyConfig.MaxDelaySeconds = value;
        }

        /// <summary>
        /// Whether to retry on timeout errors (default: true)
        /// </summary>
        public bool RetryOnTimeout
        {
            get => RetryPolicyConfig.RetryOnTimeout;
            set => RetryPolicyConfig.RetryOnTimeout = value;
        }

        /// <summary>
        /// Whether to retry on rate limiting errors (default: true)
        /// </summary>
        public bool RetryOnRateLimit
        {
            get => RetryPolicyConfig.RetryOnRateLimit;
            set => RetryPolicyConfig.RetryOnRateLimit = value;
        }

        /// <summary>
        /// Whether to retry on server errors (5xx) (default: true)
        /// </summary>
        public bool RetryOnServerErrors
        {
            get => RetryPolicyConfig.RetryOnServerErrors;
            set => RetryPolicyConfig.RetryOnServerErrors = value;
        }
    }
}