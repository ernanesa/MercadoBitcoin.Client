# Architecture Overview - MercadoBitcoin.Client

## 📋 Executive Summary

This .NET library provides a complete, high‑performance interface to Mercado Bitcoin API v4, the largest cryptocurrency exchange in Latin America. The implementation focuses on **maximum performance**, **minimal latency**, and **production‑grade robustness**.

## 🎯 Main Objectives

### Performance and Efficiency
- ✅ **Native HTTP/2** with multiplexing for parallel requests  
- ✅ **System.Text.Json** with Source Generators for ultra‑fast serialization (2x faster than Newtonsoft.Json)  
- ✅ **AOT compatible** for near‑instant startup applications  
- ✅ **Zero reflection** at runtime  
- ✅ Optimized **connection pooling**  
- ✅ Smart **client‑side rate limiting**

### Robustness and Resilience
- ✅ **Retry policies** with exponential backoff + jitter  
- ✅ **Circuit breaker** to protect against cascading failures  
- ✅ **Configurable timeouts** per operation  
- ✅ **Metrics** (System.Diagnostics.Metrics) for observability  
- ✅ **Rich error handling** with specific types

### Usability
- ✅ **Fluent, intuitive API**  
- ✅ **Strongly typed** models everywhere  
- ✅ Native **async/await**  
- ✅ **Dependency Injection** friendly  
- ✅ Comprehensive **inline documentation**

## 🏗️ Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Application                        │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              MercadoBitcoinClient (Facade)                   │
│  • Public Methods (GetTickers, GetOrderBook, etc)            │
│  • Private Methods (GetBalances, PlaceOrder, etc)            │
│  • Authentication Management                                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌──────────────┐  ┌────────────────┐  ┌──────────────┐
│ Rate Limiter │  │  Auth Handler  │  │  Generated   │
│              │  │                │  │   Client     │
│ • Token      │  │ • Bearer Token │  │              │
│   Bucket     │  │ • Token Mgmt   │  │ • NSwag Gen  │
└──────────────┘  └────────────────┘  └──────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      Retry Handler                           │
│  • Polly Policies                                            │
│  • Exponential Backoff + Jitter                              │
│  • Circuit Breaker (manual)                                  │
│  • Retry-After header respect                                │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   HttpClient (HTTP/2)                        │
│  • Connection Pooling                                        │
│  • Compression (gzip/deflate)                                │
│  • TLS 1.3                                                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                Mercado Bitcoin API v4                        │
└─────────────────────────────────────────────────────────────┘
```

### High‑Level Flow

1. The **Client Application** calls methods on `MercadoBitcoinClient`.  
2. `MercadoBitcoinClient` orchestrates:
   - Rate limiting  
   - Authentication (via `AuthHttpClient`)  
   - Retry policies and circuit breaker (via `RetryHandler`)  
   - HTTP/2 requests (via `HttpClient`)  
3. Responses are deserialized using `System.Text.Json` with Source Generators and exposed as strongly typed models.

---

## 🧩 Core Components

### 1. MercadoBitcoinClient (Facade)

**Responsibility**: Main entry point and façade for the entire library.

**Key Functions**:
- Public methods: `GetTickersAsync`, `GetOrderBookAsync`, `PlaceOrderAsync`, etc.  
- Orchestrates internal components:
  - `AuthHttpClient`  
  - `RetryHandler`  
  - Rate limiter  
  - Generated HTTP client  
- Maps API exceptions to rich types (`MercadoBitcoinApiException`, etc.)  
- Manages authentication via `AuthHttpClient`  
- Applies rate limiting via `AsyncRateLimiter`

### 2. RetryHandler (Resilience)

**Responsibility**: Implement retry policies, circuit breaker, and metrics.

**Capabilities**:
- **Retry with Polly** using configurable exponential backoff  
- **Jitter** to avoid thundering herd  
- **Retry‑After** header respect for rate limiting  
- **Manual circuit breaker**:
  - Opens after N consecutive failures  
  - Half‑open and closes after successful attempts  
- **Metrics**: counters and histograms for observability

**Configuration**:
```csharp
public class RetryPolicyConfig
{
    public int MaxRetryAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 1.0;
    public double BackoffMultiplier { get; set; } = 2.0;
    public double MaxDelaySeconds { get; set; } = 30.0;
    public bool EnableJitter { get; set; } = true;
    public int JitterMillisecondsMax { get; set; } = 250;
    
    public bool EnableCircuitBreaker { get; set; } = true;
    public int CircuitBreakerFailuresBeforeBreaking { get; set; } = 8;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    
    public bool RetryOnTimeout { get; set; } = true;
    public bool RetryOnRateLimit { get; set; } = true;
    public bool RetryOnServerErrors { get; set; } = true;
    public bool RespectRetryAfterHeader { get; set; } = true;
    
    public bool EnableMetrics { get; set; } = true;
}
```

**Retry Scenarios**:
- ⏱️ Timeout (408 Request Timeout)  
- 🚦 Rate limiting (429 Too Many Requests)  
- 🔥 Server errors (500, 502, 503, 504)  
- 🌐 Network failures (`HttpRequestException`, `TaskCanceledException`)

### 3. AuthHttpClient (Authentication)

**Responsibility**: Manage Bearer token and inject it into authenticated requests.

```csharp
public class AuthHttpClient
{
    private string? _accessToken;
    
    public void SetAccessToken(string token) => _accessToken = token;
    public string? GetAccessToken() => _accessToken;
    
    // Injects the token via DelegatingHandler
}
```

### 4. AsyncRateLimiter (Client‑Side Rate Limiting)

**Responsibility**: Control request rate so that API limits are not exceeded.

```csharp
public class AsyncRateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<DateTime> _requestTimes;
    private readonly int _requestsPerSecond;
    
    public async Task WaitAsync(CancellationToken ct = default)
    {
        // Token bucket algorithm
        // Remove old timestamps
        // Wait as needed
    }
}
```

**API Rate Limits**:
- **Global limit**: 500 requests/min  
- **Public endpoints**: 1 req/s  
- **Trading (POST/DELETE)**: 3 req/s  
- **Trading (GET)**: 10 req/s  
- **Account**: 3 req/s  
- **Cancel All Orders**: 1 req/min

### 5. Generated Client (NSwag)

**Responsibility**: Auto‑generated HTTP client from `swagger.yaml`.

**Benefits**:
- Strongly typed models  
- Automatic parameter validation  
- Automatic serialization/deserialization  
- Easier maintenance (regenerate when API changes)

### 6. MercadoBitcoinJsonSerializerContext (AOT)

**Responsibility**: JSON serialization context using Source Generators.

```csharp
[JsonSourceGeneration(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AccountResponse))]
[JsonSerializable(typeof(PlaceOrderRequest))]
[JsonSerializable(typeof(TickerResponse))]
// ... all DTOs
public partial class MercadoBitcoinJsonSerializerContext : JsonSerializerContext { }
```

**Benefits**:
- Zero reflection at runtime  
- Up to 2x better performance  
- Native AOT friendly  
- Lower memory usage

---

## 🚀 HTTP/2 and Performance

### Why HTTP/2?

1. **Multiplexing**: multiple simultaneous requests over the same TCP connection  
2. **Header compression**: HPACK reduces overhead by ~30%  
3. **Server Push**: support for server‑initiated responses (if implemented)  
4. **Binary protocol**: more efficient than text‑based HTTP/1.1  
5. **Connection reuse**: less TLS handshake overhead

### Measured Gains

```
Benchmark: 100 parallel requests
HTTP/1.1: 2.3s
HTTP/2:   0.8s  (65% faster)

Memory:
HTTP/1.1: ~50 MB
HTTP/2:   ~32 MB (36% less)
```

### HTTP/2 Configuration

```csharp
public class HttpConfiguration
{
    public Version HttpVersion { get; set; } = new Version(2, 0);
    public HttpVersionPolicy VersionPolicy { get; set; } = HttpVersionPolicy.RequestVersionOrLower;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxConnectionsPerServer { get; set; } = 100;
    public bool EnableCompression { get; set; } = true;
    
    public static HttpConfiguration CreateHttp2Default() => new()
    {
        HttpVersion = new Version(2, 0),
        VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
        EnableCompression = true
    };
}
```

---

## 📊 Metrics and Observability

The library exposes metrics via `System.Diagnostics.Metrics` (OpenTelemetry‑compatible).

### Counters

| Metric | Type | Description | Tags |
|--------|------|-------------|------|
| `mb_client_http_retries` | Counter<long> | Number of retries | `status_code` |
| `mb_client_circuit_opened` | Counter<long> | Circuit breaker opened | - |
| `mb_client_circuit_half_open` | Counter<long> | Circuit breaker half‑open | - |
| `mb_client_circuit_closed` | Counter<long> | Circuit breaker closed | - |

### Histograms

| Metric | Type | Unit | Description | Tags |
|--------|------|------|-------------|------|
| `mb_client_http_request_duration` | Histogram<double> | ms | Request duration | `method`, `outcome`, `status_code` |

### Outcomes

| Outcome | Description |
|---------|-------------|
| `success` | 2xx/3xx with no retry |
| `client_error` | Non‑retriable 4xx |
| `server_error` | Final 5xx |
| `transient_exhausted` | Retries exhausted |
| `circuit_open_fast_fail` | Blocked by circuit breaker |
| `timeout_or_canceled` | Timeout/cancellation |
| `canceled` | Externally canceled |
| `exception` | Non‑HTTP exception |

### OpenTelemetry Integration

```csharp
var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("MercadoBitcoin.Client")
    .AddPrometheusExporter()
    .AddOtlpExporter()
    .Build();
```

---

## 🔒 Security

### Authentication

- **Bearer Token**: API v4 authentication mechanism  
- **Token management**: secure storage via `AuthHttpClient`  
- **Automatic injection**: `Authorization: Bearer <token>` header

### Best Practices

1. **Never hard‑code credentials** in source code  
2. Use **environment variables** or a secrets manager (e.g. Azure Key Vault)  
3. Rotate tokens regularly  
4. Enforce **TLS 1.3** (HTTP/2)  
5. Keep certificate validation enabled

### Secure Example

```csharp
// ✅ Correct
var apiId = Environment.GetEnvironmentVariable("MB_API_ID");
var apiSecret = Environment.GetEnvironmentVariable("MB_API_SECRET");
await client.AuthenticateAsync(apiId, apiSecret);

// ❌ Never do this
await client.AuthenticateAsync("hardcoded_id", "hardcoded_secret");
```

---

## 🧪 Testability

### Dependency Injection

```csharp
services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = "https://api.mercadobitcoin.net/api/v4";
    options.RequestsPerSecond = 5;
    options.MaxRetryAttempts = 3;
    options.EnableCircuitBreaker = true;
});
```

### Mocking

```csharp
// Mock HttpMessageHandler for unit tests
var mockHandler = new Mock<HttpMessageHandler>();
var client = new MercadoBitcoinClient(
    new HttpClient(mockHandler.Object),
    new AuthHttpClient()
);
```

### Integration Tests

The library includes 60+ tests covering:
- ✅ All public endpoints  
- ✅ Private endpoints (skipped if credentials are missing)  
- ✅ Serialization round‑trip  
- ✅ Performance and benchmarks  
- ✅ Error handling  
- ✅ Retry policies  
- ✅ Circuit breaker behavior

---

## 📈 Implementation Roadmap

See `10-IMPLEMENTATION-ROADMAP.md` for the detailed plan.

---

## 🔗 References

- [Mercado Bitcoin API v4](https://api.mercadobitcoin.net/api/v4/docs)  
- [HTTP/2 RFC 7540](https://tools.ietf.org/html/rfc7540)  
- [Polly Documentation](https://github.com/App-vNext/Polly)  
- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)  
- [.NET Metrics](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/metrics)

---

**Version**: 3.0.0  
**Last update**: November 2025  
**Status**: ✅ Production

