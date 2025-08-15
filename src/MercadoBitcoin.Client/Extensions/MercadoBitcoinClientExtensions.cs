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
        /// Cria uma instância do MercadoBitcoinClient com retry policies configuradas
        /// </summary>

        /// <param name="retryConfig">Configuração personalizada de retry (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient</returns>
        public static MercadoBitcoinClient CreateWithRetryPolicies(
    
            RetryPolicyConfig? retryConfig = null)
        {
            // Usar configuração padrão se não fornecida
            retryConfig ??= new RetryPolicyConfig();

            // Criar AuthHttpClient que já inclui RetryHandler
            var httpClient = AuthHttpClient.Create<MercadoBitcoinClient>();

            return new MercadoBitcoinClient(httpClient);
        }

        /// <summary>
        /// Cria uma configuração de retry otimizada para trading (mais agressiva)
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
    }
}