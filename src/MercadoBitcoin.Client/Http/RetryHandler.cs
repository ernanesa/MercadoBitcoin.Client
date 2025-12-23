using System.Net;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using MercadoBitcoin.Client.Models.Enums;
using MercadoBitcoin.Client.Configuration;
using Microsoft.Extensions.Options;
using MbOutcomeType = MercadoBitcoin.Client.Models.Enums.OutcomeType;
using MercadoBitcoin.Client.Internal.Optimization;
using Polly.CircuitBreaker;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Handler that implements retry policies with Polly V8 to improve HTTP call robustness
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        private readonly ResiliencePipelineProvider _pipelineProvider;
        private readonly RetryPolicyConfig _config;
        private readonly HttpConfiguration _httpConfig;

        // Metrics
        private static readonly Meter _meter = new(Diagnostics.MercadoBitcoinDiagnostics.MeterName, Diagnostics.MercadoBitcoinDiagnostics.MeterVersion);
        private static readonly Counter<long> _retryCounter = _meter.CreateCounter<long>(Diagnostics.MercadoBitcoinDiagnostics.RetryCounter, "retries", "Number of retry attempts");
        private static readonly Histogram<double> _requestDurationHistogram = _meter.CreateHistogram<double>(
            Diagnostics.MercadoBitcoinDiagnostics.RequestDurationHistogram,
            unit: "ms",
            description: "Duration (in ms) of HTTP requests including retries");

        public RetryHandler()
            : this(new RetryPolicyConfig(), HttpConfiguration.CreateHttp2Default())
        {
        }

        public RetryHandler(RetryPolicyConfig config, HttpConfiguration httpConfig)
            : this(CreateDefaultInnerHandler(httpConfig), config, httpConfig)
        {
        }

        /// <summary>
        /// Additional constructor that allows injecting a custom inner handler.
        /// </summary>
        /// <param name="innerHandler">Handler that will effectively execute the request. Cannot be null.</param>
        /// <param name="config">Retry/circuit breaker configuration.</param>
        /// <param name="httpConfig">HTTP configuration (timeout, version, etc).</param>
        public RetryHandler(HttpMessageHandler innerHandler, RetryPolicyConfig? config = null, HttpConfiguration? httpConfig = null)
        {
            if (innerHandler == null) throw new ArgumentNullException(nameof(innerHandler));
            _config = config ?? new RetryPolicyConfig();
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            _pipelineProvider = new ResiliencePipelineProvider(_config);
            InnerHandler = innerHandler;
        }

        /// <summary>
        /// Constructor for use with IHttpClientFactory (DI) where InnerHandler is set by the factory.
        /// </summary>
        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
        public RetryHandler(IOptionsSnapshot<MercadoBitcoinClientOptions> options)
        {
            if (options == null || options.Value == null) throw new ArgumentNullException(nameof(options));

            _config = options.Value.RetryPolicyConfig ?? new RetryPolicyConfig();
            _httpConfig = options.Value.HttpConfiguration ?? HttpConfiguration.CreateHttp2Default();
            _pipelineProvider = new ResiliencePipelineProvider(_config);
            // InnerHandler is NOT set here, it will be set by the pipeline builder
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

            // Identify endpoint for per-endpoint circuit breaker
            var endpoint = request.RequestUri?.AbsolutePath ?? "unknown";
            var pipeline = _pipelineProvider.GetPipeline(endpoint);

            try
            {
                finalResponse = await pipeline.ExecuteAsync(async (ct) =>
                {
                    return await base.SendAsync(request, ct);
                }, combinedCts.Token);

                return finalResponse;
            }
            catch (BrokenCircuitException ex)
            {
                finalException = ex;
                // Maintain backward compatibility with previous manual implementation
                throw new HttpRequestException($"Circuit breaker open for {endpoint}", ex);
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
                        new("endpoint", endpoint),
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
    }
}