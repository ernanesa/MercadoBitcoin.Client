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
        /// Se deve tentar respeitar o header Retry-After quando presente em respostas 429 (padrão: true)
        /// </summary>
        public bool RespectRetryAfterHeader { get; set; } = true;

        /// <summary>
        /// Habilita o uso de Circuit Breaker para evitar tempestade de falhas (padrão: true)
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Número de falhas (consecutivas) antes de abrir o circuito (padrão: 8)
        /// </summary>
        public int CircuitBreakerFailuresBeforeBreaking { get; set; } = 8;

        /// <summary>
        /// Janela de tempo (em segundos) que o circuito permanecerá aberto antes de meia-abertura (padrão: 30)
        /// </summary>
        public int CircuitBreakerDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Expõe callback opcional para eventos de retry (tentativa, atraso, código HTTP) - útil para métricas
        /// </summary>
        public Action<RetryEvent>? OnRetryEvent { get; set; }

        /// <summary>
        /// Expõe callback opcional para eventos de circuit breaker (estado abre / meia-abre / fecha)
        /// </summary>
        public Action<CircuitBreakerEvent>? OnCircuitBreakerEvent { get; set; }

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
            var final = TimeSpan.FromSeconds(Math.Max(0, delay));

            // Aplica jitter opcional para evitar sincronização de bursts entre múltiplos clientes
            if (EnableJitter && JitterMillisecondsMax > 0)
            {
                // Random.Shared é thread-safe a partir do .NET 6+
                var jitterMs = Random.Shared.Next(0, JitterMillisecondsMax + 1);
                var jitter = TimeSpan.FromMilliseconds(jitterMs);
                var candidate = final + jitter;
                if (candidate.TotalSeconds > maxDelay)
                {
                    // Mantém limite superior absoluto
                    candidate = TimeSpan.FromSeconds(maxDelay);
                }
                final = candidate;
            }
            return final;
        }

        /// <summary>
        /// Habilita jitter (aleatoriedade) adicional ao backoff para reduzir thundering herd (padrão: true)
        /// </summary>
        public bool EnableJitter { get; set; } = true;

        /// <summary>
        /// Jitter máximo em milissegundos adicionado ao delay calculado (padrão: 250ms)
        /// </summary>
        public int JitterMillisecondsMax { get; set; } = 250;

        /// <summary>
        /// Habilita emissão de métricas (System.Diagnostics.Metrics) para observabilidade (padrão: true)
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
    }

    /// <summary>
    /// Dados enviados em cada evento de retry
    /// </summary>
    public readonly record struct RetryEvent(int Attempt, TimeSpan PlannedDelay, TimeSpan? OverrideDelay, int? StatusCode, bool FromCircuitBreaker);

    /// <summary>
    /// Estados notificados do circuit breaker
    /// </summary>
    public enum CircuitBreakerState
    {
        Open,
        HalfOpen,
        Closed
    }

    /// <summary>
    /// Evento emitido pelo circuit breaker
    /// </summary>
    public readonly record struct CircuitBreakerEvent(CircuitBreakerState State, string Reason, int Failures, TimeSpan Duration);
}