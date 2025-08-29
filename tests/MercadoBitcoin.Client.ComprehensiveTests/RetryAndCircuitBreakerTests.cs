using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Http;
using Xunit;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    /// <summary>
    /// Testes direcionados ao comportamento de retry + circuit breaker.
    /// Usa um fake handler para simular respostas.
    /// </summary>
    public class RetryAndCircuitBreakerTests
    {
        private class SequenceHandler : HttpMessageHandler
        {
            private readonly Func<int, HttpResponseMessage> _responseFactory;
            private int _count;
            public int Calls => _count;
            public SequenceHandler(Func<int, HttpResponseMessage> responseFactory) => _responseFactory = responseFactory;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var c = Interlocked.Increment(ref _count);
                return Task.FromResult(_responseFactory(c));
            }
        }

        [Fact]
        public async Task RetryHandler_Retries_On_Transient_Errors()
        {
            var cfg = new RetryPolicyConfig
            {
                MaxRetryAttempts = 2,
                RetryOnServerErrors = true,
                RetryOnTimeout = true,
                RetryOnRateLimit = true,
                EnableCircuitBreaker = false,
                BaseDelaySeconds = 0.01,
                BackoffMultiplier = 1,
                MaxDelaySeconds = 0.05,
                EnableJitter = false
            };

            // 1ª e 2ª respostas 500 -> depois 200
            var handler = new SequenceHandler(i =>
            {
                if (i < 3) return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("err") };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
            });

            var retry = new RetryHandler(handler, cfg);
            var client = new HttpClient(retry) { BaseAddress = new Uri("https://test.local") };

            var resp = await client.GetAsync("/ping");
            var body = await resp.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            // 3 chamadas: original + 2 retries
            Assert.Equal(3, handler.Calls);
            Assert.Equal("ok", body);
        }

        [Fact]
        public async Task RetryHandler_NoRetry_When_MaxRetryAttempts_Zero()
        {
            var cfg = new RetryPolicyConfig
            {
                MaxRetryAttempts = 0,
                RetryOnServerErrors = true,
                EnableCircuitBreaker = false,
                EnableJitter = false
            };

            var handler = new SequenceHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var retry = new RetryHandler(handler, cfg);
            var client = new HttpClient(retry) { BaseAddress = new Uri("https://test.local") };

            var resp = await client.GetAsync("/fail-once");
            Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            Assert.Equal(1, handler.Calls); // sem retries
        }

        [Fact]
        public async Task CircuitBreaker_Opens_After_Consecutive_Failures()
        {
            var opened = false;
            var cfg = new RetryPolicyConfig
            {
                MaxRetryAttempts = 0, // sem retries para isolar breaker
                EnableCircuitBreaker = true,
                CircuitBreakerFailuresBeforeBreaking = 3,
                CircuitBreakerDurationSeconds = 2, // curto para teste
                OnCircuitBreakerEvent = e =>
                {
                    if (e.State == CircuitBreakerState.Open) opened = true;
                }
            };

            var handler = new SequenceHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var retry = new RetryHandler(handler, cfg);
            var client = new HttpClient(retry) { BaseAddress = new Uri("https://test.local") };

            // 3 falhas consecutivas para abrir
            for (int i = 0; i < 3; i++)
            {
                var resp = await client.GetAsync("/fail");
                Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            }
            Assert.True(opened, "Circuit breaker deveria estar aberto após 3 falhas");

            // Próxima chamada deve falhar rápido (HttpRequestException) porque circuito aberto
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/fast-fail"));
        }

        [Fact]
        public async Task CircuitBreaker_HalfOpen_Allows_Single_Probe_Then_Closes_On_Success()
        {
            var stateTransitions = 0;
            var cfg = new RetryPolicyConfig
            {
                MaxRetryAttempts = 0,
                EnableCircuitBreaker = true,
                CircuitBreakerFailuresBeforeBreaking = 2,
                CircuitBreakerDurationSeconds = 1,
                OnCircuitBreakerEvent = _ => Interlocked.Increment(ref stateTransitions)
            };

            // Falha nas duas primeiras, sucesso depois
            var handler = new SequenceHandler(i =>
            {
                if (i <= 2) return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("recover") };
            });

            var retry = new RetryHandler(handler, cfg);
            var client = new HttpClient(retry) { BaseAddress = new Uri("https://test.local") };

            // Dispara falhas para abrir
            for (int i = 0; i < 2; i++)
            {
                var resp = await client.GetAsync("/fail");
                Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            }

            // Agora circuito aberto -> fast fail
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/fast-fail"));

            // Espera período para meia-abertura
            await Task.Delay(TimeSpan.FromSeconds(cfg.CircuitBreakerDurationSeconds + 0.2));

            // Sonda (half-open) deve permitir passagem e fechar em sucesso
            var probe = await client.GetAsync("/recover");
            Assert.Equal(HttpStatusCode.OK, probe.StatusCode);

            // Nova requisição deve passar normalmente (circuito fechado)
            var second = await client.GetAsync("/after-close");
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        }
    }
}
