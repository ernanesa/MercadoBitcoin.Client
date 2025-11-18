# 10. RATE LIMITING (System.Threading.RateLimiting)

## 📋 Index

1. [Objective and Context](#objective-and-context)
2. [Issues with the Current Rate Limiter](#issues-with-the-current-rate-limiter)
3. [Target Strategy with System.Threading.RateLimiting](#target-strategy-with-systemthreadingratelimiting)
4. [TokenBucketRateLimiter – Base Configuration](#tokenbucketratelimiter--base-configuration)
5. [RateLimiterFactory and RateLimiterMetrics](#ratelimiterfactory-and-ratelimetermetrics)
6. [Integration with MercadoBitcoinClient](#integration-with-mercadobitcoinclient)
7. [Integration with Retry and Circuit Breaker](#integration-with-retry-and-circuit-breaker)
8. [Metrics and Observability](#metrics-and-observability)
9. [Load Tests and Validation](#load-tests-and-validation)
10. [Implementation Checklist](#implementation-checklist)

---

## 1. Objective and Context

Replace the custom `AsyncRateLimiter` with a rate limiter based on `System.Threading.RateLimiting`, focusing on:

- 🔄 **Precise control** of requests per second  
- 🧱 Seamless integration with the HTTP and Retry layers  
- 📊 Clear metrics (tokens consumed, queues, rejections)

---

## 2. Issues with the Current Rate Limiter

Current file: `Internal/AsyncRateLimiter.cs`.

### 2.1. Weak Points

- Custom implementation using `Channel` + `Timer`.  
- Unnecessary complexity compared to the official library.  
- Hard to expose metrics without adding even more code.

### 2.2. Impacts

- Higher chance of concurrency bugs.  
- High maintenance cost.  
- Lost optimization opportunities already provided by `System.Threading.RateLimiting`.

---

## 3. Target Strategy with System.Threading.RateLimiting

### 3.1. Rate Limiter Type

- Adopt **Token Bucket** (`TokenBucketRateLimiter`) as the default, as it is suitable for:
  - Limits such as "X requests per second" (or per minute).  
  - Allowing controlled bursts.

### 3.2. Business Parameters

- Example (configurable):
  - `RequestsPerSecond = 10` for private endpoints.  
  - `RequestsPerSecond = 50` for public endpoints.

### 3.3. Overall Design

- `RateLimiterFactory` in `Internal/RateLimiting/RateLimiterFactory.cs`.  
- `RateLimiterMetrics` in `Internal/RateLimiting/RateLimiterMetrics.cs`.  
- Rate limiter instantiated and maintained inside `MercadoBitcoinClient` or in a shared service.

---

## 4. TokenBucketRateLimiter – Base Configuration

### 4.1. Configuration Example

```csharp
using System.Threading.RateLimiting;

namespace MercadoBitcoin.Client.Internal.RateLimiting;

internal static class RateLimiterFactory
{
    public static TokenBucketRateLimiter CreateTokenBucket(
        int requestsPerSecond,
        int burstCapacity = 10,
        int queueLimit = 100)
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = burstCapacity,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = queueLimit,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = requestsPerSecond,
            AutoReplenishment = true,
        });
    }
}
```

### 4.2. Parameters

- `TokenLimit` – burst capacity (max tokens in the bucket).  
- `TokensPerPeriod` – number of tokens added per period.  
- `ReplenishmentPeriod` – replenishment interval (1s is typical).  
- `QueueLimit` – how many requests may wait for tokens.

---

## 5. RateLimiterFactory and RateLimiterMetrics

### 5.1. RateLimiterFactory

- Responsible for creating limiter instances from configuration options (`MercadoBitcoinClientOptions`).  
- Allows variation by endpoint type (public/private).

### 5.2. RateLimiterMetrics

```csharp
namespace MercadoBitcoin.Client.Internal.RateLimiting;

internal sealed class RateLimiterMetrics
{
    private long _acquired;
    private long _failed;
    private long _queued;

    public void IncrementAcquired() => Interlocked.Increment(ref _acquired);
    public void IncrementFailed() => Interlocked.Increment(ref _failed);
    public void IncrementQueued() => Interlocked.Increment(ref _queued);

    public (long acquired, long failed, long queued) Snapshot()
    {
        return (
            Interlocked.Read(ref _acquired),
            Interlocked.Read(ref _failed),
            Interlocked.Read(ref _queued));
    }
}
```

- Future: expose as metrics via `Meter` / `Counter<long>`.

---

## 6. Integration with MercadoBitcoinClient

### 6.1. Lease Acquisition

Before sending an HTTP request, acquire a lease from the rate limiter:

```csharp
public async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
{
    using RateLimitLease lease = await _rateLimiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);

    if (!lease.IsAcquired)
    {
        _metrics.IncrementFailed();
        throw new MercadoBitcoinApiException("Rate limit exceeded.");
    }

    _metrics.IncrementAcquired();

    return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
}
```

### 6.2. Location

- Rate limiting logic may live in:
  - A dedicated `DelegatingHandler` (e.g., `RateLimitingHandler`).  
  - Or inside `MercadoBitcoinClient` itself (handler is preferred for reuse).

---

## 7. Integration with Retry and Circuit Breaker

### 7.1. Retry (Polly)

- Requests failing with `RateLimitExceeded` **must not** be automatically retried (depends on business policy).  
- Requests failing with `HttpRequestException` / timeouts may be retried with backoff, respecting the rate limiter.

### 7.2. Circuit Breaker

- Many `RateLimitExceeded` errors may indicate a need to adjust configuration, not necessarily to open the circuit.

---

## 8. Metrics and Observability

### 8.1. Important Metrics

- Number of successful acquisitions.  
- Number of rejected acquisitions.  
- Average queue size.

### 8.2. Integration with `System.Diagnostics.Metrics`

- Create a dedicated `Meter` for rate limiting (e.g., `MercadoBitcoin.Client.RateLimiting`).  
- Expose counters/gauges for Prometheus, Application Insights, etc.

---

## 9. Load Tests and Validation

### 9.1. Unit Tests

- Ensure the limiter respects the configured limit (e.g., 10 req/s) under high concurrency.  
- Ensure requests above the limit are properly rejected or queued.

### 9.2. Load Tests

- Use `k6` / `JMeter` to send request bursts and validate that:
  - The rate limiter smooths the traffic.  
  - There is no overload of the Mercado Bitcoin API.

---

## 10. Implementation Checklist

- [ ] Implement `RateLimiterFactory` in `Internal/RateLimiting/RateLimiterFactory.cs`.  
- [ ] Implement `RateLimiterMetrics` in `Internal/RateLimiting/RateLimiterMetrics.cs`.  
- [ ] Add `TokenBucketRateLimiter` instances in `MercadoBitcoinClient` or a dedicated handler.  
- [ ] Integrate lease acquisition before each HTTP request.  
- [ ] Remove/delete `AsyncRateLimiter.cs` after migration.  
- [ ] Add unit tests for rate limiting.  
- [ ] Validate under load that behavior respects configured limits.

---

**Document**: 10-RATE-LIMITING.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned / Awaiting Implementation  
**Next**: [11-SOURCE-GENERATORS.md](11-SOURCE-GENERATORS.md)
