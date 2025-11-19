# MercadoBitcoin.Client

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![C#](https://img.shields.io/badge/C%23-14.0-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![System.Text.Json](https://img.shields.io/badge/System.Text.Json-Source%20Generators-purple)](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation)
[![AOT](https://img.shields.io/badge/AOT-Compatible-brightgreen)](https://docs.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![API](https://img.shields.io/badge/API-v4.0-orange)](https://api.mercadobitcoin.net/api/v4/docs)
[![HTTP/2](https://img.shields.io/badge/HTTP-2.0-brightgreen)](https://tools.ietf.org/html/rfc7540)

A complete and modern .NET 10 library for integrating with the **Mercado Bitcoin API v4**. This library provides access to all available platform endpoints, including public data, trading, account management, and wallet operations, with native support for **HTTP/3** and **System.Text.Json** for maximum performance and AOT compatibility.

> **WARNING: BREAKING CHANGE IN VERSION 4.0.0**
>
> All public constructors of `MercadoBitcoinClient` have been **removed**. The only supported way to instantiate the client is now via extension methods (`MercadoBitcoinClientExtensions.CreateWithRetryPolicies`, etc.) or dependency injection (`services.AddMercadoBitcoinClient(...)`).
>
> **Before (obsolete):**
> ```csharp
> var client = new MercadoBitcoinClient();
> ```
> **After (v4.0.0+):**
> ```csharp
> var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
> // or via DI:
> services.AddMercadoBitcoinClient(...);
> ```
> See the "Migration and Updates" section for details.

## 🚀 Features

- ✅ **Complete Coverage**: All Mercado Bitcoin API v4 endpoints
- ✅ **.NET 10 + C# 14**: Latest framework and language with optimized performance
- ✅ **System.Text.Json**: Native JSON serialization with Source Generators for maximum performance
- ✅ **AOT Compatible**: Compatible with Native AOT compilation for ultra-fast applications
- ✅ **Native HTTP/3**: HTTP/3 protocol by default for maximum performance
- ✅ **Async/Await**: Native asynchronous programming
- ✅ **Strongly Typed**: Typed data models for type safety
- ✅ **OpenAPI Integration**: Client automatically generated via NSwag
- ✅ **Clean Architecture**: Organized and maintainable code
- ✅ **Error Handling**: Robust error handling system
- ✅ **Retry Policies**: Exponential backoff + configurable jitter
- ✅ **Manual Circuit Breaker**: Protection against cascading failures (configurable)
- ✅ **Rate Limit Aware**: Respects limits and Retry-After header
- ✅ **CancellationToken in All Endpoints**: Complete cooperative cancellation
- ✅ **Custom User-Agent**: Override via `MB_USER_AGENT` env var for observability
- ✅ **Production Ready**: Ready for production use
- ✅ **Comprehensive Tests**: 64 tests covering all scenarios
- ✅ **Validated Performance**: Benchmarks prove 2x+ improvements
- ✅ **Robust Handling**: Graceful skip for scenarios without credentials
- ✅ **CI/CD Ready**: Optimized configuration for continuous integration

## 📦 Installation

```bash
# Via Package Manager Console
Install-Package MercadoBitcoin.Client

# Via .NET CLI
dotnet add package MercadoBitcoin.Client

# Via PackageReference
<PackageReference Include="MercadoBitcoin.Client" Version="4.0.0-alpha.1" />
```

## ⚡️ Usage: Public vs Private Endpoints

> **Attention:**
> - **Public Data** (e.g., tickers, candles, trades, orderbook, fees, symbols) **DO NOT require authentication**. Just instantiate the client and call the methods.
> - **Private Data** (e.g., balances, orders, deposits, withdrawals, trading) **REQUIRE authentication**. Use `AuthenticateAsync` before calling private methods.

### Quick Examples

**Public Data (NO authentication needed):**
```csharp
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
var tickers = await client.GetTickersAsync("BTC-BRL");
var candles = await client.GetCandlesAsync("BTC-BRL", "1h", to: (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), countback: 24);
```

**Private Data (Authentication needed):**
```csharp
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
await client.AuthenticateAsync("your_login", "your_password");
var accounts = await client.GetAccountsAsync();
var balances = await client.GetBalancesAsync(accounts.First().Id);
```

## 🔧 Configuration

### Basic Configuration (Modern Methods Only)

```csharp
using MercadoBitcoin.Client.Extensions;

// Recommended configuration (retry policies + HTTP/2)
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Optimized configuration for trading
var client = MercadoBitcoinClientExtensions.CreateForTrading();

// Configuration via DI (recommended for ASP.NET Core)
services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = "https://api.mercadobitcoin.net/api/v4";
    // ...other options
});
```

### Configuration with Dependency Injection (Recommended)

```csharp
// Program.cs or Startup.cs
services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = "https://api.mercadobitcoin.net/api/v4";
    options.HttpVersion = HttpVersion.Version30; // HTTP/3 by default
    options.EnableRetryPolicy = true;
});
```

## 🔄 Retry Policies and HTTP/3

The library implements robust retry policies with **Polly v8** and uses **HTTP/3** by default for maximum performance:

### HTTP/3 Features
- **QUIC Protocol**: Built on UDP for faster connection establishment
- **Multiplexing**: Multiple simultaneous requests without head-of-line blocking
- **0-RTT Resumption**: Near-instant connection resumption
- **Improved Congestion Control**: Better performance on lossy networks
- **Enhanced Security**: Integrated TLS 1.3

### Retry Policies
- **Exponential Backoff**: Increasing delay between attempts
- **Circuit Breaker**: Protection against cascading failures
- **Timeout Handling**: Configurable timeouts per operation
- **Rate Limit Aware**: Automatically respects API limits

### Custom Retry Configurations

```csharp
using MercadoBitcoin.Client.Http;

// Trading configuration (more aggressive)
var tradingConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();
// 5 attempts, initial delay 0.5s, backoff 1.5x, max 10s

// Public data configuration (more conservative)
var publicConfig = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();
// 2 attempts, initial delay 2s, backoff 2x, max 30s

// Custom configuration
var customConfig = new RetryPolicyConfig
{
    MaxRetryAttempts = 3,
    BaseDelaySeconds = 1.0,
    BackoffMultiplier = 2.0,
    MaxDelaySeconds = 30.0,
    RetryOnTimeout = true,
    RetryOnRateLimit = true,
    RetryOnServerErrors = true
};
```

## 🏗️ Architecture

```
MercadoBitcoin.Client/
├── 📁 Public Data      → Public data (tickers, orderbook, trades, candles)
├── 📁 Account          → Account management (balances, tier, positions, fees)
├── 📁 Trading          → Trading operations (orders, executions, cancellations)
├── 📁 Wallet           → Wallet (deposits, withdrawals, addresses, limits)
└── 📁 Authentication   → Bearer Token authentication system
```

## 📊 Supported Endpoints

### 🔓 Public Data
| Endpoint | Method | Description |
|----------|---------|-----------|
| `/{asset}/fees` | GET | Asset withdrawal fees |
| `/{symbol}/orderbook` | GET | Order book |
| `/{symbol}/trades` | GET | Trade history |
| `/candles` | GET | Candlestick data (OHLCV) |
| `/symbols` | GET | Instrument information |
| `/tickers` | GET | Current prices |
| `/{asset}/networks` | GET | Available networks for the asset |

### 🔐 Account and Authentication
| Endpoint | Method | Description |
|----------|---------|-----------|
| `/authorize` | POST | Authentication via login/password |
| `/accounts` | GET | List user accounts |
| `/accounts/{accountId}/balances` | GET | Account balances |
| `/accounts/{accountId}/tier` | GET | Account fee tier |
| `/accounts/{accountId}/{symbol}/fees` | GET | Trading fees |
| `/accounts/{accountId}/positions` | GET | Open positions |

### 📈 Trading
| Endpoint | Method | Description |
|----------|---------|-----------|
| `/accounts/{accountId}/{symbol}/orders` | GET/POST | List/Create orders |
| `/accounts/{accountId}/{symbol}/orders/{orderId}` | GET/DELETE | Get/Cancel order |
| `/accounts/{accountId}/orders` | GET | All orders |
| `/accounts/{accountId}/cancel_all_open_orders` | DELETE | Cancel all open orders |

### 💰 Wallet
| Endpoint | Method | Description |
|----------|---------|-----------|
| `/accounts/{accountId}/wallet/{symbol}/deposits` | GET | Deposit history |
| `/accounts/{accountId}/wallet/{symbol}/deposits/addresses` | GET | Deposit addresses |
| `/accounts/{accountId}/wallet/fiat/{symbol}/deposits` | GET | Fiat deposits (BRL) |
| `/accounts/{accountId}/wallet/{symbol}/withdraw` | GET/POST | Get/Request withdrawals |
| `/accounts/{accountId}/wallet/{symbol}/withdraw/{withdrawId}` | GET | Get specific withdrawal |
| `/accounts/{accountId}/wallet/withdraw/config/limits` | GET | Withdrawal limits |
| `/accounts/{accountId}/wallet/withdraw/config/BRL` | GET | BRL withdrawal config |
| `/accounts/{accountId}/wallet/withdraw/addresses` | GET | Withdrawal addresses |
| `/accounts/{accountId}/wallet/withdraw/bank-accounts` | GET | Bank accounts |

## 💻 Usage Examples

### 📊 Public Data (no authentication)

```csharp
using MercadoBitcoin.Client.Extensions;
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Get list of all available symbols
var symbols = await client.GetSymbolsAsync();
Console.WriteLine($"Available symbols: {symbols.Symbol.Count}");

// Get Bitcoin ticker
var tickers = await client.GetTickersAsync("BTC-BRL");
var btcTicker = tickers.First();
Console.WriteLine($"BTC: R$ {btcTicker.Last}");

// Get order book
var orderBook = await client.GetOrderBookAsync("BTC-BRL", limit: "10");
Console.WriteLine($"Best bid: R$ {orderBook.Bids[0][0]}");
Console.WriteLine($"Best ask: R$ {orderBook.Asks[0][0]}");

// Get trade history
var trades = await client.GetTradesAsync("BTC-BRL", limit: 100);
Console.WriteLine($"Last {trades.Count} trades retrieved");

// Get candle/chart data
var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var candles = await client.GetCandlesAsync("BTC-BRL", "1h", (int)to, countback: 24);
Console.WriteLine($"OHLCV for the last 24 hours retrieved");
```

### 👤 Private Data (with authentication)

```csharp
using MercadoBitcoin.Client.Extensions;
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
await client.AuthenticateAsync("your_login", "your_password");

// Get account information
var accounts = await client.GetAccountsAsync();
var account = accounts.First();
Console.WriteLine($"Account: {account.Name} ({account.Currency})");

// Get balances
var balances = await client.GetBalancesAsync(account.Id);
foreach (var balance in balances)
{
    Console.WriteLine($"{balance.Symbol}: {balance.Available} (available) + {balance.On_hold} (reserved)");
}
```

### 📈 Trading

```csharp
var accountId = accounts.First().Id;

// Create limit buy order
var buyOrder = new PlaceOrderRequest
{
    Side = "buy",
    Type = "limit",
    Qty = "0.001",              // Amount in BTC
    LimitPrice = 280000,        // Limit price in R$
    ExternalId = "my-order-001"
};

var placedOrder = await authenticatedClient.PlaceOrderAsync("BTC-BRL", accountId, buyOrder);
Console.WriteLine($"Order created: {placedOrder.OrderId}");

// List open orders
var openOrders = await authenticatedClient.ListOrdersAsync("BTC-BRL", accountId, status: "working");
Console.WriteLine($"You have {openOrders.Count} open orders");

// Cancel an order
var cancelResult = await authenticatedClient.CancelOrderAsync(accountId, "BTC-BRL", placedOrder.OrderId);
Console.WriteLine($"Cancellation: {cancelResult.Status}");
```

## ⚡ Rate Limits

The library automatically respects Mercado Bitcoin API rate limits:

- **Public Data**: 1 request/second
- **Trading**: 3 requests/second (create/cancel), 10 requests/second (queries)
- **Account**: 3 requests/second
- **Wallet**: Varies by endpoint
- **Cancel All Orders**: 1 request/minute

## 🔒 Security

### Authentication
- Uses Mercado Bitcoin's **Bearer Token** system
- Tokens are automatically managed by the library
- Support for automatic token renewal

### Best Practices
- Never expose your API credentials in source code
- Use environment variables or Azure Key Vault for credentials
- Implement retry policies with exponential backoff
- Configure appropriate timeouts

```csharp
// ✅ Good
var apiKey = Environment.GetEnvironmentVariable("MB_API_KEY");
var apiSecret = Environment.GetEnvironmentVariable("MB_API_SECRET");
await client.AuthenticateAsync(apiKey, apiSecret);

// ❌ Bad
await client.AuthenticateAsync("hardcoded_key", "hardcoded_secret");
```

## ⚡ System.Text.Json and AOT Compatibility

### Migration Benefits

The library has been completely migrated from **Newtonsoft.Json** to **System.Text.Json** with **Source Generators**, offering:

#### 🚀 Performance
- **2x faster** in serialization/deserialization
- **50% less memory usage** during JSON operations
- **3x faster startup** with Source Generators
- **Zero reflection** at runtime

#### 📦 AOT Compatibility
- **Native AOT compilation** supported
- **Ultra-fast applications** with minimal startup time
- **Smaller footprint** on memory and disk
- **Better performance** in containerized environments

## 🛡️ Quality and Reliability

### 🧪 Quality Tests

The library has undergone rigorous quality tests ensuring:

#### ✅ **Complete Coverage**
- **64 tests** covering all API endpoints
- **100% of public endpoints** tested and validated
- **Private endpoints** with graceful authentication handling
- **Error scenarios** completely mapped and tested

#### 🚀 **Proven Performance**
- **Real benchmarks** with Mercado Bitcoin API data
- **Thresholds adjusted** based on production measurements
- **HTTP/3 vs HTTP/2 comparisons** with measurable results
- **Optimized memory usage** and validated

## 📈 Observability and Metrics

The library exposes metrics via `System.Diagnostics.Metrics` (.NET Instrumentation) which can be collected by OpenTelemetry, Prometheus (via exporter), or Application Insights.

### 🔢 Counters

| Instrument | Name | Type | Description | Tags |
|-------------|------|------|-----------|------|
| `_retryCounter` | `mb_client_http_retries` | Counter<long> | Number of retry attempts executed | `status_code` |
| `_circuitOpenCounter` | `mb_client_circuit_opened` | Counter<long> | Number of times the circuit opened | *(no tag)* |
| `_circuitHalfOpenCounter` | `mb_client_circuit_half_open` | Counter<long> | Number of transitions to half-open | *(no tag)* |
| `_circuitClosedCounter` | `mb_client_circuit_closed` | Counter<long> | Number of times the circuit closed after success | *(no tag)* |

### ⏱️ Histogram

| Instrument | Name | Type | Unit | Description | Tags |
|-------------|------|------|--------|-----------|------|
| `_requestDurationHistogram` | `mb_client_http_request_duration` | Histogram<double> | ms | Duration of HTTP requests (including retries) | `method`, `outcome`, `status_code` |

## 📘 Detailed Documentation

### For Humans 🧑‍💻
Complete guide with step-by-step instructions, code examples, best practices, and detailed explanations:
- **[User Guide (English)](docs/USER_GUIDE.md)**

### For AI Agents 🤖
For automated consumption (LLMs / agents), use specialized guides containing contracts, operational flows, prompts, and safety heuristics:
- **[AI Usage Guide (English)](docs/AI_USAGE_GUIDE.md)**

These documents are self-contained and optimized for programmatic interpretation (structures, decision tables, retry strategies, and parameter validation).

---

*Last update: November 2025 - Version 4.0.0-alpha.1 (Migration to .NET 10 & C# 14, HTTP/3 support, Polly v8, removal of public constructors, mandatory DI and extension methods, full AOT compatibility)*

[![GitHub stars](https://img.shields.io/github/stars/ernanesa/MercadoBitcoin.Client?style=social)](https://github.com/ernanesa/MercadoBitcoin.Client/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/ernanesa/MercadoBitcoin.Client?style=social)](https://github.com/ernanesa/MercadoBitcoin.Client/network/members)
[![NuGet Version](https://img.shields.io/nuget/v/MercadoBitcoin.Client.svg)](https://www.nuget.org/packages/MercadoBitcoin.Client) [![NuGet Downloads](https://img.shields.io/nuget/dt/MercadoBitcoin.Client.svg)](https://www.nuget.org/packages/MercadoBitcoin.Client)
