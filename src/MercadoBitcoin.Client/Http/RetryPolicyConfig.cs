using System;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Configuration for retry policies
    /// </summary>
    public class RetryPolicyConfig
    {
        /// <summary>
        /// Maximum number of retry attempts (default: 3)
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay in seconds for exponential backoff (default: 1)
        /// </summary>
        public double BaseDelaySeconds { get; set; } = 1.0;

        /// <summary>
        /// Multiplier for exponential backoff (default: 2)
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Maximum delay in seconds (default: 30)
        /// </summary>
        public double MaxDelaySeconds { get; set; } = 30.0;

        /// <summary>
        /// Whether to retry on timeout errors (default: true)
        /// </summary>
        public bool RetryOnTimeout { get; set; } = true;

        /// <summary>
        /// Whether to retry on rate limiting errors (default: true)
        /// </summary>
        public bool RetryOnRateLimit { get; set; } = true;

        /// <summary>
        /// Whether to retry on server errors (5xx) (default: true)
        /// </summary>
        public bool RetryOnServerErrors { get; set; } = true;

        /// <summary>
        /// Whether to try respecting the Retry-After header when present in 429 responses (default: true)
        /// </summary>
        public bool RespectRetryAfterHeader { get; set; } = true;

        /// <summary>
        /// Enables Circuit Breaker usage to avoid failure storms (default: true)
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Number of (consecutive) failures before opening the circuit (default: 8)
        /// </summary>
        public int CircuitBreakerFailuresBeforeBreaking { get; set; } = 8;

        /// <summary>
        /// Time window (in seconds) the circuit will remain open before half-open (default: 30)
        /// </summary>
        public int CircuitBreakerDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Exposes optional callback for retry events (attempt, delay, HTTP code) - useful for metrics
        /// </summary>
        public Action<RetryEvent>? OnRetryEvent { get; set; }

        /// <summary>
        /// Exposes optional callback for circuit breaker events (open / half-open / closed state)
        /// </summary>
        public Action<CircuitBreakerEvent>? OnCircuitBreakerEvent { get; set; }

        /// <summary>
        /// Calculates the delay for a specific attempt using exponential backoff
        /// </summary>
        /// <param name="retryAttempt">Attempt number (1, 2, 3...)</param>
        /// <returns>TimeSpan with the calculated delay</returns>
        public TimeSpan CalculateDelay(int retryAttempt)
        {
            // Ensures BaseDelaySeconds is non-negative
            var baseDelay = Math.Max(0, BaseDelaySeconds);
            var multiplier = BackoffMultiplier;
            var maxDelay = Math.Max(0, MaxDelaySeconds);

            var delay = baseDelay * Math.Pow(multiplier, retryAttempt - 1);
            delay = Math.Min(delay, maxDelay);
            var final = TimeSpan.FromSeconds(Math.Max(0, delay));

            // Applies optional jitter to avoid burst synchronization among multiple clients
            if (EnableJitter && JitterMillisecondsMax > 0)
            {
                // Random.Shared is thread-safe from .NET 6+
                var jitterMs = Random.Shared.Next(0, JitterMillisecondsMax + 1);
                var jitter = TimeSpan.FromMilliseconds(jitterMs);
                var candidate = final + jitter;
                if (candidate.TotalSeconds > maxDelay)
                {
                    // Keeps absolute upper limit
                    candidate = TimeSpan.FromSeconds(maxDelay);
                }
                final = candidate;
            }
            return final;
        }

        /// <summary>
        /// Enables additional jitter (randomness) to backoff to reduce thundering herd (default: true)
        /// </summary>
        public bool EnableJitter { get; set; } = true;

        /// <summary>
        /// Maximum jitter in milliseconds added to the calculated delay (default: 250ms)
        /// </summary>
        public int JitterMillisecondsMax { get; set; } = 250;

        /// <summary>
        /// Enables metrics emission (System.Diagnostics.Metrics) for observability (default: true)
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
    }

    /// <summary>
    /// Data sent in each retry event
    /// </summary>
    public readonly record struct RetryEvent(int Attempt, TimeSpan PlannedDelay, TimeSpan? OverrideDelay, int? StatusCode, bool FromCircuitBreaker);

    /// <summary>
    /// Notified circuit breaker states
    /// </summary>
    public enum CircuitBreakerState
    {
        Open,
        HalfOpen,
        Closed
    }

    /// <summary>
    /// Event emitted by the circuit breaker
    /// </summary>
    public readonly record struct CircuitBreakerEvent(CircuitBreakerState State, string Reason, int Failures, TimeSpan Duration);
}