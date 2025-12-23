using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using MercadoBitcoin.Client.Http;

namespace MercadoBitcoin.Client.Internal.Optimization
{
    /// <summary>
    /// Provides per-endpoint resilience pipelines.
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

        private ResiliencePipeline<HttpResponseMessage> CreatePipeline(string endpoint)
        {
            var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

            // 1. Circuit Breaker (Outer layer)
            if (_config.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = _config.CircuitBreakerFailuresBeforeBreaking,
                    BreakDuration = TimeSpan.FromSeconds(_config.CircuitBreakerDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutException>()
                        .HandleResult(r => ShouldRetry(r)),
                    OnOpened = args =>
                    {
                        _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.Open, endpoint, 0, args.BreakDuration));
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.Closed, endpoint, 0, TimeSpan.Zero));
                        return ValueTask.CompletedTask;
                    }
                });
            }

            // 2. Retry (Inner layer)
            if (_config.MaxRetryAttempts > 0)
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = _config.MaxRetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true, // Added Jitter as requested
                    Delay = TimeSpan.FromSeconds(_config.BaseDelaySeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>(ex => _config.RetryOnTimeout)
                        .HandleResult(r => ShouldRetry(r)),
                    OnRetry = args =>
                    {
                        int statusCode = args.Outcome.Result != null ? (int)args.Outcome.Result.StatusCode : 0;
                        _config.OnRetryEvent?.Invoke(new RetryEvent(args.AttemptNumber, args.RetryDelay, null, statusCode, false));
                        return ValueTask.CompletedTask;
                    }
                });
            }

            return builder.Build();
        }

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
    }
}
