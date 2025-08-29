using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;

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

        public RetryHandler(RetryPolicyConfig? config = null, HttpConfiguration? httpConfig = null)
        {
            _config = config ?? new RetryPolicyConfig();
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            _resiliencePolicy = CreateResiliencePolicy();

            // Configurar HttpClientHandler com as configurações HTTP
            var handler = new HttpClientHandler()
            {
                MaxConnectionsPerServer = _httpConfig.MaxConnectionsPerServer
            };

            // Configurar compressão se habilitada
            if (_httpConfig.EnableCompression)
            {
                handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
            }

            InnerHandler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Aplicar configurações HTTP da configuração
            request.Version = _httpConfig.HttpVersion;
            request.VersionPolicy = _httpConfig.VersionPolicy;

            // Configurar timeout se especificado
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_httpConfig.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Circuit Breaker manual: verifica se circuito está aberto
            if (_config.EnableCircuitBreaker && IsCircuitOpen())
            {
                throw new HttpRequestException("Circuit breaker open - fast fail");
            }

            try
            {
                var result = await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var response = await base.SendAsync(request, combinedCts.Token);
                    return response;
                });

                if (_config.EnableCircuitBreaker)
                {
                    if (ShouldRetry(result))
                    {
                        RegisterFailure(new HttpRequestException($"HTTP {(int)result.StatusCode}"));
                    }
                    else
                    {
                        RegisterSuccess();
                    }
                }

                return result;
            }
            catch (Exception ex) when (_config.EnableCircuitBreaker)
            {
                RegisterFailure(ex);
                throw;
            }
        }

        private IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy()
        {
            var retryPolicy = Policy
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
                                    return; // evita delay duplo
                                }
                            }
                        }
                        _config.OnRetryEvent?.Invoke(new RetryEvent(attempt, timespan, overrideDelay, statusCode, false));
                    });

            return retryPolicy; // circuit breaker manual aplicado em SendAsync
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
            }
        }

        private void RegisterSuccess()
        {
            if (_consecutiveFailures > 0)
            {
                _config.OnCircuitBreakerEvent?.Invoke(new CircuitBreakerEvent(CircuitBreakerState.Closed, "Success", _consecutiveFailures, TimeSpan.Zero));
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