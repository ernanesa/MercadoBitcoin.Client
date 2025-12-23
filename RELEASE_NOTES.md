# Release Notes - MercadoBitcoin.Client

This document consolidates all release notes for the MercadoBitcoin.Client library.

## Table of Contents

- [v5.1.1 (2025-12-23)](#v511---2025-12-23) - **LATEST STABLE**
- [v5.1.0 (2025-12-23)](#v510---2025-12-23)
- [v5.0.0 (2025-12-23)](#v500---2025-12-23)
- [v3.0.0 (2025-08-27)](#v300---2025-08-27)
- [Previous Versions](#previous-versions)

---

## [v5.1.1] - 2025-12-23

### 🔧 DI Consistency & Performance

This patch release refactors the Dependency Injection integration to ensure better consistency and performance across different service lifetimes.

### 🔧 Technical Changes

- **IOptions Migration**: Switched from `IOptionsSnapshot<T>` to `IOptions<T>` in the `MercadoBitcoinClient` constructor and DI registration.
- **Performance**: `IOptions<T>` has lower overhead than `IOptionsSnapshot<T>` as it doesn't require scope-based resolution.
- **Consistency**: Ensures the client behaves consistently whether registered as Transient, Scoped, or Singleton (though Transient/Scoped is recommended for `HttpClient` based clients).

---

## [v5.1.0] - 2025-12-23

### 🎉 Enterprise & Multi-User Release

This release introduces a major architectural shift to support **Enterprise-grade multi-user applications** and **Universal Filtering** across the entire API surface.

### 🚀 New Features

#### Multi-User Architecture (Scoped DI)

The client now supports dynamic credential resolution via `IMercadoBitcoinCredentialProvider`. This is ideal for Web APIs where each request might belong to a different user:

```csharp
// 1. Implement the provider
public class MyUserCredentialProvider : IMercadoBitcoinCredentialProvider
{
    public Task<MercadoBitcoinCredentials?> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        // Fetch credentials from your database, vault, or request context
        return Task.FromResult(new MercadoBitcoinCredentials("KEY", "SECRET"));
    }
}

// 2. Register in DI
services.AddScoped<IMercadoBitcoinCredentialProvider, MyUserCredentialProvider>();
services.AddMercadoBitcoinClient();

// 3. Use the client normally - it will automatically use the scoped credentials
public class MyService(IMercadoBitcoinClient client)
{
    public async Task GetBalance() => await client.GetAccountBalanceAsync();
}
```

#### Universal Filtering (Fetch All)

All methods that previously required a symbol now support fetching data for **all tradable assets** by passing `null` or an empty list:

```csharp
// Fetch tickers for ALL symbols
var allTickers = await client.GetTickersAsync();

// Fetch balance for ALL assets
var allBalances = await client.GetAccountBalanceAsync();

// List orders for ALL symbols in parallel
var allOrders = await client.ListAllOrdersAsync();
```

#### Backward Compatibility

Existing code using string-based symbols continues to work without changes:

```csharp
// Still works perfectly
var ticker = await client.GetTickerAsync("BTC-BRL");
```

### 🔧 Technical Improvements

- **Parallel Fan-out**: Endpoints that don't support native batching (like `ListAllOrders`) now use high-performance parallel execution to aggregate results.
- **AOT Readiness**: Expanded Source Generation support for all new response types.
- **Resilient Symbol Discovery**: `GetAllSymbolsAsync` now filters for `ExchangeTraded` assets to ensure reliability in private endpoints.

### ✅ Test Suite

- **94 integration tests** passing (100% success rate).
- Validated multi-user credential resolution.
- Validated universal filtering across Public, Trading, Wallet, and Account categories.

---

## [v5.0.0] - 2025-12-23

### 🎉 Performance & Modernization Release

This release marks a significant cleanup and modernization of the library, focusing on **Polly v8**, **HTTP/2 Multiplexing**, and **Request Coalescing**.

### 🚀 New Features

#### Polly v8 Resilience Pipelines

Complete migration to the modern Polly v8 API for better performance and cleaner configuration:

```csharp
builder.Services.AddMercadoBitcoinClient(options =>
{
    options.RetryPolicyConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();
});
```

#### Request Coalescing

Prevents "thundering herd" problems by coalescing multiple concurrent requests for the same resource into a single API call.

#### Server Time Estimation

High-precision clock synchronization to prevent "Request out of time" errors in high-frequency trading scenarios.

### ⚡ Performance Improvements

- **HTTP/2 Multiplexing**: Native support for parallel requests over a single connection.
- **Scorched Earth Cleanup**: Removed ~2,000 lines of legacy code.
- **L1 Cache**: Integrated memory caching for static/semi-static data.

---

## [v4.1.0] - 2025-11-25

### 🎉 Stable Release

This is the stable 4.0.0 version, marking the complete migration to **.NET 10** and **C# 14** with **HTTP/3** support.

### ⚠️ Breaking Changes

#### 1. Removal of Public Constructors

All public convenience constructors of `MercadoBitcoinClient` have been **removed**.

**Before (obsolete):**
```csharp
var client = new MercadoBitcoinClient();
```

**After (v4.0.0+):**
```csharp
using MercadoBitcoin.Client.Extensions;

// Option 1: Factory methods
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Option 2: Dependency Injection (recommended for applications)
builder.Services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = "https://api.mercadobitcoin.net/api/v4";
    options.HttpConfiguration = HttpConfiguration.CreateHttp2Default();
    options.RetryPolicyConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();
});
```

#### 2. Target Framework

- **Requirement**: .NET 10.0 or higher
- **Language**: C# 14
- Consumer projects must update `global.json` and `.csproj` to `net10.0`

#### 3. HTTP/2 as Default

- **HTTP/2** is now the default protocol in all factories
- **HTTP/3** available via explicit configuration: `HttpConfiguration.CreateHttp3Default()`
- Environments without HTTP/3 support should use HTTP/2 (default)

#### 4. Standardized Configuration

All HTTP and retry configurations are centralized in:
- `MercadoBitcoinClientOptions`
- `HttpConfiguration`
- `RetryPolicyConfig`

### 🚀 New Features

#### HTTP/3 (QUIC) Support

Optional HTTP/3 support for low-latency scenarios:

```csharp
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Http;

var http3Config = HttpConfiguration.CreateHttp3Default();
var retryConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();

var client = MercadoBitcoinClientExtensions.CreateWithHttp2(retryConfig, http3Config);
```

**HTTP/3 Benefits:**
- QUIC protocol over UDP
- Multiplexing without head-of-line blocking
- 0-RTT connection resumption
- Better congestion control
- Integrated TLS 1.3

#### SIMD Extensions for Candle Analysis

New high-performance extensions in `CandleMathExtensions`:

```csharp
using MercadoBitcoin.Client.Extensions;

var candles = await client.GetCandlesAsync("BTC-BRL", "1h", to, countback: 100);
var avgClose = candles.CalculateAverageClose();
var maxHigh = candles.CalculateMaxHigh();
var minLow = candles.CalculateMinLow();
```

**Performance:**
- Uses **AVX2** when available
- Safe fallback to scalar loop
- Up to 4x faster in aggregation operations

#### Specialized Retry Configurations

New predefined configurations based on use cases:

```csharp
// For trading (more aggressive)
var tradingConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();
// 5 attempts, initial delay 0.5s, backoff 1.5x, max 10s

// For public data (more conservative)
var publicConfig = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();
// 2 attempts, initial delay 2s, backoff 2x, max 30s

// For development (no retry)
var devClient = MercadoBitcoinClientExtensions.CreateForDevelopment();
```

#### Metrics and Observability

Expanded integration with `System.Diagnostics.Metrics`:

**Counters:**
- `mb_client_http_retries` - Number of retry attempts
- `mb_client_circuit_opened` - Circuit breaker openings
- `mb_client_circuit_half_open` - Transitions to half-open
- `mb_client_circuit_closed` - Circuit breaker closures

**Histogram:**
- `mb_client_http_request_duration` - HTTP request duration (ms)

**Rate Limiting Metrics:**
- New metrics in `RateLimiterMetrics` to track rate limits

### 📚 Enhanced Documentation

- **README.md** completely revised with focus on factories and DI
- **docs/USER_GUIDE.md** - Complete guide for developers
- **docs/AI_USAGE_GUIDE.md** - Guide optimized for agents/LLMs
- Updated code examples for v4.0.0
- Clear separation between public and private endpoints

### 🔧 Performance Improvements

- **Polly v8** with optimized retry policies
- **System.Text.Json** with Source Generators (2x faster)
- **Complete AOT compatibility**
- **Native HTTP/2** with multiplexing
- **Optional HTTP/3** for ultra-low latency
- **SIMD** for mathematical operations on candles

### 📦 How to Migrate from v3.x

#### Step 1: Update Target Framework

```xml
<!-- .csproj -->
<TargetFramework>net10.0</TargetFramework>
```

```json
// global.json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

#### Step 2: Replace Constructors

**Console App / Worker:**
```csharp
using MercadoBitcoin.Client.Extensions;

// Before
// var client = new MercadoBitcoinClient();

// After
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
```

**Web API with DI:**
```csharp
using MercadoBitcoin.Client.Extensions;

// Program.cs
builder.Services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = builder.Configuration["MercadoBitcoin:BaseUrl"]
                      ?? "https://api.mercadobitcoin.net/api/v4";
    options.HttpConfiguration = HttpConfiguration.CreateHttp2Default();
    options.RetryPolicyConfig = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();
});
```

#### Step 3: Test

```bash
dotnet build
dotnet test
```

### ✅ Verified and Tested

- ✅ All endpoints tested with real credentials
- ✅ 65+ comprehensive tests passing
- ✅ Build in Release mode without critical errors
- ✅ NuGet package created and validated
- ✅ AOT compatibility verified
- ✅ Performance benchmarks executed

### 🔗 Useful Links

- [README.md](README.md) - Main documentation
- [CHANGELOG.md](CHANGELOG.md) - Complete change history
- [docs/USER_GUIDE.md](docs/USER_GUIDE.md) - User guide
- [GitHub Repository](https://github.com/ernanesa/MercadoBitcoin.Client)
- [API Documentation](https://api.mercadobitcoin.net/api/v4/docs)

---

## [v3.0.0] - 2025-08-27

### ⚠️ Breaking Changes

#### Removal of Public Constructors (First Phase)

- All public constructors of `MercadoBitcoinClient` have been removed
- Instantiation now only via extension methods or DI
- Legacy methods like `CreateLegacyHttpClient` removed

**Before (v2.x - obsolete):**
```csharp
var client = new MercadoBitcoinClient();
```

**After (v3.0.0+):**
```csharp
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
// or via DI:
services.AddMercadoBitcoinClient(...);
```

### 📋 Reason for Changes

- Alignment with .NET 9 and AOT best practices
- Avoids incorrect HttpClient usage and performance issues
- Facilitates integration with DI and modern configurations
- Preparation for future extensions and new feature support

### 🔧 Other Improvements

- Revised documentation and updated examples
- Preparation for HTTP/3 support
- Improved configuration architecture
- Detailed release notes

### 📚 Migration

See README.md for complete examples and detailed migration instructions.

---

## Previous Versions

### [2.1.1] - 2025-06-10
**Patch Release**
- Improved AOT compatibility (TypeInfoResolver, extra DTOs in JsonSerializerContext)
- Fixed JSON deserialization errors in AOT builds
- Memory threshold adjustment in tests
- All 64 tests passing

### [2.1.0] - 2025-05-01
**Minor Release**
- Configurable jitter in backoff
- Manual circuit breaker
- CancellationToken in 100% of endpoints
- Customizable User-Agent via environment variable
- Native metrics: counters and latency histogram
- Expanded test suite: 64 scenarios

### [2.0.1] - 2024-12-15
**Patch Release**
- Fixed error handling in AuthHttpClient
- Expanded test coverage
- Memory usage optimization

### [2.0.0] - 2024-11-01
**Major Release - BREAKING CHANGES**
- Complete migration to System.Text.Json with Source Generators
- Full AOT (Ahead-of-Time compilation) support
- .NET 9 and C# 13
- Native HTTP/2
- Comprehensive tests
- Removal of Newtonsoft.Json dependency
- Change from snake_case in JSON names
- Significant performance and architecture improvements

---

## Support and Contributions

For questions, issues or suggestions:
- 📝 Open an [issue on GitHub](https://github.com/ernanesa/MercadoBitcoin.Client/issues)
- 💬 See the [complete documentation](README.md)
- 🤝 Contributions are welcome via Pull Requests

---

**Last update:** December 2025 - Version 5.1.1 (Enterprise & Multi-User Release: Scoped DI, Universal Filtering, and DI Consistency)
