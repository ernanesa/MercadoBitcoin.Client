using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.Http;
using Xunit;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    /// <summary>
    /// Tests targeted at retry + circuit breaker behavior.
    /// Uses a fake handler to simulate responses.
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

            // 1st and 2nd responses 500 -> then 200
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
            // 3 calls: original + 2 retries
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
            Assert.Equal(1, handler.Calls); // no retries
        }

        [Fact]
        public async Task CircuitBreaker_Opens_After_Consecutive_Failures()
        {
            var opened = false;
            var cfg = new RetryPolicyConfig
            {
                MaxRetryAttempts = 0, // no retries to isolate breaker
                EnableCircuitBreaker = true,
                CircuitBreakerFailuresBeforeBreaking = 3,
                CircuitBreakerDurationSeconds = 2, // short for testing
                OnCircuitBreakerEvent = e =>
                {
                    if (e.State == CircuitBreakerState.Open) opened = true;
                }
            };

            var handler = new SequenceHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var retry = new RetryHandler(handler, cfg);
            var client = new HttpClient(retry) { BaseAddress = new Uri("https://test.local") };

            // 3 consecutive failures to open
            for (int i = 0; i < 3; i++)
            {
                var resp = await client.GetAsync("/fail");
                Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            }
            Assert.True(opened, "Circuit breaker should be open after 3 failures");

            // Next call should fail fast (HttpRequestException) because circuit is open
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

            // Fails on first two, success afterwards
            var handler = new SequenceHandler(i =>
            {
                if (i <= 2) return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("recover") };
            });

            var retry = new RetryHandler(handler, cfg);
            var client = new HttpClient(retry) { BaseAddress = new Uri("https://test.local") };

            // Trigger failures to open
            for (int i = 0; i < 2; i++)
            {
                var resp = await client.GetAsync("/fail");
                Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
            }

            // Now circuit open -> fast fail
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/fast-fail"));

            // Wait for half-open period
            await Task.Delay(TimeSpan.FromSeconds(cfg.CircuitBreakerDurationSeconds + 0.2));

            // Probe (half-open) should allow passage and close on success
            var probe = await client.GetAsync("/recover");
            Assert.Equal(HttpStatusCode.OK, probe.StatusCode);

            // New request should pass normally (circuit closed)
            var second = await client.GetAsync("/after-close");
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        }
    }
}
