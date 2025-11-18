# 09. HTTP OPTIMIZATION (HTTP/2, HTTP/3, SOCKETSHTTPHANDLER)

## 📋 Index

1. [Objective and Scope](#objective-and-scope)  
2. [Current Scenario](#current-scenario)  
3. [HTTP Performance Goals](#http-performance-goals)  
4. [Optimized SocketsHttpHandler Configuration](#optimized-socketshttphandler-configuration)  
5. [HTTP/2 – Multiplexing and Connection Pooling](#http2--multiplexing-and-connection-pooling)  
6. [HTTP/3 – QUIC and Use Cases](#http3--quic-and-use-cases)  
7. [Integration with AuthHttpClient and GeneratedClient](#integration-with-authhttpclient-and-generatedclient)  
8. [Timeouts, Retry, and Resilience](#timeouts-retry-and-resilience)  
9. [HTTP Telemetry and Diagnostics](#http-telemetry-and-diagnostics)  
10. [Metrics, Benchmarks, and Validation](#metrics-benchmarks-and-validation)  
11. [Implementation Checklist](#implementation-checklist)

---

## 1. Objective and Scope

This document defines how to **optimize the entire HTTP layer** of the MercadoBitcoin.Client library on .NET 10, using:

- Manually configured `SocketsHttpHandler`  
- HTTP/2 as default, with optional HTTP/3  
- Correct and predictable connection pooling  
- Consistent and aggressive, but safe, timeouts

---

## 2. Current Scenario

### 2.1. Situation (v3.0.0)

- `HttpClient` configured via DI  
- Likely reliance on the default handler created by `HttpClientFactory`  
- No explicit configuration for:
  - `PooledConnectionLifetime`  
  - `PooledConnectionIdleTimeout`  
  - `MaxConnectionsPerServer`  
  - `EnableMultipleHttp2Connections`

### 2.2. Issues

- Possible **underuse of HTTP/2** (no aggressive multiplexing)  
- Risk of **socket exhaustion** if pooling is not well configured  
- Potentially inconsistent timeouts (mix of `HttpClient.Timeout` and custom timeouts)

---

## 3. HTTP Performance Goals

- Ensure the library:
  - Uses HTTP/2 by default, with fallback to HTTP/1.1  
  - Can handle **high throughput** (10k+ req/min)  
  - Avoids unnecessary reconnections (good keep‑alive usage)  
  - Is prepared for HTTP/3 where applicable

---

## 4. Optimized SocketsHttpHandler Configuration

### 4.1. Base Handler

Target file: `Http/HttpConfiguration.cs` (or similar) – centralizing `SocketsHttpHandler` creation.

```csharp
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;

namespace MercadoBitcoin.Client.Http;

internal static class HttpConfiguration
{
    public static SocketsHttpHandler CreateOptimizedHandler()
    {
        var handler = new SocketsHttpHandler
        {
            // Pooling
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 50,

            // HTTP/2
            EnableMultipleHttp2Connections = true,

            // Keep-alive
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),

            // Compressão
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli,

            // Timeouts de baixo nível
            ConnectTimeout = TimeSpan.FromSeconds(5),
            Expect100ContinueTimeout = TimeSpan.FromSeconds(1),
            ResponseDrainTimeout = TimeSpan.FromSeconds(2),

            // Segurança
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            },

            // Proxy e cookies
            UseProxy = false,
            UseCookies = false,
        };

        return handler;
    }
}
```

### 4.2. Rationale for Key Parameters

- `PooledConnectionLifetime`: forces connection rotation to avoid long‑lived "bad" connections.  
- `PooledConnectionIdleTimeout`: closes idle connections to avoid wasting resources.  
- `MaxConnectionsPerServer`: protects against overloading a single host.  
- `EnableMultipleHttp2Connections`: improves parallelism for HTTP/2.  
- `AutomaticDecompression`: reduces network bandwidth usage.

---

## 5. HTTP/2 – Multiplexing and Connection Pooling

### 5.1. Configuração de Versão HTTP

Ao criar o `HttpClient` principal:

```csharp
client.BaseAddress = new Uri("https://api.mercadobitcoin.net");
client.DefaultRequestVersion = HttpVersion.Version20;
client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
client.Timeout = TimeSpan.FromSeconds(30); // timeout geral de request
```

### 5.2. Benefits

- A single TCP/TLS connection per host can serve multiple simultaneous requests.  
- Fewer TLS handshakes.  
- Better resource usage in high‑concurrency scenarios.

### 5.3. Considerations

- Some proxies and middleboxes still have issues with HTTP/2; allowing fallback is important.

---

## 6. HTTP/3 – QUIC and Use Cases

### 6.1. When to Consider HTTP/3

- Clients on unstable networks (mobile, public Wi‑Fi).  
- Scenarios where latency is critical.

### 6.2. Optional Configuration

When exposing an option for HTTP/3 (e.g., via `MercadoBitcoinClientOptions`):

```csharp
var handler = HttpConfiguration.CreateOptimizedHandler();

var client = new HttpClient(handler)
{
    DefaultRequestVersion = HttpVersion.Version30,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
};
```

### 6.3. Strategy in the Library

- Default: HTTP/2.  
- HTTP/3: configurable via an option, clearly documented as environment/infra‑dependent.

---

## 7. Integration with AuthHttpClient and GeneratedClient

### 7.1. AuthHttpClient

- Must receive an `HttpClient` already configured with the optimized handler.  
- Must not create new `HttpClient`/`SocketsHttpHandler` instances on its own.

### 7.2. GeneratedClient

- Generated clients (in `Generated/GeneratedClient.cs`) must be constructed using the same shared `HttpClient`.

### 7.3. DI Registration Example

```csharp
services.AddHttpClient("MercadoBitcoin", client =>
{
    client.BaseAddress = new Uri("https://api.mercadobitcoin.net");
    client.DefaultRequestVersion = HttpVersion.Version20;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(HttpConfiguration.CreateOptimizedHandler)
.SetHandlerLifetime(Timeout.InfiniteTimeSpan);
```

---

## 8. Timeouts, Retry, and Resilience

### 8.1. Timeout Layers

- `HttpClient.Timeout`: overall request timeout.  
- `SocketsHttpHandler.ConnectTimeout`: TCP connection timeout.  
- Retry policy (`RetryHandler` + Polly): handle timeouts with differentiated behavior.

### 8.2. Recommendations

- Set `HttpClient.Timeout` between 10–30s (configurable via options).  
- Do not use `CancellationToken.None` – always propagate the received token.

### 8.3. Retry Interaction

- For connection timeouts (`TaskCanceledException` + `TimeoutException` as `InnerException`), apply retry policy with backoff.  
- For timeouts triggered by `HttpClient.Timeout`, limit the number of attempts to avoid cascades.

---

## 9. HTTP Telemetry and Diagnostics

### 9.1. Important Log Fields

- Method (GET/POST/…)  
- URL (without sensitive query data)  
- Status code  
- Total request duration  
- Retry events (attempts, reasons)

### 9.2. EventSource / DiagnosticSource

- .NET exposes HTTP diagnostic events via the `System.Net.Http` diagnostic source.  
- We can later integrate with OpenTelemetry for distributed tracing.

---

## 10. Metrics, Benchmarks, and Validation

### 10.1. Expected Metrics

- Throughput increase relative to default configuration.  
- Reduction of errors related to timeouts/broken connections.

### 10.2. Benchmarks

- Test scenarios such as:
  - `GetTickerAsync` under high concurrency (e.g., 500 parallel tasks).  
  - `GetBalanceAsync` with authentication.

### 10.3. Tools

- `BenchmarkDotNet` for micro‑benchmarks.  
- `k6` / `JMeter` for end‑to‑end load testing.

---

## 11. Implementation Checklist

- [ ] Implement `HttpConfiguration.CreateOptimizedHandler()` with `SocketsHttpHandler`.  
- [ ] Configure the main `HttpClient` to use HTTP/2 by default.  
- [ ] Expose an option for HTTP/3 in `MercadoBitcoinClientOptions` (optional).  
- [ ] Ensure reuse of `HttpClient`/handler across the entire library.  
- [ ] Adjust timeouts (connection, request) with reasonable and configurable defaults.  
- [ ] Update integration tests to validate connectivity and behavior under load.

---

**Document**: 09-HTTP-OPTIMIZATION.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned / Awaiting Implementation  
**Next**: [10-RATE-LIMITING.md](10-RATE-LIMITING.md)
