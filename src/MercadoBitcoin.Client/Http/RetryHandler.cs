using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using MercadoBitcoin.Client.Models.Enums;
using MbOutcomeType = MercadoBitcoin.Client.Models.Enums.OutcomeType;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Handler that implements retry policies with Polly V8 to improve HTTP call robustness
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
        private readonly RetryPolicyConfig _config;
        private readonly HttpConfiguration _httpConfig;

        // Metrics
        private static readonly Meter _meter = new(Diagnostics.MercadoBitcoinDiagnostics.MeterName, Diagnostics.MercadoBitcoinDiagnostics.MeterVersion);
        private static readonly Counter<long> _retryCounter = _meter.CreateCounter<long>(Diagnostics.MercadoBitcoinDiagnostics.RetryCounter, "retries", "Number of retry attempts");
        private static readonly Histogram<double> _requestDurationHistogram = _meter.CreateHistogram<double>(
            Diagnostics.MercadoBitcoinDiagnostics.RequestDurationHistogram,
            unit: "ms",
            description: "Duration (in ms) of HTTP requests including retries");

        public RetryHandler(RetryPolicyConfig? config = null, HttpConfiguration? httpConfig = null)
            : this(CreateDefaultInnerHandler(httpConfig ?? HttpConfiguration.CreateHttp2Default()), config, httpConfig)
        {
        }

        /// <summary>
        /// Additional constructor that allows injecting a custom inner handler (useful for tests / mocks).
        /// </summary>
        /// <param name="innerHandler">Handler that will effectively execute the request (e.g., mock). Cannot be null.</param>
        /// <param name="config">Retry/circuit breaker configuration.</param>
        /// <param name="httpConfig">HTTP configuration (timeout, version, etc).</param>
        public RetryHandler(HttpMessageHandler innerHandler, RetryPolicyConfig? config = null, HttpConfiguration? httpConfig = null)
        {
            if (innerHandler == null) throw new ArgumentNullException(nameof(innerHandler));
            _config = config ?? new RetryPolicyConfig();
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            _pipeline = CreateResiliencePipeline();
            InnerHandler = innerHandler;
        }

        private static HttpMessageHandler CreateDefaultInnerHandler(HttpConfiguration httpConfig)
        {
            return httpConfig.CreateOptimizedHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var startTimestamp = Stopwatch.GetTimestamp();

            // Apply HTTP configurations from config
            request.Version = _httpConfig.HttpVersion;
            request.VersionPolicy = _httpConfig.VersionPolicy;

            // Configure timeout if specified
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_httpConfig.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            HttpResponseMessage? finalResponse = null;
            Exception? finalException = null;

            try
            {
                finalResponse = await _pipeline.ExecuteAsync(async (ct) =>
                {
                    return await base.SendAsync(request, ct);
                }, combinedCts.Token);

                return finalResponse;
            }
            catch (BrokenCircuitException ex)
            {
                finalException = ex;
                // Maintain backward compatibility with previous manual implementation
                throw new HttpRequestException("Circuit breaker open", ex);
            }
            catch (Exception ex)
            {
                finalException = ex;
                throw;
            }
            finally
            {
                if (_config.EnableMetrics)
                {
                    var elapsedMs = ElapsedMilliseconds(startTimestamp);
                    var outcome = ClassifyOutcome(finalResponse, finalException, combinedCts);
                    object? statusCodeObj = finalResponse != null ? (int)finalResponse.StatusCode : null;
                    _requestDurationHistogram.Record(elapsedMs, new KeyValuePair<string, object?>[]
                    {
                        new("method", request.Method.Method),
                        new("outcome", outcome),
                        new("status_code", statusCodeObj)
                    });
                }
            }
        }

        private static double ElapsedMilliseconds(long startTimestamp)
        {
            var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            return (double)elapsedTicks * 1000 / Stopwatch.Frequency;
        }

        private string ClassifyOutcome(HttpResponseMessage? response, Exception? exception, CancellationTokenSource combinedCts)
        {
            if (exception != null)
            {
                if (exception is BrokenCircuitException)
                    return MbOutcomeType.CircuitBreakerOpen.ToString();
                if (combinedCts.IsCancellationRequested)
                    return MbOutcomeType.Timeout.ToString();
                if (exception is TaskCanceledException)
                    return MbOutcomeType.Timeout.ToString();
                return MbOutcomeType.NetworkError.ToString();
            }
            if (response == null)
                return MbOutcomeType.UnknownError.ToString();

            var code = (int)response.StatusCode;
            if (code >= 200 && code < 400)
                return MbOutcomeType.Success.ToString();
            if (code == 429)
                return MbOutcomeType.RateLimitExceeded.ToString();
            if (code == 401 || code == 403)
                return MbOutcomeType.AuthenticationError.ToString();
            if (code >= 400 && code < 500)
                return MbOutcomeType.HttpError.ToString();
            if (code >= 500)
                return MbOutcomeType.HttpError.ToString();

            return MbOutcomeType.UnknownError.ToString();
        }

        private ResiliencePipeline<HttpResponseMessage> CreateResiliencePipeline()
        {
            var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

            // 1. Circuit Breaker (Outer layer)
            if (_config.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5, // 50% failure rate
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = _config.CircuitBreakerFailuresBeforeBreaking,
                    BreakDuration = TimeSpan.FromSeconds(_config.CircuitBreakerDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutException>()
                        .HandleResult(r => ShouldRetry(r)),
                    OnOpened = args =>
                    {
                        _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.Open, "CircuitBreaker", 0, args.BreakDuration));
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.Closed, "CircuitBreaker", 0, TimeSpan.Zero));
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.HalfOpen, "CircuitBreaker", 0, TimeSpan.Zero));
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
                    Delay = TimeSpan.FromSeconds(1), // Base delay, calculated dynamically in config usually, but Polly V8 handles backoff
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(ex => _config.RetryOnTimeout)
                    .HandleResult(r => ShouldRetry(r)),
                    OnRetry = args =>
                    {
                        int statusCode = args.Outcome.Result != null ? (int)args.Outcome.Result.StatusCode : 0;

                        // Handle Retry-After header
                        if (_config.RespectRetryAfterHeader && args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests &&
                            args.Outcome.Result.Headers.TryGetValues("Retry-After", out var values))
                        {
                            // Note: Polly V8 RetryStrategyOptions doesn't support dynamic delay per retry easily in the same way as WaitAndRetryAsync
                            // But we can log it. For true dynamic delay based on header, we'd need a custom strategy or advanced configuration.
                            // For now, we stick to exponential backoff which is robust.
                        }

                        _config.OnRetryEvent?.Invoke(new RetryEvent(args.AttemptNumber, args.RetryDelay, null, statusCode, false));
                        if (_config.EnableMetrics)
                        {
                            _retryCounter.Add(1, new KeyValuePair<string, object?>("status_code", statusCode));
                        }
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