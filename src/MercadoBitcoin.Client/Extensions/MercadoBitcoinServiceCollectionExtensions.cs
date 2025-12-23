using System;
using System.Net.Http;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Policies;
using MercadoBitcoin.Client.Internal.Security;
using MercadoBitcoin.Client.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MercadoBitcoin.Client.Http;
using Microsoft.Extensions.Logging;

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
        public static IHttpClientBuilder AddMercadoBitcoinClient(this IServiceCollection services, Action<MercadoBitcoinClientOptions> configureOptions)
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
        public static IHttpClientBuilder AddMercadoBitcoinClient(this IServiceCollection services, IConfiguration configuration)
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
                services.TryAddSingleton(new WebSocketClientOptions());
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

        private static IHttpClientBuilder AddMercadoBitcoinClientCore(IServiceCollection services)
        {
            // Register TokenStore as Scoped to support multi-user scenarios (per-user credentials)
            services.TryAddScoped<TokenStore>();

            // Register Options as Scoped using IOptionsSnapshot for per-request/per-user configuration
            services.TryAddScoped(sp => sp.GetRequiredService<IOptionsSnapshot<MercadoBitcoinClientOptions>>().Value);

            // Register dependencies for RetryHandler
            services.TryAddScoped<RetryPolicyConfig>(sp => sp.GetRequiredService<MercadoBitcoinClientOptions>().RetryPolicyConfig ?? new RetryPolicyConfig());
            services.TryAddScoped<HttpConfiguration>(sp => sp.GetRequiredService<MercadoBitcoinClientOptions>().HttpConfiguration);

            // Register Handlers
            services.TryAddTransient<RateLimitingHandler>();
            services.TryAddTransient<AuthenticationHandler>();
            services.TryAddTransient(sp => new RetryHandler(sp.GetRequiredService<IOptionsSnapshot<MercadoBitcoinClientOptions>>()));
            services.TryAddTransient(sp => new AuthHttpClient(sp.GetRequiredService<TokenStore>(), sp.GetRequiredService<IOptionsSnapshot<MercadoBitcoinClientOptions>>()));

            // Register the Client
            var builder = services.AddHttpClient<MercadoBitcoinClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<MercadoBitcoinClientOptions>();
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = 50,
                EnableMultipleHttp2Connections = true,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests
            })
            // Handlers order: RateLimitingHandler -> AuthHttpClient -> RetryHandler -> AuthenticationHandler
            .AddHttpMessageHandler<RateLimitingHandler>()
            .AddHttpMessageHandler<AuthHttpClient>()
            .AddHttpMessageHandler<RetryHandler>()
            .AddHttpMessageHandler<AuthenticationHandler>()
            // Apply Polly retry policy for transient errors and 429
            .AddPolicyHandler(MercadoBitcoinPolicy.GetRetryPolicy());

            return builder;
        }
    }
}
