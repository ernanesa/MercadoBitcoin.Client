```markdown
# Error Handling and Resilience - Mercado Bitcoin API

## 🚨 API Error Codes

### Domain Errors

| Code | Description | Recommended Action |
|------|-------------|--------------------|
| `API_UNAVAILABLE` | API temporarily unavailable | Retry with backoff |
| `FORBIDDEN` | Invalid credentials | Verify credentials |
| `INVALID_SYMBOL` | Symbol not found | Validate BASE-QUOTE format |
| `INVALID_PARAMETER` | Invalid parameter | Check documentation |
| `INSUFFICIENT_BALANCE` | Insufficient balance | Check balance before action |
| `ORDER_NOT_FOUND` | Order not found | Verify orderId |
| `ADDRESS_NOT_REGISTERED` | Address not registered | Register the address |
| `PROBLEM_TRANSFERRING` | Transfer error | Contact support |
| `INVALID_ACCESS` | Attempt with read-only key | Use a key with correct permissions |
| `ORDER_PROCESSED` | Order already processed/cancelled | Check order status |
| `INVALID_BANK_ACCOUNT` | Invalid bank account | Verify bank account data |
| `INVALID_WITHDRAWAL_VALUE` | Invalid withdrawal amount | Check limits |
| `WITHDRAWAL_AMOUNT_LIMIT` | Withdrawal limit exceeded | Wait 24h or adjust amount |
| `MINIMUM_WITHDRAWAL_AMOUNT` | Below minimum | Minimum R$ 50.00 |
| `REQUEST_RATE_EXCEEDED` | Rate limit exceeded | Wait and retry |
| `REQUEST_DENIED` | Request denied | Lower request rate |
| `REQUEST_BLOCKED` | Requests blocked | Wait for unblock |
| `ORDER_IN_PROCESSING` | Order in processing | Wait |
| `ORDER_DUPLICATE` | Duplicate order | Check externalId |
| `INVALID_LIMIT_PRICE` | Invalid limit price | Check min/max limits |
| `INVALID_STOP_PRICE` | Invalid stop price | stopPrice > limitPrice for sell |

### HTTP Status Codes

| Status | Meaning | Action |
|--------|---------|--------|
| 200 | Success | - |
| 400 | Bad Request | Validate parameters |
| 401 | Unauthorized | Re-authenticate |
| 403 | Forbidden | Check permissions |
| 404 | Not Found | Check endpoint/ids |
| 408 | Request Timeout | Retry |
| 429 | Too Many Requests | Rate limit - wait |
| 500 | Internal Server Error | Retry |
| 502 | Bad Gateway | Retry |
| 503 | Service Unavailable | Retry |
| 504 | Gateway Timeout | Retry |

## 🛡 Exception Hierarchy

```
Exception
└── MercadoBitcoinApiException (base)
    ├── MercadoBitcoinUnauthorizedException (401/403)
    ├── MercadoBitcoinValidationException (400)
    ├── MercadoBitcoinRateLimitException (429)
    └── MercadoBitcoinServerException (5xx)
```

## 🔄 Retry Strategies

### 1. Exponential Backoff + Jitter

```csharp
public class RetryPolicyConfig
{
    public int MaxRetryAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 1.0;
    public double BackoffMultiplier { get; set; } = 2.0;
    public double MaxDelaySeconds { get; set; } = 30.0;
    public bool EnableJitter { get; set; } = true;
    public int JitterMillisecondsMax { get; set; } = 250;
}

// Example delays with jitter:
// Attempt 1: 1s + jitter(0-250ms)
// Attempt 2: 2s + jitter(0-250ms)
// Attempt 3: 4s + jitter(0-250ms)
```

### 2. Selective Retry

Only automatically retry transient errors:
- Timeout (408)
- Rate limiting (429)
- Server errors (500, 502, 503, 504)
- Network failures (HttpRequestException)

Do NOT retry for:
- Bad Request (400)
- Unauthorized (401/403)
- Not Found (404)
- Domain validation errors

### 3. Respect `Retry-After` header

```csharp
if (response.StatusCode == HttpStatusCode.TooManyRequests)
{
    if (response.Headers.TryGetValues("Retry-After", out var values))
    {
        if (int.TryParse(values.First(), out var seconds))
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            // Retry
        }
    }
}
```

## ⚡ Circuit Breaker

### States

```
┌─────────┐  8 failures   ┌──────┐  30s    ┌───────────┐
│ Closed  │────────────>│ Open │────────>│ Half-Open │
└────▲────┘             └──────┘         └─────┬─────┘
     │                                         │
     │          Success                        │
     └─────────────────────────────────────────┘
```

| State | Behavior |
|-------|----------|
| Closed | Normal requests flow |
| Open | Fast-fail immediately |
| Half-Open | Allow a single test request |

### Configuration

```csharp
var config = new RetryPolicyConfig
{
    EnableCircuitBreaker = true,
    CircuitBreakerFailuresBeforeBreaking = 8,
    CircuitBreakerDurationSeconds = 30
};
```

### Events

```csharp
config.OnCircuitBreakerEvent = (e) =>
{
    Console.WriteLine($"Circuit Breaker: {e.State} - {e.Reason}");
    if (e.State == CircuitBreakerState.Open)
    {
        AlertOps("API Circuit Breaker opened!");
    }
};
```

## 🎯 Specific Handling Examples

### Full Example: Tickers with error handling

```csharp
public async Task<List<TickerResponse>> GetTickersWithErrorHandlingAsync(string symbols)
{
    try
    {
        return await client.GetTickersAsync(symbols);
    }
    catch (MercadoBitcoinRateLimitException ex)
    {
        var retryAfter = ex.RetryAfter ?? TimeSpan.FromSeconds(60);
        _logger.LogWarning("Rate limit hit. Waiting {Seconds}s", retryAfter.TotalSeconds);
        await Task.Delay(retryAfter);
        return await client.GetTickersAsync(symbols);
    }
    catch (MercadoBitcoinUnauthorizedException ex)
    {
        _logger.LogWarning("Token expired. Re-authenticating...");
        await client.AuthenticateAsync(apiId, apiSecret);
        return await client.GetTickersAsync(symbols);
    }
    catch (MercadoBitcoinValidationException ex)
    {
        _logger.LogError("Validation error: {Code} - {Message}", ex.Code, ex.Message);
        throw;
    }
    catch (MercadoBitcoinServerException ex)
    {
        _logger.LogError("Server error after retries: {Code}", ex.Code);
        if (_cache.TryGetValue("tickers", out var cached))
        {
            _logger.LogInformation("Returning cached data");
            return cached;
        }
        throw;
    }
}
```

### Resilience Pattern

```csharp
public class ResilientApiClient
{
    public async Task<T> ExecuteResilientlyAsync<T>(Func<Task<T>> operation, T? fallback = default, int maxAttempts = 3) where T : class
    {
        Exception? lastException = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (MercadoBitcoinRateLimitException ex)
            {
                if (attempt == maxAttempts) throw;
                var delay = ex.RetryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
            catch (MercadoBitcoinUnauthorizedException ex)
            {
                if (attempt == maxAttempts) throw;
                await ReAuthenticateAsync();
            }
            catch (MercadoBitcoinServerException ex)
            {
                lastException = ex;
                if (attempt == maxAttempts) break;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
            catch (MercadoBitcoinValidationException)
            {
                throw;
            }
        }

        if (fallback != null)
        {
            _logger.LogWarning("Returning fallback after {Max} attempts", maxAttempts);
            return fallback;
        }

        throw lastException ?? new Exception("Operation failed after multiple attempts");
    }
}
```

## 📊 Error Metrics

```csharp
public class ErrorMetrics
{
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _errorRateHistogram;
    
    public void RecordError(Exception ex)
    {
        var errorType = ex.GetType().Name;
        var errorCode = ex is MercadoBitcoinApiException apiEx ? apiEx.Code : "UNKNOWN";
        _errorCounter.Add(1,
            new KeyValuePair<string, object?>("type", errorType),
            new KeyValuePair<string, object?>("code", errorCode));
    }
}
```

## 🔔 Alerting and Monitoring

```csharp
public class ErrorAlerting
{
    private readonly int _errorThreshold = 10;
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(5);
    private readonly Queue<DateTime> _recentErrors = new();
    
    public void RecordError()
    {
        _recentErrors.Enqueue(DateTime.UtcNow);
        while (_recentErrors.Any() && DateTime.UtcNow - _recentErrors.Peek() > _timeWindow)
            _recentErrors.Dequeue();
        if (_recentErrors.Count >= _errorThreshold)
            AlertOps($"⚠️ {_recentErrors.Count} errors in the last {_timeWindow.TotalMinutes} minutes!");
    }
}
```

## ✅ Checklist

- [ ] Map all API error codes
- [ ] Implement exception hierarchy
- [ ] Configure retry policies
- [ ] Implement circuit breaker
- [ ] Respect `Retry-After` header
- [ ] Structured error logging
- [ ] Error metrics and alerts
- [ ] Fallbacks for critical data
- [ ] Troubleshooting documentation

**Next**: [08-SECURITY-BEST-PRACTICES.md](08-SECURITY-BEST-PRACTICES.md)

```
