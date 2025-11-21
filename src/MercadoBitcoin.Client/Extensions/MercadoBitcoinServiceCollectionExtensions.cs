using System;
using System.Net.Http;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Internal.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

        private static IHttpClientBuilder AddMercadoBitcoinClientCore(IServiceCollection services)
        {
            // Register TokenStore as Singleton (assuming one client configuration per app, or at least per container)
            // If multiple named clients are needed with different creds, this needs to be Scoped or keyed.
            // For simplicity in v4.0, we use Singleton as the client is typically singleton.
            services.TryAddSingleton<TokenStore>();

            // Register Options as Singleton for injection into Client constructor
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value);
            
            // Register dependencies for RetryHandler
            services.TryAddSingleton<RetryPolicyConfig>(sp => sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value.RetryPolicyConfig ?? new RetryPolicyConfig());
            services.TryAddSingleton<HttpConfiguration>(sp => sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value.HttpConfiguration ?? HttpConfiguration.CreateHttp2Default());

            // Register Handlers
            services.TryAddTransient<AuthenticationHandler>();
            // Explicitly register RetryHandler with factory to avoid constructor ambiguity
            services.TryAddTransient<RetryHandler>(sp => new RetryHandler(
                sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>()
            ));
            
            // AuthHttpClient needs to be Transient. Explicitly use the constructor we want.
            services.TryAddTransient<AuthHttpClient>(sp => new AuthHttpClient(
                sp.GetRequiredService<TokenStore>(),
                sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>()
            ));

            // Register the Client
            var builder = services.AddHttpClient<MercadoBitcoinClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler((sp) =>
            {
                var options = sp.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;
                return new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 20
                };
            })
            // Add Handlers in order: Outer -> Inner
            // Request flow: AuthHttpClient -> RetryHandler -> AuthenticationHandler -> Network
            // Response flow: Network -> AuthenticationHandler (handles 401) -> RetryHandler -> AuthHttpClient (adds token) -> Client
            .AddHttpMessageHandler<AuthHttpClient>()
            .AddHttpMessageHandler<RetryHandler>()
            .AddHttpMessageHandler<AuthenticationHandler>();

            return builder;
        }
    }
}
