using System;
using MercadoBitcoin.Client.Http;

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

            // Criar AuthHttpClient que já inclui RetryHandler com HTTP/2
            var httpClient = AuthHttpClient.Create<MercadoBitcoinClient>();

            return new MercadoBitcoinClient(httpClient);
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
            var httpClient = AuthHttpClient.Create<MercadoBitcoinClient>(retryConfig, httpConfig);

            return new MercadoBitcoinClient(httpClient);
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


    }
}