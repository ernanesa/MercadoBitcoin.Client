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
    /// Extensões para facilitar a configuração do MercadoBitcoinClient
    /// </summary>
    public static class MercadoBitcoinClientExtensions
    {
        /// <summary>
        /// Cria uma instância do MercadoBitcoinClient com retry policies configuradas e HTTP/2 habilitado
        /// </summary>
        /// <param name="retryConfig">Configuração personalizada de retry (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient com HTTP/2</returns>
        public static MercadoBitcoinClient CreateWithRetryPolicies(
            RetryPolicyConfig? retryConfig = null)
        {
            // Usar configuração padrão se não fornecida
            retryConfig ??= new RetryPolicyConfig();
            var httpConfig = HttpConfiguration.CreateHttp2Default();

            // Criar AuthHttpClient que já inclui RetryHandler com HTTP/2
            var authHttpClient = new AuthHttpClient(retryConfig, httpConfig);

            return new MercadoBitcoinClient(authHttpClient);
        }

        /// <summary>
        /// Cria uma configuração de retry otimizada para trading (mais agressiva) com HTTP/2
        /// </summary>
        /// <returns>Configuração de retry para operações de trading</returns>
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
        /// Cria uma instância do MercadoBitcoinClient otimizada para HTTP/2
        /// </summary>
        /// <param name="retryConfig">Configuração personalizada de retry (opcional)</param>
        /// <param name="httpConfig">Configuração HTTP personalizada (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient com HTTP/2 otimizado</returns>
        public static MercadoBitcoinClient CreateWithHttp2(
            RetryPolicyConfig? retryConfig = null,
            HttpConfiguration? httpConfig = null)
        {
            // Usar configuração padrão otimizada para HTTP/2
            retryConfig ??= new RetryPolicyConfig
            {
                MaxRetryAttempts = 3,
                BaseDelaySeconds = 0.5, // HTTP/2 permite retry mais rápido
                BackoffMultiplier = 1.5,
                MaxDelaySeconds = 15.0,
                RetryOnTimeout = true,
                RetryOnRateLimit = true,
                RetryOnServerErrors = true
            };
            
            httpConfig ??= HttpConfiguration.CreateHttp2Default();

            // Criar AuthHttpClient com configurações HTTP/2
            var authHttpClient = new AuthHttpClient(retryConfig, httpConfig);

            return new MercadoBitcoinClient(authHttpClient);
        }

        /// <summary>
        /// Cria uma configuração de retry conservadora para consultas públicas
        /// </summary>
        /// <returns>Configuração de retry para consultas públicas</returns>
        public static RetryPolicyConfig CreatePublicDataRetryConfig()
        {
            return new RetryPolicyConfig
            {
                MaxRetryAttempts = 2,
                BaseDelaySeconds = 2.0,
                BackoffMultiplier = 2.0,
                MaxDelaySeconds = 30.0,
                RetryOnTimeout = true,
                RetryOnRateLimit = false, // Dados públicos geralmente não têm rate limit
                RetryOnServerErrors = true
            };
        }

        /// <summary>
        /// Cria uma instância do MercadoBitcoinClient otimizada para trading com HTTP/2
        /// </summary>
        /// <param name="retryConfig">Configuração personalizada de retry (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient</returns>
        public static MercadoBitcoinClient CreateForTrading(RetryPolicyConfig? retryConfig = null)
        {
            var tradingRetryConfig = retryConfig ?? CreateTradingRetryConfig();
            var httpConfig = HttpConfiguration.CreateTradingOptimized();
            
            return CreateWithHttp2(tradingRetryConfig, httpConfig);
        }

        /// <summary>
        /// Cria uma instância do MercadoBitcoinClient otimizada para desenvolvimento com HTTP/2
        /// </summary>
        /// <param name="retryConfig">Configuração personalizada de retry (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient</returns>
        public static MercadoBitcoinClient CreateForDevelopment(RetryPolicyConfig? retryConfig = null)
        {
            var publicRetryConfig = retryConfig ?? CreatePublicDataRetryConfig();
            var httpConfig = HttpConfiguration.CreateHttp2Default();
            
            return CreateWithHttp2(publicRetryConfig, httpConfig);
        }

        /// <summary>
        /// Registra o MercadoBitcoinClient no contêiner de DI com integração ao IHttpClientFactory
        /// </summary>
        /// <param name="services">Coleção de serviços</param>
        /// <param name="configureOptions">Configuração das opções do cliente</param>
        /// <returns>IServiceCollection para encadeamento</returns>
        public static IServiceCollection AddMercadoBitcoinClient(
            this IServiceCollection services,
            Action<MercadoBitcoinClientOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Registra as opções
            services.Configure(configureOptions);

            // Registra o AuthHttpClient com ciclo de vida escopado para isolamento por requisição
            services.AddScoped<AuthHttpClient>();

            // Registra o MercadoBitcoinClient usando AddHttpClient para integração com IHttpClientFactory
            services.AddHttpClient<MercadoBitcoinClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;
                
                // Configura o HttpClient baseado nas opções
                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(options.HttpConfiguration.TimeoutSeconds);
                httpClient.DefaultRequestVersion = options.HttpConfiguration.HttpVersion;
                httpClient.DefaultVersionPolicy = options.HttpConfiguration.VersionPolicy;
            })
            .AddHttpMessageHandler<AuthHttpClient>(); // Adiciona o AuthHttpClient ao pipeline

            return services;
        }
    }
}