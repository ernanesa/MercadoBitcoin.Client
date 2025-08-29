using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;
using System.Diagnostics.Metrics; // added
using System.Diagnostics; // stopwatch para latência

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Handler que implementa retry policies com Polly para melhorar a robustez das chamadas HTTP
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;
        // Estado simples de circuit breaker manual
        private int _consecutiveFailures;
        private DateTime _circuitOpenedUtc = DateTime.MinValue;
        private bool _halfOpenProbe;
        private readonly RetryPolicyConfig _config;
        private readonly HttpConfiguration _httpConfig;
        // Métricas
        private static readonly Meter _meter = new("MercadoBitcoin.Client", "2.1.0");
        private static readonly Counter<long> _retryCounter = _meter.CreateCounter<long>("mb_client_http_retries", "retries", "Número de tentativas de retry");
        private static readonly Counter<long> _circuitOpenCounter = _meter.CreateCounter<long>("mb_client_circuit_opened", description: "Quantidade de vezes que o circuito abriu");
        private static readonly Counter<long> _circuitHalfOpenCounter = _meter.CreateCounter<long>("mb_client_circuit_half_open", description: "Quantidade de vezes que entrou em half-open");
        private static readonly Counter<long> _circuitClosedCounter = _meter.CreateCounter<long>("mb_client_circuit_closed", description: "Quantidade de vezes que o circuito fechou após sucesso");
        private static readonly Histogram<double> _requestDurationHistogram = _meter.CreateHistogram<double>(
            "mb_client_http_request_duration",
            unit: "ms",
            description: "Duração (em ms) das requisições HTTP incluindo retries");

        public RetryHandler(RetryPolicyConfig? config = null, HttpConfiguration? httpConfig = null)
            : this(CreateDefaultInnerHandler(httpConfig ?? HttpConfiguration.CreateHttp2Default()), config, httpConfig)
        {
        }

        /// <summary>
        /// Construtor adicional que permite injetar um handler interno customizado (útil para testes / mocks).
        /// </summary>
        /// <param name="innerHandler">Handler que efetivamente executará a requisição (ex: mock). Não pode ser null.</param>
        /// <param name="config">Configuração de retry/circuit breaker.</param>
        /// <param name="httpConfig">Configuração HTTP (timeout, version, etc).</param>
        public RetryHandler(HttpMessageHandler innerHandler, RetryPolicyConfig? config = null, HttpConfiguration? httpConfig = null)
        {
            if (innerHandler == null) throw new ArgumentNullException(nameof(innerHandler));
            _config = config ?? new RetryPolicyConfig();
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            _resiliencePolicy = CreateResiliencePolicy();
            InnerHandler = innerHandler;
        }

        private static HttpMessageHandler CreateDefaultInnerHandler(HttpConfiguration httpConfig)
        {
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = httpConfig.MaxConnectionsPerServer
            };
            if (httpConfig.EnableCompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            return handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            // Aplicar configurações HTTP da configuração
            request.Version = _httpConfig.HttpVersion;
            request.VersionPolicy = _httpConfig.VersionPolicy;

            // Configurar timeout se especificado
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_httpConfig.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Circuit Breaker manual: verifica se circuito está aberto
            if (_config.EnableCircuitBreaker && IsCircuitOpen())
            {
                var elapsedMsFastFail = ElapsedMilliseconds(startTimestamp);
                if (_config.EnableMetrics)
                {
                    _requestDurationHistogram.Record(elapsedMsFastFail, new KeyValuePair<string, object?>[]
                    {
                        new("method", request.Method.Method),
                        new("outcome", "circuit_open_fast_fail"),
                        new("status_code", null)
                    });
                }
                throw new HttpRequestException("Circuit breaker open - fast fail");
            }

            HttpResponseMessage? finalResponse = null;
            Exception? finalException = null;
            try
            {
                finalResponse = await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var response = await base.SendAsync(request, combinedCts.Token);
                    return response;
                });

                if (_config.EnableCircuitBreaker)
                {
                    if (ShouldRetry(finalResponse))
                    {
                        RegisterFailure(new HttpRequestException($"HTTP {(int)finalResponse.StatusCode}"));
                    }
                    else
                    {
                        RegisterSuccess();
                    }
                }

                return finalResponse;
            }
            catch (Exception ex)
            {
                finalException = ex;
                if (_config.EnableCircuitBreaker)
                {
                    RegisterFailure(ex);
                }
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
                if (exception is HttpRequestException hre && hre.Message.Contains("Circuit breaker open"))
                    return "circuit_open_fast_fail";
                if (combinedCts.IsCancellationRequested)
                    return "canceled";
                if (exception is TaskCanceledException)
                    return "timeout_or_canceled";
                return "exception";
            }
            if (response == null)
                return "unknown";

            var code = (int)response.StatusCode;
            if (code >= 200 && code < 400)
                return "success";
            if (ShouldRetry(response))
                return "transient_exhausted"; // chegamos aqui mesmo após retries exaustos
            if (code >= 400 && code < 500)
                return "client_error";
            if (code >= 500)
                return "server_error";
            return "other";
        }

        private IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy()
        {
            return Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>(ex => _config.RetryOnTimeout)
                .OrResult<HttpResponseMessage>(r => ShouldRetry(r))
                .WaitAndRetryAsync(
                    retryCount: _config.MaxRetryAttempts,
                    sleepDurationProvider: attempt => _config.CalculateDelay(attempt),
                    onRetryAsync: async (outcome, timespan, attempt, context) =>
                    {
                        TimeSpan? overrideDelay = null;
                        int? statusCode = null;
                        if (outcome.Result != null)
                        {
                            statusCode = (int)outcome.Result.StatusCode;
                            if (_config.RespectRetryAfterHeader && outcome.Result.StatusCode == HttpStatusCode.TooManyRequests &&
                                outcome.Result.Headers.TryGetValues("Retry-After", out var values))
                            {
                                var raw = System.Linq.Enumerable.FirstOrDefault(values);
                                if (int.TryParse(raw, out var seconds) && seconds >= 0)
                                {
                                    overrideDelay = TimeSpan.FromSeconds(seconds);
                                    await Task.Delay(overrideDelay.Value);
                                    _config.OnRetryEvent?.Invoke(new RetryEvent(attempt, timespan, overrideDelay, statusCode, false));
                                    if (_config.EnableMetrics)
                                    {
                                        _retryCounter.Add(1, new KeyValuePair<string, object?>("status_code", statusCode));
                                    }
                                    return; // evita delay duplo
                                }
                            }
                        }
                        _config.OnRetryEvent?.Invoke(new RetryEvent(attempt, timespan, overrideDelay, statusCode, false));
                        if (_config.EnableMetrics)
                        {
                            _retryCounter.Add(1, new KeyValuePair<string, object?>("status_code", statusCode));
                        }
                    });
        }

        private bool IsCircuitOpen()
        {
            if (_consecutiveFailures < _config.CircuitBreakerFailuresBeforeBreaking)
                return false;

            var openDuration = TimeSpan.FromSeconds(_config.CircuitBreakerDurationSeconds);
            var elapsed = DateTime.UtcNow - _circuitOpenedUtc;
            if (elapsed >= openDuration)
            {
                // Half-open probe: permitir uma tentativa
                if (!_halfOpenProbe)
                {
                    _halfOpenProbe = true;
                    _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.HalfOpen, "Probe", _consecutiveFailures, openDuration - elapsed));
                    if (_config.EnableMetrics)
                    {
                        _circuitHalfOpenCounter.Add(1);
                    }
                    return false; // deixa passar
                }
                return false;
            }
            return true;
        }

        private void RegisterFailure(Exception ex)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures == _config.CircuitBreakerFailuresBeforeBreaking)
            {
                _circuitOpenedUtc = DateTime.UtcNow;
                _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.Open, ex.GetType().Name, _consecutiveFailures, TimeSpan.FromSeconds(_config.CircuitBreakerDurationSeconds)));
                if (_config.EnableMetrics)
                {
                    _circuitOpenCounter.Add(1);
                }
            }
        }

        private void RegisterSuccess()
        {
            if (_consecutiveFailures > 0)
            {
                _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.Closed, "Success", _consecutiveFailures, TimeSpan.Zero));
                if (_config.EnableMetrics)
                {
                    _circuitClosedCounter.Add(1);
                }
            }
            _consecutiveFailures = 0;
            _halfOpenProbe = false;
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