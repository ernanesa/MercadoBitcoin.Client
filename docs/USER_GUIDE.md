# MercadoBitcoin.Client - User Guide

Welcome to the **MercadoBitcoin.Client** library! This guide is designed to help you integrate your .NET applications with the Mercado Bitcoin API v4 efficiently and reliably.

## 🌟 Key Features

*   **High Performance**: Built on .NET 10, using **HTTP/2** by default with connection pooling and multiplexing.
*   **Memory Efficient**: Uses `System.Text.Json` with Source Generators and **SIMD-accelerated** math for candle analysis.
*   **Resilient**: Built-in **Retry Policies**, **Circuit Breaker**, and **Jitter** to handle network instability.
*   **Developer Friendly**: Strong typing, comprehensive XML documentation, and intuitive method overloads.

---

## 🚀 Getting Started

### 1. Installation

Install the package via NuGet:

```bash
dotnet add package MercadoBitcoin.Client
```

### 2. Configuration (Dependency Injection)

The recommended way to use the client is via Dependency Injection (DI) in your `Program.cs`:

```csharp
using MercadoBitcoin.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add MercadoBitcoin Client
builder.Services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = "https://api.mercadobitcoin.net/api/v4";
    options.HttpConfiguration.HttpVersion = new Version(2, 0); // Use HTTP/2
    options.RetryPolicyConfig.MaxRetryAttempts = 3;
});

var app = builder.Build();
```

### 3. Manual Instantiation (Console Apps / Scripts)

For simple scripts or console applications, use the factory methods:

```csharp
using MercadoBitcoin.Client.Extensions;

// Create a client with default retry policies and HTTP/2
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Or optimized for trading (lower latency)
var tradingClient = MercadoBitcoinClientExtensions.CreateForTrading();
```

---

## 💡 Usage Examples

### 1. Public Data (No Authentication)

Fetch tickers, order books, and trades without credentials.

**Batch Fetching (New in v3.0):**
You can now fetch multiple tickers or symbols in a single request!

```csharp
// Fetch multiple tickers at once
var symbols = new[] { "BTC-BRL", "ETH-BRL", "USDC-BRL" };
var tickers = await client.GetTickersAsync(symbols);

foreach (var ticker in tickers)
{
    Console.WriteLine($"{ticker.Pair}: R$ {ticker.Last}");
}
```

**Candle Analysis (SIMD Optimized):**
The library includes SIMD-accelerated extensions for analyzing candle data.

```csharp
using MercadoBitcoin.Client.Extensions;

// Fetch last 100 candles
var candles = await client.GetRecentCandlesTypedAsync("BTC-BRL", "1h", 100);

// Ultra-fast calculations using AVX2 instructions
var avgClose = candles.CalculateAverageClose();
var maxHigh = candles.CalculateMaxHigh();

Console.WriteLine($"Average Close: {avgClose} | Max High: {maxHigh}");
```

### 2. Private Data (Authentication Required)

To access account data or trade, you must authenticate first.

```csharp
// 1. Authenticate
await client.AuthenticateAsync("YOUR_API_ID", "YOUR_API_SECRET");

// 2. Get Account ID
var accounts = await client.GetAccountsAsync();
var accountId = accounts.First().Id;

// 3. Check Balances
var balances = await client.GetBalancesAsync(accountId);
var brl = balances.FirstOrDefault(b => b.Symbol == "BRL");
Console.WriteLine($"Available BRL: {brl?.Available}");
```

### 3. Trading

Placing orders is simple and strongly typed.

```csharp
var orderRequest = new PlaceOrderRequest
{
    Side = "buy",
    Type = "limit",
    Qty = "0.001",       // Amount of BTC
    LimitPrice = 350000, // Price in BRL
    ExternalId = Guid.NewGuid().ToString() // Idempotency key
};

var result = await client.PlaceOrderAsync("BTC-BRL", accountId, orderRequest);
Console.WriteLine($"Order Placed: {result.OrderId}");
```

---

## ⚙️ Advanced Configuration

### HTTP/2 & Connection Pooling
The library uses a custom `SocketsHttpHandler` optimized for high throughput:
*   **Multiplexing**: Multiple requests over a single TCP connection.
*   **Pooling**: Connections are pooled for 5 minutes to prevent socket exhaustion.
*   **Compression**: GZip/Brotli enabled by default.

You don't need to do anything to enable this; it's the default behavior.

### Resilience (Polly)
The client includes a robust resilience pipeline:
*   **Retries**: Automatically retries on 429 (Rate Limit), 408 (Timeout), and 5xx (Server Errors).
*   **Circuit Breaker**: Temporarily stops requests if too many failures occur, protecting your system and the API.
*   **Jitter**: Adds random delays to retries to prevent "thundering herd" problems.

**Customizing Retry Policy:**

```csharp
var customConfig = new RetryPolicyConfig
{
    MaxRetryAttempts = 5,
    BaseDelaySeconds = 0.5,
    EnableCircuitBreaker = true
};

var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies(customConfig);
```

---

## ❓ FAQ

**Q: Do I need to handle Rate Limits manually?**
A: No, the library automatically handles `429 Too Many Requests` responses and respects the `Retry-After` header.

**Q: Is it thread-safe?**
A: Yes, `MercadoBitcoinClient` is designed to be a singleton or scoped service. You should reuse the same instance across your application to benefit from connection pooling.

**Q: How do I debug HTTP requests?**
A: Set the environment variable `MB_TRACE_HTTP=1` to see detailed request/response logs in the console.

---

## 📚 Reference

*   [Official API Documentation](https://api.mercadobitcoin.net/api/v4/docs)
*   [GitHub Repository](https://github.com/ernanesa/MercadoBitcoin.Client)
