using MercadoBitcoin.Client.Http;
using System;

namespace MercadoBitcoin.Client.Configuration
{
    /// <summary>
    /// Opções de configuração para o MercadoBitcoinClient
    /// </summary>
    public class MercadoBitcoinClientOptions
    {
        /// <summary>
        /// Limite de requisições por segundo (rate limit client-side). Padrão: 5 req/s.
        /// </summary>
        public int RequestsPerSecond { get; set; } = 5;
        /// <summary>
        /// URL base da API (padrão: https://api.mercadobitcoin.net/api/v4)
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.mercadobitcoin.net/api/v4";

        /// <summary>
        /// Configuração HTTP para o cliente
        /// </summary>
        public HttpConfiguration HttpConfiguration { get; set; } = HttpConfiguration.CreateHttp2Default();

        /// <summary>
        /// Configuração das políticas de retry
        /// </summary>
        public RetryPolicyConfig RetryPolicyConfig { get; set; } = new RetryPolicyConfig();

        /// <summary>
        /// Timeout para requisições HTTP em segundos (padrão: 30)
        /// </summary>
        public int TimeoutSeconds
        {
            get => HttpConfiguration.TimeoutSeconds;
            set => HttpConfiguration.TimeoutSeconds = value;
        }

        /// <summary>
        /// Versão HTTP a ser utilizada (padrão: 2.0 para HTTP/2)
        /// </summary>
        public Version HttpVersion
        {
            get => HttpConfiguration.HttpVersion;
            set => HttpConfiguration.HttpVersion = value;
        }

        /// <summary>
        /// Política de versão HTTP (padrão: RequestVersionOrLower)
        /// </summary>
        public HttpVersionPolicy VersionPolicy
        {
            get => HttpConfiguration.VersionPolicy;
            set => HttpConfiguration.VersionPolicy = value;
        }

        /// <summary>
        /// Número máximo de tentativas de retry (padrão: 3)
        /// </summary>
        public int MaxRetryAttempts
        {
            get => RetryPolicyConfig.MaxRetryAttempts;
            set => RetryPolicyConfig.MaxRetryAttempts = value;
        }

        /// <summary>
        /// Delay base em segundos para o exponential backoff (padrão: 1)
        /// </summary>
        public double BaseDelaySeconds
        {
            get => RetryPolicyConfig.BaseDelaySeconds;
            set => RetryPolicyConfig.BaseDelaySeconds = value;
        }

        /// <summary>
        /// Multiplicador para o exponential backoff (padrão: 2)
        /// </summary>
        public double BackoffMultiplier
        {
            get => RetryPolicyConfig.BackoffMultiplier;
            set => RetryPolicyConfig.BackoffMultiplier = value;
        }

        /// <summary>
        /// Delay máximo em segundos (padrão: 30)
        /// </summary>
        public double MaxDelaySeconds
        {
            get => RetryPolicyConfig.MaxDelaySeconds;
            set => RetryPolicyConfig.MaxDelaySeconds = value;
        }

        /// <summary>
        /// Se deve fazer retry em erros de timeout (padrão: true)
        /// </summary>
        public bool RetryOnTimeout
        {
            get => RetryPolicyConfig.RetryOnTimeout;
            set => RetryPolicyConfig.RetryOnTimeout = value;
        }

        /// <summary>
        /// Se deve fazer retry em erros de rate limiting (padrão: true)
        /// </summary>
        public bool RetryOnRateLimit
        {
            get => RetryPolicyConfig.RetryOnRateLimit;
            set => RetryPolicyConfig.RetryOnRateLimit = value;
        }

        /// <summary>
        /// Se deve fazer retry em erros de servidor (5xx) (padrão: true)
        /// </summary>
        public bool RetryOnServerErrors
        {
            get => RetryPolicyConfig.RetryOnServerErrors;
            set => RetryPolicyConfig.RetryOnServerErrors = value;
        }
    }
}