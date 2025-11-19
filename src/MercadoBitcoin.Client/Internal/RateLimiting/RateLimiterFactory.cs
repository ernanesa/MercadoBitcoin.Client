using System;
using System.Threading.RateLimiting;

namespace MercadoBitcoin.Client.Internal.RateLimiting;

internal static class RateLimiterFactory
{
    public static TokenBucketRateLimiter CreateTokenBucket(int requestsPerSecond)
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = Math.Max(10, requestsPerSecond / 10),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = requestsPerSecond,
            AutoReplenishment = true
        });
    }
}
