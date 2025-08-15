using System;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Configurações para as políticas de retry
    /// </summary>
    public class RetryPolicyConfig
    {
        /// <summary>
        /// Número máximo de tentativas de retry (padrão: 3)
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay base em segundos para o exponential backoff (padrão: 1)
        /// </summary>
        public double BaseDelaySeconds { get; set; } = 1.0;

        /// <summary>
        /// Multiplicador para o exponential backoff (padrão: 2)
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Delay máximo em segundos (padrão: 30)
        /// </summary>
        public double MaxDelaySeconds { get; set; } = 30.0;

        /// <summary>
        /// Se deve fazer retry em erros de timeout (padrão: true)
        /// </summary>
        public bool RetryOnTimeout { get; set; } = true;

        /// <summary>
        /// Se deve fazer retry em erros de rate limiting (padrão: true)
        /// </summary>
        public bool RetryOnRateLimit { get; set; } = true;

        /// <summary>
        /// Se deve fazer retry em erros de servidor (5xx) (padrão: true)
        /// </summary>
        public bool RetryOnServerErrors { get; set; } = true;

        /// <summary>
        /// Calcula o delay para uma tentativa específica usando exponential backoff
        /// </summary>
        /// <param name="retryAttempt">Número da tentativa (1, 2, 3...)</param>
        /// <returns>TimeSpan com o delay calculado</returns>
        public TimeSpan CalculateDelay(int retryAttempt)
        {
            // Garante que BaseDelaySeconds seja não-negativo
            var baseDelay = Math.Max(0, BaseDelaySeconds);
            var multiplier = BackoffMultiplier;
            var maxDelay = Math.Max(0, MaxDelaySeconds);
            
            var delay = baseDelay * Math.Pow(multiplier, retryAttempt - 1);
            delay = Math.Min(delay, maxDelay);
            return TimeSpan.FromSeconds(Math.Max(0, delay));
        }
    }
}