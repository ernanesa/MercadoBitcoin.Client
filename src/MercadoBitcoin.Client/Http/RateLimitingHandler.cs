using System;
using System.Net.Http;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// A DelegatingHandler that applies client-side rate limiting using a Token Bucket / Fixed Window algorithm.
    /// This ensures we don't exceed the API limits proactively, avoiding 429 Too Many Requests errors.
    /// </summary>
    public class RateLimitingHandler : DelegatingHandler
    {
        private readonly RateLimiter _rateLimiter;

        public RateLimitingHandler()
        {
            // Configuration based on MB API v4 limits (adjust as needed for VIP levels)
            // Default: 60 requests per minute
            _rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
        }

        public RateLimitingHandler(RateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);

            if (lease.IsAcquired)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // If we couldn't acquire a permit (e.g. queue full), we throw a client-side rate limit exception
            // or we could wait. AcquireAsync waits if there is queue space.
            // If IsAcquired is false here, it means even the queue was full or rejected.
            throw new ClientSideRateLimitException($"Client-side rate limit exceeded. Queue limit reached.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rateLimiter.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class ClientSideRateLimitException : Exception
    {
        public ClientSideRateLimitException(string message) : base(message) { }
    }
}
