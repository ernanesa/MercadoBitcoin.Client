# 📚 Quick Reference Guide - Mercado Bitcoin API

> **Last update**: 2024  
> **Library version**: 3.0.0  
> **Mercado Bitcoin API**: v4

---

## 🎯 What is this library?

Complete and optimized .NET client for integrating with Mercado Bitcoin API v4.

### ✨ Key Features

- ✅ **HTTP/2** with multiplexing and HPACK compression
- ✅ **System.Text.Json** with Source Generators (AOT-friendly, 2x faster)
- ✅ **Retry Policies** (Polly: exponential backoff + jitter)
- ✅ **Manual Circuit Breaker** with metrics
- ✅ **Hierarchical Rate Limiting** per endpoint
- ✅ **Async/await** across the entire API
- ✅ **64 tests** (unit + integration + E2E)
- ✅ **.NET 9** + C# 13 (nullable enabled, init-only)

---

## 🚀 Quick Start (5 minutes)

### 1. Installation

```bash
dotnet add package MercadoBitcoin.Client
```

### 2. Configuration

```bash
# User Secrets (development)
dotnet user-secrets set "MercadoBitcoin:ApiId" "your_api_id"
dotnet user-secrets set "MercadoBitcoin:ApiSecret" "your_api_secret"
```

### 3. First Code Sample

```csharp
using MercadoBitcoin.Client;

// Create client with retry policies
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Public endpoints (no authentication)
var tickers = await client.GetTickersAsync("BTC-BRL");
Console.WriteLine($"BTC Price: R$ {tickers[0].Last}");

// Authenticate for private endpoints
var apiId = Environment.GetEnvironmentVariable("MB_API_ID")!;
var apiSecret = Environment.GetEnvironmentVariable("MB_API_SECRET")!;
await client.AuthenticateAsync(apiId, apiSecret);

// Get balances
var accounts = await client.GetAccountsAsync();
var accountId = accounts.First().Id;

var balances = await client.GetBalancesAsync(accountId);
foreach (var balance in balances)
{
    Console.WriteLine($"{balance.Symbol}: {balance.Available}");
}
```

---

## 📖 Documents Index

| # | Document | Description |
|---|----------|-------------|
| 01 | [Architecture Overview](01-ARCHITECTURE-OVERVIEW.md) | Components, HTTP/2, metrics |
| 02 | [Public Endpoints](02-PUBLIC-ENDPOINTS.md) | Tickers, order book, trades, candles |
| 03 | [Private Endpoints](03-PRIVATE-ENDPOINTS.md) | Authentication, accounts, balances |
| 04 | [Trading Operations](04-TRADING-OPERATIONS.md) | Create/cancel orders, strategies |
| 05 | [Wallet Operations](05-WALLET-OPERATIONS.md) | Deposits, withdrawals, limits |
| 06 | [Performance and Optimization](06-PERFORMANCE-AND-OPTIMIZATION.md) | Batching, cache, pooling |
| 07 | [Error Handling](07-ERROR-HANDLING.md) | Retry, circuit breaker, fallbacks |
| 08 | [Security and Best Practices](08-SECURITY-BEST-PRACTICES.md) | Credentials, TLS, validation |
| 09 | [Testing and Validation](09-TESTING-AND-VALIDATION.md) | Unit, integration, E2E, load tests |
| 10 | [Implementation Roadmap](10-IMPLEMENTATION-ROADMAP.md) | Phases, timeline, prioritization |

---

## 🔥 Common Recipes

### Real-Time Price Monitor

```csharp
while (true)
{
    var ticker = (await client.GetTickersAsync("BTC-BRL")).First();
    var price = decimal.Parse(ticker.Last);
    var change24h = decimal.Parse(ticker.High) - decimal.Parse(ticker.Low);
    
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] BTC: R$ {price:N2} (Δ24h: R$ {change24h:N2})");
    
    await Task.Delay(TimeSpan.FromSeconds(5));
}
```

### Create Order with Validation

```csharp
// 1. Validate symbol
var symbols = await client.GetSymbolsAsync();
var symbol = symbols.FirstOrDefault(s => s.Symbol == "BTC-BRL");
if (symbol == null)
    throw new InvalidOperationException("Symbol not found");

// 2. Check balance
var balances = await client.GetBalancesAsync(accountId);
var brlBalance = decimal.Parse(balances.First(b => b.Symbol == "BRL").Available);

// 3. Calculate quantity
var price = 350000m;
var quantityBTC = (brlBalance * 0.1m) / price; // 10% of balance

// 4. Validate limits
if (quantityBTC < decimal.Parse(symbol.BaseMinSize))
    throw new InvalidOperationException($"Minimum quantity: {symbol.BaseMinSize}");

// 5. Create order
var order = await client.PlaceOrderAsync("BTC-BRL", accountId, new()
{
    Side = "buy",
    Type = "limit",
    Qty = quantityBTC.ToString("F8"),
    LimitPrice = price,
    ExternalId = $"order_{Guid.NewGuid()}"
});

Console.WriteLine($"Order created: {order.OrderId}");
```

### DCA (Dollar Cost Averaging)

```csharp
public async Task ExecuteDCAAsync(string symbol, decimal amountBRL, TimeSpan interval)
{
    while (true)
    {
        try
        {
            var ticker = (await client.GetTickersAsync(symbol)).First();
            var currentPrice = decimal.Parse(ticker.Last);
            var quantity = amountBRL / currentPrice;
            
            var order = await client.PlaceOrderAsync(symbol, accountId, new()
            {
                Side = "buy",
                Type = "market",
                Qty = quantity.ToString("F8")
            });
            
            _logger.LogInformation("DCA executed: {Symbol} {Qty} @ {Price}",
                symbol, quantity, currentPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DCA");
        }
        
        await Task.Delay(interval);
    }
}

// Usage: invest R$ 100 every week
await ExecuteDCAAsync("BTC-BRL", 100m, TimeSpan.FromDays(7));
```

### Grid Trading

```csharp
public class GridTrader
{
    private readonly decimal _lowerPrice = 300000m;
    private readonly decimal _upperPrice = 400000m;
    private readonly int _levels = 10;
    
    public async Task RunAsync()
    {
        var gridStep = (_upperPrice - _lowerPrice) / _levels;
        
        // Create buy orders at lower levels
        for (int i = 0; i < _levels / 2; i++)
        {
            var price = _lowerPrice + (i * gridStep);
            await client.PlaceOrderAsync("BTC-BRL", accountId, new()
            {
                Side = "buy",
                Type = "limit",
                Qty = "0.001",
                LimitPrice = price
            });
        }
        
        // Create sell orders at higher levels
        for (int i = _levels / 2; i < _levels; i++)
        {
            var price = _lowerPrice + (i * gridStep);
            await client.PlaceOrderAsync("BTC-BRL", accountId, new()
            {
                Side = "sell",
                Type = "limit",
                Qty = "0.001",
                LimitPrice = price
            });
        }
    }
}
```

---

## 🛡️ Robust Error Handling

```csharp
public class ResilientTrader
{
    public async Task<PlaceOrderResponse?> PlaceOrderWithRetryAsync(
        string symbol,
        PlaceOrderRequest request,
        int maxAttempts = 3)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await client.PlaceOrderAsync(symbol, accountId, request);
            }
            catch (MercadoBitcoinApiException ex) when (ex.ErrorCode == "INSUFFICIENT_BALANCE")
            {
                _logger.LogError("Insufficient balance. Aborting.");
                return null; // Not retriable
            }
            catch (MercadoBitcoinApiException ex) when (ex.ErrorCode == "RATE_LIMIT_EXCEEDED")
            {
                var waitTime = ex.RetryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning("Rate limit. Waiting {Wait}s...", waitTime.TotalSeconds);
                await Task.Delay(waitTime);
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Network error. Attempt {Attempt}/{Max}",
                    attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
            }
        }
        
        _logger.LogError("All attempts failed");
        return null;
    }
}
```

---

## 📊 Metrics and Observability

### Basic Instrumentation

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

var meter = new Meter("MyTradingApp");
var orderCounter = meter.CreateCounter<long>("orders_placed");
var orderDuration = meter.CreateHistogram<double>("order_duration_ms");

// When placing an order
var sw = Stopwatch.StartNew();
try
{
    var order = await client.PlaceOrderAsync(...);
    orderCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
}
catch (Exception)
{
    orderCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));
    throw;
}
finally
{
    orderDuration.Record(sw.Elapsed.TotalMilliseconds);
}
```

### Export to Prometheus

```csharp
// Program.cs
using OpenTelemetry.Metrics;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
        metrics.AddMeter("MyTradingApp");
        metrics.AddMeter("MercadoBitcoin.Client"); // Library metrics
    });

app.MapPrometheusScrapingEndpoint(); // /metrics
```

---

## 🔧 Advanced Configuration

### Custom HttpClient

```csharp
var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(15),
    MaxConnectionsPerServer = 100,
    EnableMultipleHttp2Connections = true,
    AutomaticDecompression = DecompressionMethods.All
};

var httpClient = new HttpClient(new RetryHandler(handler))
{
    BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4"),
    DefaultRequestVersion = HttpVersion.Version20,
    Timeout = TimeSpan.FromSeconds(30)
};

var client = new MercadoBitcoinClient(httpClient, new AuthHttpClient());
```

### Custom Rate Limiting

```csharp
var rateLimiter = new AsyncRateLimiter(maxRequestsPerSecond: 5);

// Wrapper for all calls
async Task<T> WithRateLimitAsync<T>(Func<Task<T>> action)
{
    await rateLimiter.WaitAsync();
    return await action();
}

var tickers = await WithRateLimitAsync(() => client.GetTickersAsync("BTC-BRL"));
```

---

## 🐛 Troubleshooting

### Issue: `401 Unauthorized`

```csharp
// Check if token has expired
// Token valid for 30 minutes, but proactively renew at 25 minutes

private DateTime _tokenExpiration;

if (DateTime.UtcNow >= _tokenExpiration.AddMinutes(-5))
{
    await client.AuthenticateAsync(apiId, apiSecret);
    _tokenExpiration = DateTime.UtcNow.AddMinutes(30);
}
```

### Issue: `429 Rate Limit Exceeded`

```csharp
// Respect Retry-After header
catch (MercadoBitcoinApiException ex) when (ex.ErrorCode == "RATE_LIMIT_EXCEEDED")
{
    var retryAfter = ex.RetryAfter ?? TimeSpan.FromSeconds(60);
    _logger.LogWarning("Rate limit. Pausing for {Seconds}s", retryAfter.TotalSeconds);
    await Task.Delay(retryAfter);
}
```

### Issue: Slow Serialization

```csharp
// Ensure Source Generators are being used
var options = new JsonSerializerOptions
{
    TypeInfoResolver = MercadoBitcoinJsonSerializerContext.Default
};

var json = JsonSerializer.Serialize(data, options);
var obj = JsonSerializer.Deserialize<TickerResponse>(json, options);
```

---

## 📚 External Resources

### Official API
- **Documentation**: https://www.mercadobitcoin.com.br/api-doc/
- **Status**: https://status.mercadobitcoin.com.br/
- **Support**: api@mercadobitcoin.com.br

### .NET and Libraries
- **Polly**: https://github.com/App-vNext/Polly
- **System.Text.Json**: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json
- **HTTP/2**: https://learn.microsoft.com/aspnet/core/fundamentals/http-requests

### Financial Markets
- **TradingView**: https://www.tradingview.com/
- **CoinGecko API**: https://www.coingecko.com/api/documentation
- **Alpha Vantage**: https://www.alphavantage.co/

---

## ✅ Production Checklist

### Before Deploy
- [ ] Credentials in Key Vault/Secrets Manager
- [ ] Rate limiting configured
- [ ] Retry policies enabled
- [ ] Circuit breaker active
- [ ] Structured logs
- [ ] Metrics exported
- [ ] Health checks configured
- [ ] Load tests executed

### Monitoring
- [ ] Dashboard with key metrics
- [ ] Alerts for critical errors
- [ ] Alerts for rate limit
- [ ] Alerts for open circuit breaker
- [ ] Centralized logs

### Security
- [ ] TLS 1.3 enabled
- [ ] API keys with minimal permissions
- [ ] Key rotation scheduled
- [ ] Audit for sensitive operations
- [ ] IP whitelisting (if available)

---

## 🎓 Next Steps

1. **Read**: [01-ARCHITECTURE-OVERVIEW.md](01-ARCHITECTURE-OVERVIEW.md)
2. **Implement**: Quick Start above
3. **Explore**: Common recipes
4. **Test**: Create a test order
5. **Optimize**: Implement batching and cache
6. **Monitor**: Configure metrics
7. **Scale**: Follow full roadmap

---

**Built with 🚀 for maximum performance and reliability**

*Full documentation: 10+ markdown files, 40+ code samples, 100+ pages*

