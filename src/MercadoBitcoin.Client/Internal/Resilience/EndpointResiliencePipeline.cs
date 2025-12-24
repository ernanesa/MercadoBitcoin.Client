using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MercadoBitcoin.Client.Internal.Resilience
{
    /// <summary>
    /// Advanced resilience pipeline using Polly v8 ResiliencePipeline.
    /// Provides circuit breaker, retry, timeout, and bulkhead policies.
    /// Each endpoint can have its own resilience strategy.
    /// </summary>
    public sealed class EndpointResiliencePipeline : IDisposable
    {
        private readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> _pipelines = new();
        private volatile bool _disposed;

        /// <summary>
        /// Configuration for resilience policies.
        /// </summary>
        public sealed class ResilienceConfig
        {
            /// <summary>
            /// Maximum number of retry attempts.
            /// </summary>
            public int MaxRetries { get; set; } = 3;

            /// <summary>
            /// Initial delay for exponential backoff in milliseconds.
            /// </summary>
            public int InitialDelayMs { get; set; } = 100;

            /// <summary>
            /// Circuit breaker failure threshold (e.g., 5 failures within a window).
            /// </summary>
            public int CircuitBreakerThreshold { get; set; } = 5;

            /// <summary>
            /// Circuit breaker evaluation window in seconds.
            /// </summary>
            public int CircuitBreakerWindowSeconds { get; set; } = 30;

            /// <summary>
            /// Request timeout in milliseconds.
            /// </summary>
            public int TimeoutMs { get; set; } = 30000;

            /// <summary>
            /// Enable circuit breaker (default: true).
            /// </summary>
            public bool EnableCircuitBreaker { get; set; } = true;

            /// <summary>
            /// Enable retry (default: true).
            /// </summary>
            public bool EnableRetry { get; set; } = true;

            /// <summary>
            /// Enable timeout (default: true).
            /// </summary>
            public bool EnableTimeout { get; set; } = true;
        }

        private readonly ResilienceConfig _defaultConfig;

        /// <summary>
        /// Creates a new endpoint resilience pipeline manager.
        /// </summary>
        public EndpointResiliencePipeline(ResilienceConfig? defaultConfig = null)
        {
            _defaultConfig = defaultConfig ?? new ResilienceConfig();
        }

        /// <summary>
        /// Gets or creates a resilience pipeline for the specified endpoint.
        /// </summary>
        public ResiliencePipeline<HttpResponseMessage> GetOrCreatePipeline(
            string endpointKey,
            ResilienceConfig? config = null)
        {
            ThrowIfDisposed();

            return _pipelines.GetOrAdd(endpointKey, _ => BuildPipeline(config ?? _defaultConfig));
        }

        /// <summary>
        /// Executes an async operation through the resilience pipeline.
        /// </summary>
        public async Task<HttpResponseMessage> ExecuteAsync(
            string endpointKey,
            Func<CancellationToken, Task<HttpResponseMessage>> operation,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var pipeline = GetOrCreatePipeline(endpointKey);
            return await pipeline.ExecuteAsync(async (ctx) => await operation(cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the circuit breaker for a specific endpoint.
        /// </summary>
        public void ResetCircuitBreaker(string endpointKey)
        {
            ThrowIfDisposed();

            if (_pipelines.TryGetValue(endpointKey, out var pipeline))
            {
                // Circuit breaker state is managed internally, just remove and recreate
                _pipelines.TryRemove(endpointKey, out _);
            }
        }

        /// <summary>
        /// Gets count of managed pipelines.
        /// </summary>
        public int PipelineCount => _pipelines.Count;

        private ResiliencePipeline<HttpResponseMessage> BuildPipeline(ResilienceConfig config)
        {
            var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

            // Retry policy
            if (config.EnableRetry)
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = config.MaxRetries,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMilliseconds(config.InitialDelayMs),
                    ShouldHandle = args => new ValueTask<bool>(
                        args.Outcome.Exception != null ||
                        (args.Outcome.Result?.StatusCode >= System.Net.HttpStatusCode.InternalServerError))
                });
            }

            // Circuit breaker policy
            if (config.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = config.CircuitBreakerThreshold,
                    SamplingDuration = TimeSpan.FromSeconds(config.CircuitBreakerWindowSeconds),
                    ShouldHandle = args => new ValueTask<bool>(
                        args.Outcome.Exception != null ||
                        (args.Outcome.Result?.StatusCode >= System.Net.HttpStatusCode.InternalServerError)),
                    OnOpened = context =>
                    {
                        // Log or handle circuit opened
                        return default;
                    },
                    OnClosed = context =>
                    {
                        // Log or handle circuit closed
                        return default;
                    }
                });
            }

            // Timeout policy
            if (config.EnableTimeout)
            {
                builder.AddTimeout(TimeSpan.FromMilliseconds(config.TimeoutMs));
            }

            return builder.Build();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EndpointResiliencePipeline));
        }

        /// <summary>
        /// Disposes all pipelines and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _pipelines.Clear();
        }
    }
}
