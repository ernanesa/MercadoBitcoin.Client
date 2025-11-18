````markdown
# Private (Authenticated) Endpoints - Mercado Bitcoin API v4

## 📋 Overview

Private endpoints **require authentication** via Bearer Token and provide access to:
- 👤 Account information
- 💰 Balances and positions
- 📊 Customized trading fees
- 🎚️ Fee tiers

**Rate Limit**: 3 requests/second
**Authentication**: ✅ Bearer Token required

## 🔐 Authentication

### Authentication Flow

```
1. Client calls AuthenticateAsync(login, password)
2. API returns access_token + expiration
3. Token is stored in AuthHttpClient
4. Token is automatically injected into all private requests
5. Header: Authorization: Bearer <token>
```

### Implementation

```csharp
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Authenticate (obtain token)
await client.AuthenticateAsync("your_api_id", "your_api_secret");

// Now you can call private endpoints
var accounts = await client.GetAccountsAsync();
var balances = await client.GetBalancesAsync(accounts.First().Id);
```

### Security

```csharp
// ✅ CORRECT: Environment variables
var apiId = Environment.GetEnvironmentVariable("MB_API_ID");
var apiSecret = Environment.GetEnvironmentVariable("MB_API_SECRET");
await client.AuthenticateAsync(apiId!, apiSecret!);

// ✅ CORRECT: Azure Key Vault
var apiId = await keyVault.GetSecretAsync("MB-API-ID");
var apiSecret = await keyVault.GetSecretAsync("MB-API-SECRET");
await client.AuthenticateAsync(apiId, apiSecret);

// ❌ NEVER DO: Hardcoded credentials
await client.AuthenticateAsync("hardcoded_id", "hardcoded_secret");
```

---

## 📍 Available Endpoints

### 1. Authorize - Obtain Token

**Endpoint**: `POST /authorize`
**Rate Limit**: Not specified (use sparingly)
**Authentication**: ❌ Not required (authentication endpoint)

#### Description
Authenticates the user and returns a Bearer access token.

#### Request Body

```json
{
  "login": "your_api_token_id",
  "password": "your_api_token_secret"
}
```

#### Response

```json
{
  "access_token": "01GF442ATTVP4M6M0XGHQYT544",
  "expiration": 1666116857
}
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `access_token` | string | Bearer token for authentication |
| `expiration` | integer | Unix timestamp (UTC) of expiration |

#### Usage Example

```csharp
// Method encapsulated by the client
await client.AuthenticateAsync(apiId, apiSecret);

// Check token
var token = client.GetAccessToken();
Console.WriteLine($"Token: {token?.Substring(0, 10)}...");

// Compute expiration time (commonly 1 hour)
// Implement auto-refresh if needed
```

---

### 2. Accounts - List Accounts

**Endpoint**: `GET /accounts`
**Rate Limit**: 3 req/s
**Authentication**: ✅ Required

#### Description
Lists all user accounts. Typically Mercado Bitcoin uses a single default account.

#### Response

```json
[
  {
    "id": "a322205ace882ef800553118e5000066",
    "name": "Mercado Bitcoin",
    "type": "live",
    "currency": "BRL",
    "currencySign": "R$"
  }
]
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique account ID (accountId) |
| `name` | string | Account name |
| `type` | string | Account type (live, demo) |
| `currency` | string | Account base currency |
| `currencySign` | string | Currency symbol |

#### Usage Example

```csharp
// List accounts
var accounts = await client.GetAccountsAsync();
var mainAccount = accounts.First();

Console.WriteLine($"Account: {mainAccount.Name}");
Console.WriteLine($"ID: {mainAccount.Id}");
Console.WriteLine($"Type: {mainAccount.Type}");
Console.WriteLine($"Currency: {mainAccount.Currency}");

// Cache accountId for later use
var accountId = mainAccount.Id;
```

#### Optimizations
- ✅ **Cache**: AccountId rarely changes, cache per session
- ✅ **Validation**: Verify account is `live` before trading

---

### 3. Balances - Account Balances

**Endpoint**: `GET /accounts/{accountId}/balances`
**Rate Limit**: 3 req/s
**Authentication**: ✅ Required

#### Description
Returns balances of all assets in the account, including fiat (BRL).

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `accountId` | string (path) | ✅ Yes | Account ID |

#### Response

```json
[
  {
    "symbol": "BRL",
    "available": "10000.00000000",
    "on_hold": "500.00000000",
    "total": "10500.00000000"
  },
  {
    "symbol": "BTC",
    "available": "0.50000000",
    "on_hold": "0.01000000",
    "total": "0.51000000"
  }
]
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `symbol` | string | Asset symbol (BTC, BRL, ETH, etc) |
| `available` | string | Available balance |
| `on_hold` | string | Reserved balance (open orders) |
| `total` | string | Total balance (available + on_hold) |

#### Usage Example

```csharp
// Get all balances
var balances = await client.GetBalancesAsync(accountId);

// Print balances
foreach (var balance in balances)
{
    Console.WriteLine($"{balance.Symbol}:");
    Console.WriteLine($"  Available: {balance.Available}");
    Console.WriteLine($"  On hold: {balance.On_hold}");
    Console.WriteLine($"  Total: {balance.Total}");
}

// Check specific balance
var btcBalance = balances.FirstOrDefault(b => b.Symbol == "BTC");
if (btcBalance != null)
{
    var available = decimal.Parse(btcBalance.Available);
    if (available >= 0.001m)
    {
        Console.WriteLine("Sufficient BTC balance to trade");
    }
}

// Compute total net worth in BRL
var tickers = await client.GetTickersAsync("BTC-BRL,ETH-BRL");
decimal totalBRL = 0;

foreach (var balance in balances)
{
    if (balance.Symbol == "BRL")
    {
        totalBRL += decimal.Parse(balance.Total);
    }
    else
    {
        var ticker = tickers.FirstOrDefault(t => t.Pair == $"{balance.Symbol}-BRL");
        if (ticker != null)
        {
            var amount = decimal.Parse(balance.Total);
            var price = decimal.Parse(ticker.Last);
            totalBRL += amount * price;
        }
    }
}

Console.WriteLine($"Total net worth: R$ {totalBRL:N2}");
```

#### Optimizations
- ✅ **Polling**: Update every 5–10 seconds during operation
- ✅ **Caching**: Cache 1–2 seconds for multiple queries
- ✅ **Filters**: API lacks native filters; filter client-side

---

### 4. Tier - Fee Tier

**Endpoint**: `GET /accounts/{accountId}/tier`
**Rate Limit**: 3 req/s
**Authentication**: ✅ Required

#### Description
Returns the account fee tier. Higher tiers yield lower fees.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `accountId` | string (path) | ✅ Yes | Account ID |

#### Response

```json
[
  {
    "tier": "5"
  }
]
```

#### Tiers and Fees

The exchange uses tier-based fees based on 30-day trading volume:

| Tier | 30d Volume (BRL) | Maker Fee | Taker Fee |
|------|------------------|-----------|-----------|
| 1 | 0 - 50k | 0.70% | 0.70% |
| 2 | 50k - 500k | 0.60% | 0.70% |
| 3 | 500k - 2M | 0.50% | 0.70% |
| 4 | 2M - 5M | 0.40% | 0.65% |
| 5 | 5M - 10M | 0.30% | 0.60% |
| 6 | 10M+ | Negotiable | Negotiable |

#### Usage Example

```csharp
// Get current tier
var tierResponse = await client.GetTierAsync(accountId);
var tier = int.Parse(tierResponse.First().Tier);

Console.WriteLine($"Current tier: {tier}");

// Estimate maker fee (simplified)
var estimatedMakerFee = tier switch
{
    1 => 0.70m,
    2 => 0.60m,
    3 => 0.50m,
    4 => 0.40m,
    5 => 0.30m,
    _ => 0.25m
};

Console.WriteLine($"Estimated maker fee: {estimatedMakerFee}%");

// For exact fees, query trading fees endpoint
```

#### Optimizations
- ✅ **Cache**: Tier rarely changes, cache for 1 hour
- ✅ **Validation**: Use tier to estimate costs

---

### 5. Trading Fees - Symbol Fees

**Endpoint**: `GET /accounts/{accountId}/{symbol}/fees`
**Rate Limit**: 3 req/s
**Authentication**: ✅ Required

#### Description
Returns exact trading fees (maker and taker) for a specific symbol.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `accountId` | string (path) | ✅ Yes | Account ID |
| `symbol` | string (path) | ✅ Yes | BASE-QUOTE pair |

#### Response

```json
{
  "base": "BTC",
  "quote": "BRL",
  "maker_fee": "0.00300000",
  "taker_fee": "0.00600000"
}
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `base` | string | Base currency |
| `quote` | string | Quote currency |
| `maker_fee` | string | Maker fee (decimal) |
| `taker_fee` | string | Taker fee (decimal) |

#### Maker vs Taker

| Type | Description | Fee |
|------|-------------|-----|
| **Maker** | Adds liquidity (limit order that does not match immediately) | Lower |
| **Taker** | Removes liquidity (market or matching limit order) | Higher |

#### Usage Example

```csharp
// Get fees for BTC-BRL
var fees = await client.GetTradingFeesAsync(accountId, "BTC-BRL");

var makerFee = decimal.Parse(fees.Maker_fee) * 100;
var takerFee = decimal.Parse(fees.Taker_fee) * 100;

Console.WriteLine($"Fees for {fees.Base}-{fees.Quote}:");
Console.WriteLine($"  Maker: {makerFee:F2}%");
Console.WriteLine($"  Taker: {takerFee:F2}%");

// Compute order cost
var orderValue = 10000m; // R$ 10,000
var makerCost = orderValue * decimal.Parse(fees.Maker_fee);
var takerCost = orderValue * decimal.Parse(fees.Taker_fee);

Console.WriteLine($"Maker order cost: R$ {makerCost:N2}");
Console.WriteLine($"Taker order cost: R$ {takerCost:N2}");

// Batch for multiple symbols
var symbols = new[] { "BTC-BRL", "ETH-BRL", "USDC-BRL" };
var feesTasks = symbols.Select(s => client.GetTradingFeesAsync(accountId, s));
var feesResults = await Task.WhenAll(feesTasks);
```

#### Optimizations
- ✅ **Cache**: Fees change rarely, cache for 1 hour
- ✅ **Batch**: Query symbols in parallel
- ✅ **Validation**: Use to compute costs before placing orders

---

### 6. Positions - Open Positions

**Endpoint**: `GET /accounts/{accountId}/positions`
**Rate Limit**: 1 req/s
**Authentication**: ✅ Required

#### Description
Returns open positions (partially or unfilled orders) for the account.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `accountId` | string (path) | ✅ Yes | Account ID |
| `symbols` | string (query) | ❌ No | Filter by symbols (comma-separated) |

#### Response

```json
[
  {
    "id": "27",
    "instrument": "BTC-BRL",
    "side": "buy",
    "category": "limit",
    "qty": "0.001",
    "avgPrice": 345000.50
  }
]
```

#### Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Order ID |
| `instrument` | string | Trading pair |
| `side` | string | Order side (buy/sell) |
| `category` | string | Type (limit/stoplimit/post-only) |
| `qty` | string | Quantity |
| `avgPrice` | number | Average execution price |

#### Usage Example

```csharp
// List open positions
var positions = await client.GetPositionsAsync(accountId);

Console.WriteLine($"Open positions: {positions.Count}");
foreach (var pos in positions)
{
    Console.WriteLine($"{pos.Side.ToUpper()} {pos.Qty} {pos.Instrument} @ R$ {pos.AvgPrice:N2}");
}

// Filter by symbols
var btcPositions = await client.GetPositionsAsync(accountId, "BTC-BRL,ETH-BRL");

// Compute total value of positions
decimal totalValue = 0;
foreach (var pos in positions)
{
    var qty = decimal.Parse(pos.Qty);
    var value = qty * (decimal)pos.AvgPrice;
    totalValue += value;
}

Console.WriteLine($"Total positions value: R$ {totalValue:N2}");

// Identify P&L
var tickers = await client.GetTickersAsync(
    string.Join(",", positions.Select(p => p.Instrument))
);

foreach (var pos in positions)
{
    var ticker = tickers.FirstOrDefault(t => t.Pair == pos.Instrument);
    if (ticker != null)
    {
        var currentPrice = decimal.Parse(ticker.Last);
        var avgPrice = (decimal)pos.AvgPrice;
        var pnl = pos.Side == "buy" 
            ? (currentPrice - avgPrice) / avgPrice * 100
            : (avgPrice - currentPrice) / avgPrice * 100;
        
        Console.WriteLine($"{pos.Instrument}: {pnl:F2}% {(pnl > 0 ? "📈" : "📉")}");
    }
}
```

#### Optimizations
- ✅ **Filters**: Specify symbols to reduce payload
- ✅ **Polling**: Update every 5–10 seconds
- ✅ **Caching**: Cache 2–5 seconds for multiple queries

---

## 🔄 Typical Usage Flow

### 1. Initialization

```csharp
// Create client
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Authenticate
var apiId = Environment.GetEnvironmentVariable("MB_API_ID");
var apiSecret = Environment.GetEnvironmentVariable("MB_API_SECRET");
await client.AuthenticateAsync(apiId!, apiSecret!);

// Get account
var accounts = await client.GetAccountsAsync();
var accountId = accounts.First().Id;

Console.WriteLine($"✅ Connected to account: {accounts.First().Name}");
```

### 2. Balance Monitoring

```csharp
public class BalanceMonitor
{
    private readonly MercadoBitcoinClient _client;
    private readonly string _accountId;
    
    public async Task MonitorAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var balances = await _client.GetBalancesAsync(_accountId, ct);
                
                // Filter balances > 0
                var activeBalances = balances
                    .Where(b => decimal.Parse(b.Total) > 0)
                    .ToList();
                
                Console.Clear();
                Console.WriteLine("=== BALANCES ===");
                foreach (var balance in activeBalances)
                {
                    Console.WriteLine($"{balance.Symbol}: {balance.Available} (Total: {balance.Total})");
                }
                
                await Task.Delay(5000, ct); // Refresh every 5s
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await Task.Delay(10000, ct);
            }
        }
    }
}
```

### 3. Account Dashboard

```csharp
public class AccountDashboard
{
    private readonly MercadoBitcoinClient _client;
    private readonly string _accountId;
    
    public async Task<Dashboard> GetDashboardAsync()
    {
        // Fetch data in parallel
        var balancesTask = _client.GetBalancesAsync(_accountId);
        var tierTask = _client.GetTierAsync(_accountId);
        var positionsTask = _client.GetPositionsAsync(_accountId);
        
        await Task.WhenAll(balancesTask, tierTask, positionsTask);
        
        var balances = await balancesTask;
        var tier = await tierTask;
        var positions = await positionsTask;
        
        // Compute metrics
        var activeBalances = balances.Where(b => decimal.Parse(b.Total) > 0).ToList();
        var totalAssets = activeBalances.Count;
        var openPositions = positions.Count;
        
        return new Dashboard
        {
            Tier = int.Parse(tier.First().Tier),
            TotalAssets = totalAssets,
            OpenPositions = openPositions,
            Balances = activeBalances,
            Positions = positions
        };
    }
}
```

### 4. Pre-Trade Validation

```csharp
public class PreTradeValidator
{
    public async Task<ValidationResult> ValidateAsync(
        string accountId,
        string symbol,
        decimal qty,
        decimal price)
    {
        // 1. Check balances
        var balances = await _client.GetBalancesAsync(accountId);
        var parts = symbol.Split('-');
        var @base = parts[0];
        var quote = parts[1];
        
        var baseBalance = balances.FirstOrDefault(b => b.Symbol == @base);
        var quoteBalance = balances.FirstOrDefault(b => b.Symbol == quote);
        
        // 2. Get symbol info
        var symbols = await _client.GetSymbolsAsync(symbol);
        var minVolume = decimal.Parse(symbols.MinVolume[0]);
        var maxVolume = decimal.Parse(symbols.MaxVolume[0]);
        var minCost = decimal.Parse(symbols.MinCost[0]);
        
        // 3. Validate limits
        var cost = qty * price;
        
        if (qty < minVolume)
            return ValidationResult.Fail($"Minimum volume: {minVolume}");
        
        if (qty > maxVolume)
            return ValidationResult.Fail($"Maximum volume: {maxVolume}");
        
        if (cost < minCost)
            return ValidationResult.Fail($"Minimum cost: R$ {minCost}");
        
        // 4. Check available balance
        var available = decimal.Parse(quoteBalance?.Available ?? "0");
        if (cost > available)
            return ValidationResult.Fail($"Insufficient balance. Available: R$ {available}");
        
        // 5. Compute fee
        var fees = await _client.GetTradingFeesAsync(accountId, symbol);
        var fee = cost * decimal.Parse(fees.Taker_fee);
        var totalCost = cost + fee;
        
        return ValidationResult.Success(totalCost, fee);
    }
}
```

---

## 🚨 Error Handling

### Common Errors

| Code | Description | Solution |
|------|-------------|---------|
| `FORBIDDEN` | Invalid or expired token | Re-authenticate |
| `INVALID_ACCOUNT` | Invalid accountId | Verify account |
| `INSUFFICIENT_BALANCE` | Insufficient funds | Check balance beforehand |
| `REQUEST_RATE_EXCEEDED` | Rate limit exceeded | Wait and retry |

### Implementation

```csharp
try
{
    var balances = await client.GetBalancesAsync(accountId);
}
catch (MercadoBitcoinUnauthorizedException ex)
{
    // Token expired - re-authenticate
    Console.WriteLine("Token expired, re-authenticating...");
    await client.AuthenticateAsync(apiId, apiSecret);
    
    // Retry
    var balances = await client.GetBalancesAsync(accountId);
}
catch (MercadoBitcoinRateLimitException ex)
{
    // Rate limit - wait
    var retryAfter = ex.RetryAfter ?? TimeSpan.FromSeconds(60);
    Console.WriteLine($"Rate limit. Waiting {retryAfter.TotalSeconds}s...");
    await Task.Delay(retryAfter);
}
catch (MercadoBitcoinApiException ex)
{
    // Generic API error
    Console.WriteLine($"API error: {ex.Code} - {ex.Message}");
}
```

---

## 📊 Rate Limiting and Best Practices

### Rate Limiting Strategies

```csharp
// 1. Global rate limiter (client already implements this)
var options = new MercadoBitcoinClientOptions
{
    RequestsPerSecond = 3 // 3 req/s for private endpoints
};

// 2. Batch queries
var tasks = new List<Task>
{
    client.GetBalancesAsync(accountId),
    client.GetTierAsync(accountId),
    client.GetPositionsAsync(accountId)
};
await Task.WhenAll(tasks);

// 3. Smart caching
private Dictionary<string, (DateTime, object)> _cache = new();

public async Task<T> GetCachedAsync<T>(string key, Func<Task<T>> fetcher, TimeSpan ttl)
{
    if (_cache.TryGetValue(key, out var cached))
    {
        if (DateTime.UtcNow - cached.Item1 < ttl)
            return (T)cached.Item2;
    }
    
    var result = await fetcher();
    _cache[key] = (DateTime.UtcNow, result!);
    return result;
}

// Usage:
var balances = await GetCachedAsync(
    $"balances_{accountId}",
    () => client.GetBalancesAsync(accountId),
    TimeSpan.FromSeconds(5)
);
```

### Recommended TTL

| Endpoint | Recommended TTL | Reason |
|----------|-----------------|--------|
| Accounts | 1 hour | Rarely changes |
| Balances | 2-5 seconds | Changes with orders |
| Tier | 1 hour | Changes slowly |
| Trading Fees | 1 hour | Depends on tier |
| Positions | 5 seconds | Changes with executions |

---

## ✅ Implementation Checklist

### Authentication
- [ ] Implement authentication flow
- [ ] Store token securely
- [ ] Implement automatic token refresh
- [ ] Handle token expiration

### Basic Endpoints
- [ ] List accounts
- [ ] Get balances
- [ ] Query tier
- [ ] Get trading fees
- [ ] List open positions

### Optimizations
- [ ] Configurable TTL cache
- [ ] Parallel batching of queries
- [ ] Client-side rate limiting
- [ ] Automatic retry on failures

### Monitoring
- [ ] Account dashboard
- [ ] Real-time balance monitor
- [ ] Low-balance alerts
- [ ] API usage metrics

---

**Next**: [04-TRADING-OPERATIONS.md](04-TRADING-OPERATIONS.md)

````
