using System;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.WebSocket.Interfaces;

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
        /// <param name="webSocketConfig">Configuração personalizada do WebSocket (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient</returns>
        public static MercadoBitcoinClient CreateWithRetryPolicies(
            RetryPolicyConfig? retryConfig = null,
            IWebSocketConfiguration? webSocketConfig = null)
        {
            // Usar configuração padrão se não fornecida
            retryConfig ??= new RetryPolicyConfig();

            // Criar AuthHttpClient que já inclui RetryHandler
            var httpClient = AuthHttpClient.Create<MercadoBitcoinClient>();

            return new MercadoBitcoinClient(httpClient, webSocketConfig);
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

        /// <summary>
        /// Cria uma instância do MercadoBitcoinClient com configuração WebSocket otimizada para trading
        /// </summary>
        /// <param name="retryConfig">Configuração personalizada de retry (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient</returns>
        public static MercadoBitcoinClient CreateForTrading(RetryPolicyConfig? retryConfig = null)
        {
            var tradingRetryConfig = retryConfig ?? CreateTradingRetryConfig();
            var webSocketConfig = WebSocketConfiguration.CreateProduction();
            
            // Configurações otimizadas para trading
            webSocketConfig.EnableAutoReconnect = true;
            webSocketConfig.ReconnectIntervalSeconds = 1;
            webSocketConfig.MaxReconnectAttempts = 10;
            
            return CreateWithRetryPolicies(tradingRetryConfig, webSocketConfig);
        }

        /// <summary>
        /// Cria uma instância do MercadoBitcoinClient com configuração WebSocket otimizada para desenvolvimento
        /// </summary>
        /// <param name="retryConfig">Configuração personalizada de retry (opcional)</param>
        /// <returns>Instância configurada do MercadoBitcoinClient</returns>
        public static MercadoBitcoinClient CreateForDevelopment(RetryPolicyConfig? retryConfig = null)
        {
            var publicRetryConfig = retryConfig ?? CreatePublicDataRetryConfig();
            var webSocketConfig = WebSocketConfiguration.CreateDevelopment();
            
            return CreateWithRetryPolicies(publicRetryConfig, webSocketConfig);
        }

        /// <summary>
        /// Cria uma configuração WebSocket personalizada
        /// </summary>
        /// <param name="url">URL do WebSocket (opcional, usa padrão se não fornecida)</param>
        /// <param name="enableAutoReconnect">Habilitar reconexão automática</param>
        /// <param name="reconnectInterval">Intervalo entre tentativas de reconexão</param>
        /// <param name="maxReconnectAttempts">Número máximo de tentativas de reconexão</param>
        /// <param name="enableDetailedLogging">Habilitar logging detalhado</param>
        /// <returns>Configuração WebSocket personalizada</returns>
        public static IWebSocketConfiguration CreateWebSocketConfig(
            string? url = null,
            bool enableAutoReconnect = true,
            TimeSpan? reconnectInterval = null,
            int maxReconnectAttempts = 5,
            bool enableDetailedLogging = false)
        {
            var config = WebSocketConfiguration.CreateProduction();
            
            if (!string.IsNullOrEmpty(url))
                config.Url = url;
                
            config.EnableAutoReconnect = enableAutoReconnect;
            config.ReconnectIntervalSeconds = (int)(reconnectInterval ?? TimeSpan.FromSeconds(5)).TotalSeconds;
            config.MaxReconnectAttempts = maxReconnectAttempts;
            config.EnableVerboseLogging = enableDetailedLogging;
            
            return config;
        }
    }
}