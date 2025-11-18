# Public Endpoints - Mercado Bitcoin API v4

## 📋 Overview

Public endpoints **do not require authentication** and provide real‑time market data. They are ideal for:
- 📊 Price monitoring
- 📈 Technical analysis and charting
- 📉 Historical data
- 🔍 Instrument information
- 💰 Available fees and networks

**Rate Limit**: 1 request/second per endpoint  
**Global Limit**: 500 requests/minute (shared with private endpoints)

## 🎯 Available Endpoints

### 1. Tickers - Current Prices

**Endpoint**: `GET /tickers`  
**Rate Limit**: 1 req/s  
**Authentication**: ❌ Not required

#### Description
Returns current prices and market statistics for one or more trading pairs.

#### Parameters

| Parameter | Type | Required | Description | Example |
|----------|------|----------|-------------|---------|
| `symbols` | string | ✅ Yes | Pair(s) in BASE-QUOTE format | `BTC-BRL` or `BTC-BRL,ETH-BRL` |

#### Response

```json
[
  {
    "pair": "BTC-BRL",
    "high": "350000.00000000",
    "low": "340000.00000000",
    "vol": "125.50000000",
    "last": "345000.00000000",
    "buy": "344900.00000000",
    "sell": "345100.00000000",
    "open": "342000.00000000",
    "date": 1699999999
  }
]
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `pair` | string | Trading pair (BASE-QUOTE) |
| `high` | string | Highest price in the last 24h |
| `low` | string | Lowest price in the last 24h |
| `vol` | string | Total volume traded in 24h (in BASE) |
| `last` | string | Last traded price |
| `buy` | string | Best current bid |
| `sell` | string | Best current ask |
| `open` | string | Opening price (24h ago) |
| `date` | integer | Unix timestamp (UTC) of last update |

#### Usage Example

```csharp
// Single symbol
var tickers = await client.GetTickersAsync("BTC-BRL");
Console.WriteLine($"BTC: R$ {tickers.First().Last}");

// Multiple symbols (more efficient)
var tickers = await client.GetTickersAsync("BTC-BRL,ETH-BRL,USDC-BRL");
foreach (var ticker in tickers)
{
    Console.WriteLine($"{ticker.Pair}: R$ {ticker.Last}");
}
```

#### Optimizations
- ✅ **Batching**: Request multiple symbols in a single call
- ✅ **Caching**: Cache for 1–5 seconds for non‑critical data
- ✅ **Filtering**: Request only the symbols you actually need

---

### 2. OrderBook - Order Book

**Endpoint**: `GET /{symbol}/orderbook`  
**Rate Limit**: 1 req/s  
**Authentication**: ❌ Not required

#### Description
Returns the order book (depth of market) with buy orders (bids) and sell orders (asks).

#### Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `symbol` | string | ✅ Yes | Pair in BASE-QUOTE format | `BTC-BRL` |
| `limit` | string | ❌ No | Limit of orders per side (max 1000) | `"50"` |

#### Response

```json
{
  "timestamp": 1699999999,
  "bids": [
    ["344900.00000000", "0.50000000"],
    ["344800.00000000", "1.20000000"]
  ],
  "asks": [
    ["345100.00000000", "0.75000000"],
    ["345200.00000000", "2.00000000"]
  ]
}
```

#### Structure

- **bids**: Array of `[price, quantity]` ordered by price DESC  
- **asks**: Array of `[price, quantity]` ordered by price ASC  
- **timestamp**: Unix timestamp when the order book was generated

#### Usage Example

```csharp
// Full order book (default: top 100)
var orderBook = await client.GetOrderBookAsync("BTC-BRL");

// Optimized order book (top 10)
var orderBook = await client.GetOrderBookAsync("BTC-BRL", limit: "10");

// Best bid and ask
var bestBid = decimal.Parse(orderBook.Bids[0][0]);
var bestAsk = decimal.Parse(orderBook.Asks[0][0]);
var spread = bestAsk - bestBid;

Console.WriteLine($"Bid: R$ {bestBid:N2} | Ask: R$ {bestAsk:N2} | Spread: R$ {spread:N2}");

// Total depth
var totalBidVolume = orderBook.Bids.Sum(b => decimal.Parse(b[1]));
var totalAskVolume = orderBook.Asks.Sum(a => decimal.Parse(a[1]));
```

#### Optimizations
- ✅ **Reduced Limit**: Use `limit` to reduce payload (e.g., 10–50 levels)
- ✅ **Smart Polling**: Refresh only when needed
- ✅ **WebSocket**: For real‑time updates, consider WebSocket (if available)

---

### 3. Trades - Recent Trades

**Endpoint**: `GET /{symbol}/trades`  
**Rate Limit**: 1 req/s  
**Authentication**: ❌ Not required

#### Description
Lists recent trades (executions) for a pair.

#### Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `symbol` | string | ✅ Yes | BASE-QUOTE pair | `BTC-BRL` |
| `tid` | integer | ❌ No | Specific trade ID | `12345` |
| `since` | integer | ❌ No | Starting trade ID | `12340` |
| `from` | integer | ❌ No | Initial Unix timestamp (UTC) | `1699900000` |
| `to` | integer | ❌ No | Final Unix timestamp (UTC) | `1699999999` |
| `limit` | integer | ❌ No | Result limit (max 1000) | `100` |

#### Response

```json
[
  {
    "tid": 12345,
    "date": 1699999999,
    "type": "buy",
    "price": "345000.00",
    "amount": "0.001"
  }
]
```

#### Usage Example

```csharp
// Last 100 trades
var trades = await client.GetTradesAsync("BTC-BRL", limit: 100);

// Trades in a time range
var from = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var trades = await client.GetTradesAsync("BTC-BRL", from: (int)from, to: (int)to);

// Volume analysis
var buyVolume = trades.Where(t => t.Type == "buy").Sum(t => decimal.Parse(t.Amount));
var sellVolume = trades.Where(t => t.Type == "sell").Sum(t => decimal.Parse(t.Amount));
var buyPressure = buyVolume / (buyVolume + sellVolume) * 100;

Console.WriteLine($"Buy pressure: {buyPressure:F2}%");
```

#### Optimizations
- ✅ **Pagination**: Use `since` to fetch trades incrementally
- ✅ **Time Ranges**: Combine `from` and `to` for specific periods
- ✅ **Limit**: Adjust `limit` as needed (default 100)

---

### 4. Candles - OHLCV Data

**Endpoint**: `GET /candles`  
**Rate Limit**: 1 req/s  
**Authentication**: ❌ Not required

#### Description
Returns historical OHLCV candles for technical analysis and charting.

#### Parameters

| Parameter | Type | Required | Description | Allowed Values |
|-----------|------|----------|-------------|----------------|
| `symbol` | string | ✅ Yes | BASE-QUOTE pair | `BTC-BRL` |
| `resolution` | string | ✅ Yes | Candle timeframe | `1m`, `15m`, `1h`, `3h`, `1d`, `1w`, `1M` |
| `to` | integer | ✅ Yes | Final timestamp (UTC) | Unix timestamp |
| `from` | integer | ❌ No | Initial timestamp (UTC) | Unix timestamp |
| `countback` | integer | ❌ No | Number of candles (priority over `from`) | e.g., `100` |

#### Available Resolutions

| Resolution | Description | Typical Use |
|------------|-------------|-------------|
| `1m` | 1 minute | Scalping, intraday |
| `15m` | 15 minutes | Day trading |
| `1h` | 1 hour | Swing trading |
| `3h` | 3 hours | Mid‑term analysis |
| `1d` | 1 day | Position trading |
| `1w` | 1 week | Long‑term analysis |
| `1M` | 1 month | Macro trends |

#### Response

```json
{
  "t": [1699900000, 1699903600, 1699907200],
  "o": ["344000.00", "345000.00", "344500.00"],
  "h": ["345500.00", "346000.00", "345000.00"],
  "l": ["343500.00", "344000.00", "344000.00"],
  "c": ["345000.00", "344500.00", "344800.00"],
  "v": ["12.5", "15.3", "10.8"]
}
```

#### Fields

| Field | Description |
|-------|-------------|
| `t` | Timestamps (candle open time) |
| `o` | Open prices |
| `h` | High prices |
| `l` | Low prices |
| `c` | Close prices |
| `v` | Volumes |

#### Usage Example

```csharp
// Last 24 hours (24 candles of 1h)
var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var candles = await client.GetCandlesAsync("BTC-BRL", "1h", (int)to, countback: 24);

// Trend analysis
var closes = candles.C.Select(decimal.Parse).ToList();
var sma20 = closes.TakeLast(20).Average();
var currentPrice = closes.Last();
var trend = currentPrice > sma20 ? "Bullish" : "Bearish";

Console.WriteLine($"Price: R$ {currentPrice:N2} | SMA(20): R$ {sma20:N2} | Trend: {trend}");

// Volatility
var volatility = candles.H.Zip(candles.L, (h, l) => (decimal.Parse(h) - decimal.Parse(l)) / decimal.Parse(l) * 100).Average();
Console.WriteLine($"Average volatility: {volatility:F2}%");
```

#### Optimizations
- ✅ **Countback**: More efficient than `from`/`to` for fixed windows
- ✅ **Resolution**: Use higher resolutions for long periods
- ✅ **Caching**: Cache historical candles (they are immutable)
- ✅ **Batching**: Fetch multiple symbols in parallel

---

### 5. Symbols - Instrument Information

**Endpoint**: `GET /symbols`  
**Rate Limit**: 1 req/s  
**Authentication**: ❌ Not required

#### Description
Returns detailed information about available trading instruments.

#### Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `symbols` | string | ❌ No | Filter by specific symbols | `BTC-BRL,ETH-BRL` |

#### Response

```json
{
  "symbol": ["BTC-BRL", "ETH-BRL"],
  "base-currency": ["BTC", "ETH"],
  "currency": ["BRL", "BRL"],
  "description": ["Bitcoin", "Ethereum"],
  "type": ["CRYPTO", "CRYPTO"],
  "exchange-listed": [true, true],
  "exchange-traded": [true, true],
  "minmovement": ["0.00000001", "0.00000001"],
  "pricescale": [100000000, 100000000],
  "session-regular": ["24x7", "24x7"],
  "timezone": ["America/Sao_Paulo", "America/Sao_Paulo"],
  "min-price": ["1000.00000000", "100.00000000"],
  "max-price": ["10000000.00000000", "1000000.00000000"],
  "min-volume": ["0.00000100", "0.00001000"]
}
```

#### Usage Example

```csharp
var symbols = await client.GetSymbolsAsync();

foreach (var symbol in symbols)
{
    Console.WriteLine($"{symbol.Symbol} - {symbol.Description} (min vol: {symbol.BaseMinSize})");
}
```

---

### 6. Asset Fees - Withdrawal Fees

**Endpoint**: `GET /{asset}/fees`  
**Rate Limit**: 1 req/s  
**Authentication**: ❌ Not required

#### Description
Returns withdrawal (network) fees for a specific asset.

#### Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `asset` | string | ✅ Yes | Asset (BASE) | `BTC` |
| `network` | string | ❌ No | Specific network | `bitcoin`, `ethereum` |

#### Response

```json
{
  "asset": "BTC",
  "network": "bitcoin",
  "deposit_minimum": "0.00001",
  "deposit_confirmations_required": "3",
  "withdraw_minimum": "0.0001",
  "withdrawal_fee": "0.00005"
}
```

#### Usage Example

```csharp
// Default Bitcoin fees
var fees = await client.GetAssetFeesAsync("BTC");
Console.WriteLine($"BTC withdraw fee: {fees.Withdrawal_fee}");

// USDC fees on Ethereum network
var fees = await client.GetAssetFeesAsync("USDC", network: "ethereum");
Console.WriteLine($"USDC (ETH) withdraw fee: {fees.Withdrawal_fee}");

// Net withdrawal amount
var withdrawAmount = 0.01m;
var fee = decimal.Parse(fees.Withdrawal_fee);
var netAmount = withdrawAmount - fee;
Console.WriteLine($"Net: {netAmount} BTC");
```

---

### 7. Asset Networks - Available Networks

**Endpoint**: `GET /{asset}/networks`  
**Rate Limit**: 1 req/s  
**Authentication**: ❌ Not required

#### Description
Returns available blockchain networks for an asset (important for multichain assets such as USDC, USDT).

#### Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `asset` | string | ✅ Yes | Asset (BASE) | `USDC` |

#### Response

```json
[
  {
    "coin": "USDC",
    "network": "ethereum"
  },
  {
    "coin": "USDC",
    "network": "stellar"
  }
]
```

#### Usage Example

```csharp
// List available networks
var networks = await client.GetAssetNetworksAsync("USDC");
foreach (var network in networks)
{
    Console.WriteLine($"{network.Coin} available on {network.Network} network");
}

// Check if specific network is available
var hasEthereum = networks.Any(n => n.Network == "ethereum");
if (hasEthereum)
{
    Console.WriteLine("USDC available on Ethereum network");
}
```

---

## 🚀 Optimization Strategies

### 1. Batching

```csharp
// ❌ Inefficient: 3 requests
var btcTicker = await client.GetTickersAsync("BTC-BRL");
var ethTicker = await client.GetTickersAsync("ETH-BRL");
var usdcTicker = await client.GetTickersAsync("USDC-BRL");

// ✅ Efficient: 1 request
var tickers = await client.GetTickersAsync("BTC-BRL,ETH-BRL,USDC-BRL");
```

### 2. Parallelism

```csharp
// Fetch data for multiple symbols in parallel
var tasks = new[]
{
    client.GetTickersAsync("BTC-BRL"),
    client.GetOrderBookAsync("BTC-BRL", limit: "20"),
    client.GetTradesAsync("BTC-BRL", limit: 50)
};

var results = await Task.WhenAll(tasks);
```

