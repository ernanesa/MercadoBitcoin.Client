using System;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

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
            // Use default configuration if not provided
            retryConfig ??= new RetryPolicyConfig();
            var httpConfig = HttpConfiguration.CreateHttp2Default();

            // Create a single AuthHttpClient (DelegatingHandler) and build HttpClient with it
            var authHandler = new AuthHttpClient(retryConfig, httpConfig);
            var httpClient = new HttpClient(authHandler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4"),
                Timeout = TimeSpan.FromSeconds(httpConfig.TimeoutSeconds),
                DefaultRequestVersion = httpConfig.HttpVersion,
                DefaultVersionPolicy = httpConfig.VersionPolicy
            };

            return new MercadoBitcoinClient(httpClient, authHandler);
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
            // Use default configuration optimized for HTTP/2
            retryConfig ??= new RetryPolicyConfig
            {
                MaxRetryAttempts = 3,
                BaseDelaySeconds = 0.5, // HTTP/2 allows faster retry
                BackoffMultiplier = 1.5,
                MaxDelaySeconds = 15.0,
                RetryOnTimeout = true,
                RetryOnRateLimit = true,
                RetryOnServerErrors = true
            };

            httpConfig ??= HttpConfiguration.CreateHttp2Default();

            // Create single handler + shared HttpClient
            var authHandler = new AuthHttpClient(retryConfig, httpConfig);
            var httpClient = new HttpClient(authHandler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4"),
                Timeout = TimeSpan.FromSeconds(httpConfig.TimeoutSeconds),
                DefaultRequestVersion = httpConfig.HttpVersion,
                DefaultVersionPolicy = httpConfig.VersionPolicy
            };
            return new MercadoBitcoinClient(httpClient, authHandler);
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

        /// <summary>
        /// Registers MercadoBitcoinClient in the DI container with IHttpClientFactory integration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Client options configuration</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddMercadoBitcoinClient(
            this IServiceCollection services,
            Action<MercadoBitcoinClientOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Register options
            services.Configure(configureOptions);

            // Register AuthHttpClient with scoped lifecycle for per-request isolation
            services.AddScoped<AuthHttpClient>();

            // Register MercadoBitcoinClient using AddHttpClient for IHttpClientFactory integration
            services.AddHttpClient<MercadoBitcoinClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;

                // Configure HttpClient based on options
                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(options.HttpConfiguration.TimeoutSeconds);
                httpClient.DefaultRequestVersion = options.HttpConfiguration.HttpVersion;
                httpClient.DefaultVersionPolicy = options.HttpConfiguration.VersionPolicy;
            })
            .AddHttpMessageHandler<AuthHttpClient>(); // Add AuthHttpClient to the pipeline

            return services;
        }
    }
}