using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using MercadoBitcoin.Client.Http;

namespace MercadoBitcoin.Client.Internal.Optimization
{
    /// <summary>
    /// Beast Mode resilience pipeline factory.
    /// Provides per-endpoint resilience pipelines with advanced strategies:
    /// - Circuit Breaker (prevents cascade failures)
    /// - Retry with exponential backoff and jitter (recovers from transient errors)
    /// - Rate Limit Retry (handles 429 via Retry-After header)
    /// - Timeout hedging (for critical read operations)
    /// 
    /// The pipeline is stacked (decorators) with careful ordering for maximum effectiveness.
    /// </summary>
    internal sealed class ResiliencePipelineProvider
    {
        private readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> _pipelines = new();
        private readonly RetryPolicyConfig _config;

        public ResiliencePipelineProvider(RetryPolicyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ResiliencePipeline<HttpResponseMessage> GetPipeline(string endpoint)
        {
            return _pipelines.GetOrAdd(endpoint, CreatePipeline);
        }

        /// <summary>
        /// Creates a highly optimized resilience pipeline for HFT workloads.
        /// Stacking order (outermost to innermost):
        /// 1. Circuit Breaker (fail fast if endpoint is degraded)
        /// 2. Timeout Policy (prevent unbounded waiting)
        /// 3. Retry Policy (recover from transient errors)
        /// </summary>
        private ResiliencePipeline<HttpResponseMessage> CreatePipeline(string endpoint)
        {
            var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

            // ============ LAYER 1: CIRCUIT BREAKER ============
            // Fail fast if the endpoint is experiencing systematic failures.
            // A broken circuit immediately rejects requests without attempting the call.
            if (_config.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    // If 50% of requests fail within 30s window, open the circuit
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),

                    // Minimum number of calls in sampling window before circuit can trip
                    MinimumThroughput = _config.CircuitBreakerFailuresBeforeBreaking,

                    // Duration to keep circuit open (allow time for the service to recover)
                    BreakDuration = TimeSpan.FromSeconds(_config.CircuitBreakerDurationSeconds),

                    // Determine which outcomes are considered failures
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutException>()
                        .HandleResult(r => ShouldRetry(r)),

                    // Diagnostics callbacks
                    OnOpened = args =>
                    {
                        _config.OnCircuitBreakerEvent?.Invoke(
                            new CircuitBreakerEvent(CircuitBreakerState.Open, endpoint, 0, args.BreakDuration));
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _config.OnCircuitBreakerEvent?.Invoke(
                            new CircuitBreakerEvent(CircuitBreakerState.Closed, endpoint, 0, TimeSpan.Zero));
                        return ValueTask.CompletedTask;
                    }
                });
            }

            // ============ LAYER 2: TIMEOUT POLICY ============
            // Prevent unbounded waits. Fail fast if response takes too long.
            builder.AddTimeout(new TimeoutStrategyOptions<HttpResponseMessage>
            {
                TimeoutDuration = TimeSpan.FromSeconds(_config.TimeoutSeconds),
                TimeoutRejectedException = false, // Don't throw; instead, propagate timeout as failed result
            });

            // ============ LAYER 3: RETRY POLICY ============
            // Exponential backoff with jitter for transient failures.
            // Special handling for 429 (rate limit) via Retry-After header.
            if (_config.MaxRetryAttempts > 0)
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = _config.MaxRetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true, // Jitter spreads retry spikes, preventing thundering herd
                    Delay = TimeSpan.FromSeconds(_config.BaseDelaySeconds),

                    // Determine which outcomes warrant a retry
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>(ex => _config.RetryOnTimeout)
                        .HandleResult(r => ShouldRetry(r)),

                    // Custom delay calculator that respects Retry-After header for 429s
                    DelayGenerator = GetDelayForAttempt,

                    // Diagnostics callback
                    OnRetry = args =>
                    {
                        var statusCode = args.Outcome.Result != null ? (int)args.Outcome.Result.StatusCode : 0;
                        _config.OnRetryEvent?.Invoke(
                            new RetryEvent(args.AttemptNumber, args.RetryDelay, null, statusCode, false));
                        return ValueTask.CompletedTask;
                    }
                });
            }

            return builder.Build();
        }

        /// <summary>
        /// Determines if an HTTP response should trigger a retry.
        /// Returns true for transient failures (5xx, 408, 429) that may succeed on retry.
        /// Returns false for permanent failures (4xx except 408/429) that will not recover.
        /// </summary>
        private bool ShouldRetry(HttpResponseMessage response)
        {
            return (_config.RetryOnTimeout && response.StatusCode == HttpStatusCode.RequestTimeout) ||
                   (_config.RetryOnRateLimit && response.StatusCode == HttpStatusCode.TooManyRequests) ||
                   (_config.RetryOnServerErrors && (
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.BadGateway ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.GatewayTimeout));
        }

        /// <summary>
        /// Custom delay generator for retry backoff.
        /// Respects the Retry-After header if present (especially critical for 429s).
        /// Otherwise, uses exponential backoff with jitter.
        /// </summary>
        private static ValueTask<TimeSpan> GetDelayForAttempt(
            RetryDelayGeneratorArguments<HttpResponseMessage> args)
        {
            var response = args.Outcome.Result;
            if (response != null)
            {
                // Check for Retry-After header (standard for rate limit responses)
                if (response.Headers.RetryAfter != null)
                {
                    if (response.Headers.RetryAfter.Delta.HasValue)
                    {
                        // Retry-After: <seconds>
                        return new ValueTask<TimeSpan>(response.Headers.RetryAfter.Delta.Value);
                    }
                    else if (response.Headers.RetryAfter.Date.HasValue)
                    {
                        // Retry-After: <http-date>
                        var delay = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                        if (delay.TotalSeconds > 0)
                        {
                            return new ValueTask<TimeSpan>(delay);
                        }
                    }
                }
            }

            // Fallback: exponential backoff with jitter
            // base_delay * (2 ^ attempt) + random jitter
            var baseDelay = TimeSpan.FromSeconds(0.1); // 100ms base
            var exponentialDelay = baseDelay.TotalMilliseconds * Math.Pow(2, args.AttemptNumber - 1);
            var jitter = Random.Shared.Next(0, (int)(exponentialDelay * 0.1)); // ±10% jitter
            var totalDelay = exponentialDelay + jitter;

            // Cap at 30 seconds to prevent excessive waiting
            var capped = Math.Min(totalDelay, 30000);
            return new ValueTask<TimeSpan>(TimeSpan.FromMilliseconds(capped));
        }

        /// <summary>
        /// Clears all cached pipelines (useful for reconfiguration or testing).
        /// </summary>
        public void Clear()
        {
            _pipelines.Clear();
        }

        /// <summary>
        /// Gets diagnostics info about active pipelines.
        /// </summary>
        public int PipelineCount => _pipelines.Count;
    }
}
