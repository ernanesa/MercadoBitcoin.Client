using System;
using System.Net;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace MercadoBitcoin.Client.Policies
{
    public static class MercadoBitcoinPolicy
    {
        /// <summary>
        /// Returns a Polly retry policy that handles transient HTTP errors and 429 Too Many Requests.
        /// It respects the Retry-After header when present and falls back to exponential backoff.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // 5xx, 408
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests) // 429
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: (retryAttempt, response, context) =>
                    {
                        // Try to read Retry-After header (seconds)
                        if (response?.Result?.Headers?.TryGetValues("Retry-After", out var values) == true)
                        {
                            if (int.TryParse(values.FirstOrDefault(), out int seconds))
                            {
                                // Add jitter up to 100ms to avoid thundering herd
                                var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 100));
                                return TimeSpan.FromSeconds(seconds) + jitter;
                            }
                        }
                        // Exponential backoff: 1s, 2s, 4s
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                    },
                    onRetryAsync: async (outcome, timespan, retryNumber, context) =>
                    {
                        // Optional: log retry (no I/O here to keep side‑effects low)
                        await System.Threading.Tasks.Task.CompletedTask;
                    });
        }
    }
}
