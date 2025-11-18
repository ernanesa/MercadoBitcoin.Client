````markdown
# Trading Operations - Mercado Bitcoin API v4

## 📋 Overview

Endpoints for trading operations (buy/sell cryptocurrencies).

**Rate Limits**:
- POST/DELETE (create/cancel): 3 req/s
- GET (query): 10 req/s
- Cancel All: 1 req/min

## 🎯 Order Types

| Type | Description | Use |
|------|-------------|-----|
| `market` | Executes immediately at best price | Fast execution, no price guarantee |
| `limit` | Executes at specified price or better | Price control, may not execute immediately |
| `stoplimit` | Stop loss + limit order | Protection against large drops |
| `post-only` | Maker-only (will not execute immediately) | Ensure maker fee |

## 📍 Endpoints

### 1. Place Order

**Endpoint**: `POST /accounts/{accountId}/{symbol}/orders`
**Rate Limit**: 3 req/s

```csharp
// Limit buy order
var request = new PlaceOrderRequest
{
    Side = "buy",
    Type = "limit",
    Qty = "0.001",
    LimitPrice = 340000,
    ExternalId = "my-order-001" // optional
};

var response = await client.PlaceOrderAsync("BTC-BRL", accountId, request);
Console.WriteLine($"Order created: {response.OrderId}");
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `side` | string | ✅ | `buy` or `sell` |
| `type` | string | ✅ | `market`, `limit`, `stoplimit`, `post-only` |
| `qty` | string | Conditional | Quantity (required except market buy) |
| `cost` | number | Conditional | Total cost (market buy only) |
| `limitPrice` | number | Conditional | Limit price (limit, stoplimit, post-only) |
| `stopPrice` | number | Conditional | Activation price (stoplimit) |
| `externalId` | string | ❌ | Custom ID |
| `async` | boolean | ❌ | Create asynchronously (default: false) |

#### Examples

```csharp
// Market Buy (by cost in BRL)
var marketBuy = new PlaceOrderRequest
{
    Side = "buy",
    Type = "market",
    Cost = 1000 // R$ 1000
};

// Market Sell (by quantity)
var marketSell = new PlaceOrderRequest
{
    Side = "sell",
    Type = "market",
    Qty = "0.001"
};

// Stop Limit (stop loss)
var stopLoss = new PlaceOrderRequest
{
    Side = "sell",
    Type = "stoplimit",
    Qty = "0.001",
    StopPrice = 335000,  // activates when price falls to R$ 335k
    LimitPrice = 334000  // sells at R$ 334k or better
};

// Post-only (ensure maker fee)
var postOnly = new PlaceOrderRequest
{
    Side = "buy",
    Type = "post-only",
    Qty = "0.001",
    LimitPrice = 339000
};
```

---

### 2. Get Order

**Endpoint**: `GET /accounts/{accountId}/{symbol}/orders/{orderId}`
**Rate Limit**: 10 req/s

```csharp
var order = await client.GetOrderAsync("BTC-BRL", accountId, orderId);

Console.WriteLine($"Status: {order.Status}");
Console.WriteLine($"Filled: {order.FilledQty} of {order.Qty}");
Console.WriteLine($"Avg price: R$ {order.AvgPrice}");
```

#### Possible Statuses

| Status | Description |
|--------|-------------|
| `created` | Created, awaiting processing |
| `working` | Active, partially or not executed |
| `cancelled` | Cancelled |
| `filled` | Fully executed |

---

### 3. List Orders

**Endpoint**: `GET /accounts/{accountId}/{symbol}/orders`
**Rate Limit**: 10 req/s

```csharp
// Open orders
var openOrders = await client.ListOrdersAsync("BTC-BRL", accountId, status: "working");

// Filled orders
var filledOrders = await client.ListOrdersAsync("BTC-BRL", accountId, status: "filled");

// With date filters
var orders = await client.ListOrdersAsync(
    "BTC-BRL",
    accountId,
    created_at_from: "1699900000",
    created_at_to: "1699999999",
    side: "buy"
);
```

#### Filters

| Parameter | Type | Description |
|-----------|------|-------------|
| `has_executions` | string | `true`/`false` |
| `side` | string | `buy`/`sell` |
| `status` | string | `created`, `working`, `cancelled`, `filled` |
| `id_from` | string | From ID |
| `id_to` | string | To ID |
| `created_at_from` | string | Start timestamp |
| `created_at_to` | string | End timestamp |
| `executed_at_from` | string | Execution start timestamp |
| `executed_at_to` | string | Execution end timestamp |

---

### 4. List All Orders

**Endpoint**: `GET /accounts/{accountId}/orders`
**Rate Limit**: 3 req/s

```csharp
// All orders (all symbols)
var allOrders = await client.ListAllOrdersAsync(
    accountId,
    status: "working,filled",
    size: "50"
);

foreach (var order in allOrders.Items)
{
    Console.WriteLine($"{order.Side} {order.Qty} {order.Instrument} @ {order.LimitPrice}");
}
```

---

### 5. Cancel Order

**Endpoint**: `DELETE /accounts/{accountId}/{symbol}/orders/{orderId}`
**Rate Limit**: 3 req/s

```csharp
// Synchronous cancel (waits for confirmation)
var result = await client.CancelOrderAsync(accountId, "BTC-BRL", orderId, async: false);

if (result.Status == "cancelled")
{
    Console.WriteLine("Order cancelled successfully");
}

// Asynchronous cancel (faster)
var result = await client.CancelOrderAsync(accountId, "BTC-BRL", orderId, async: true);
// Status may be "queued_to_cancel"
```

---

### 6. Cancel All Open Orders

**Endpoint**: `DELETE /accounts/{accountId}/cancel_all_open_orders`
**Rate Limit**: 1 req/min ⚠️

```csharp
// Cancel all open orders
var results = await client.CancelAllOpenOrdersByAccountAsync(accountId);

Console.WriteLine($"{results.Count} orders cancelled");

// Filter by symbol
var results = await client.CancelAllOpenOrdersByAccountAsync(
    accountId,
    symbol: "BTC-BRL"
);

// Filter by execution state
var results = await client.CancelAllOpenOrdersByAccountAsync(
    accountId,
    has_executions: false // Only orders with no execution
);
```

⚠️ **WARNING**: 1 req/min rate limit. Use sparingly.

---

## 🎨 Trading Strategies

### 1. Basic Market Making

```csharp
public class BasicMarketMaker
{
    private readonly decimal _spread = 0.002m; // 0.2%
    
    public async Task RunAsync(string symbol, decimal midPrice)
    {
        var buyPrice = midPrice * (1 - _spread / 2);
        var sellPrice = midPrice * (1 + _spread / 2);
        
        // Place orders
        var buyOrder = await client.PlaceOrderAsync(symbol, accountId, new PlaceOrderRequest
        {
            Side = "buy",
            Type = "post-only",
            Qty = "0.001",
            LimitPrice = (int)buyPrice
        });
        
        var sellOrder = await client.PlaceOrderAsync(symbol, accountId, new PlaceOrderRequest
        {
            Side = "sell",
            Type = "post-only",
            Qty = "0.001",
            LimitPrice = (int)sellPrice
        });
    }
}
```

### 2. Dynamic Stop Loss

```csharp
public class TrailingStopLoss
{
    private decimal _highestPrice;
    private readonly decimal _trailPercent = 0.05m; // 5%
    
    public async Task MonitorAsync(string symbol, string buyOrderId)
    {
        var buyOrder = await client.GetOrderAsync(symbol, accountId, buyOrderId);
        _highestPrice = (decimal)buyOrder.AvgPrice;
        
        while (true)
        {
            var ticker = (await client.GetTickersAsync(symbol)).First();
            var currentPrice = decimal.Parse(ticker.Last);
            
            if (currentPrice > _highestPrice)
            {
                _highestPrice = currentPrice;
            }
            
            var stopPrice = _highestPrice * (1 - _trailPercent);
            
            if (currentPrice <= stopPrice)
            {
                // Sell
                await client.PlaceOrderAsync(symbol, accountId, new PlaceOrderRequest
                {
                    Side = "sell",
                    Type = "market",
                    Qty = buyOrder.FilledQty
                });
                break;
            }
            
            await Task.Delay(1000);
        }
    }
}
```

### 3. DCA (Dollar Cost Averaging)

```csharp
public class DCAStrategy
{
    private readonly decimal _investmentAmount = 100m; // R$ 100 per buy
    private readonly TimeSpan _interval = TimeSpan.FromDays(7); // Weekly
    
    public async Task ExecuteAsync(string symbol)
    {
        while (true)
        {
            try
            {
                await client.PlaceOrderAsync(symbol, accountId, new PlaceOrderRequest
                {
                    Side = "buy",
                    Type = "market",
                    Cost = (double)_investmentAmount
                });
                
                Console.WriteLine($"DCA buy executed: R$ {_investmentAmount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DCA error: {ex.Message}");
            }
            
            await Task.Delay(_interval);
        }
    }
}
```

### 4. Grid Trading

```csharp
public class GridTrading
{
    private readonly int _gridLevels = 10;
    private readonly decimal _gridSpacing = 0.01m; // 1%
    
    public async Task SetupGridAsync(string symbol, decimal basePrice)
    {
        var orders = new List<Task<PlaceOrderResponse>>();
        
        for (int i = 1; i <= _gridLevels; i++)
        {
            // Buy orders below price
            var buyPrice = basePrice * (1 - _gridSpacing * i);
            orders.Add(client.PlaceOrderAsync(symbol, accountId, new PlaceOrderRequest
            {
                Side = "buy",
                Type = "limit",
                Qty = "0.001",
                LimitPrice = (int)buyPrice
            }));
            
            // Sell orders above price
            var sellPrice = basePrice * (1 + _gridSpacing * i);
            orders.Add(client.PlaceOrderAsync(symbol, accountId, new PlaceOrderRequest
            {
                Side = "sell",
                Type = "limit",
                Qty = "0.001",
                LimitPrice = (int)sellPrice
            }));
        }
        
        await Task.WhenAll(orders);
        Console.WriteLine($"Grid created: {orders.Count} orders");
    }
}
```

---

## 🔍 Order Monitoring

```csharp
public class OrderMonitor
{
    public async Task MonitorOrderAsync(string symbol, string orderId)
    {
        while (true)
        {
            var order = await client.GetOrderAsync(symbol, accountId, orderId);
            
            Console.WriteLine($"Status: {order.Status}");
            Console.WriteLine($"Filled: {order.FilledQty}/{order.Qty}");
            Console.WriteLine($"Avg price: R$ {order.AvgPrice:N2}");
            
            if (order.Status == "filled" || order.Status == "cancelled")
            {
                Console.WriteLine("Order finished");
                break;
            }
            
            await Task.Delay(2000);
        }
    }
}
```

---

## ⚠️ Validations and Best Practices

### Pre-Order Validation

```csharp
public async Task<bool> ValidateOrderAsync(PlaceOrderRequest request, string symbol)
{
    // 1. Check valid symbols
    var symbols = await client.GetSymbolsAsync(symbol);
    var symbolInfo = symbols.Symbol.Select((s, i) => new { Symbol = s, Index = i })
        .First(s => s.Symbol == symbol);
    
    // 2. Validate quantity
    var minVol = decimal.Parse(symbols.MinVolume[symbolInfo.Index]);
    var maxVol = decimal.Parse(symbols.MaxVolume[symbolInfo.Index]);
    var qty = decimal.Parse(request.Qty ?? "0");
    
    if (qty < minVol || qty > maxVol)
    {
        Console.WriteLine($"Invalid quantity. Min: {minVol}, Max: {maxVol}");
        return false;
    }
    
    // 3. Validate price
    if (request.Type != "market")
    {
        var minPrice = decimal.Parse(symbols.MinPrice[symbolInfo.Index]);
        var maxPrice = decimal.Parse(symbols.MaxPrice[symbolInfo.Index]);
        
        if (request.LimitPrice < (double)minPrice || request.LimitPrice > (double)maxPrice)
        {
            Console.WriteLine($"Invalid price. Min: {minPrice}, Max: {maxPrice}");
            return false;
        }
    }
    
    // 4. Check balance
    var balances = await client.GetBalancesAsync(accountId);
    var parts = symbol.Split('-');
    var quoteBalance = balances.FirstOrDefault(b => b.Symbol == parts[1]);
    var available = decimal.Parse(quoteBalance?.Available ?? "0");
    
    var cost = qty * (decimal)(request.LimitPrice ?? 0);
    if (cost > available)
    {
        Console.WriteLine($"Insufficient balance. Available: {available}");
        return false;
    }
    
    return true;
}
```

### Strategic Rate Limiting

```csharp
// Prioritize GET over POST/DELETE
public class PrioritizedRateLimiter
{
    private readonly SemaphoreSlim _readSemaphore = new(10, 10); // 10 req/s
    private readonly SemaphoreSlim _writeSemaphore = new(3, 3);  // 3 req/s
    
    public async Task<T> ExecuteReadAsync<T>(Func<Task<T>> action)
    {
        await _readSemaphore.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _ = Task.Delay(100).ContinueWith(_ => _readSemaphore.Release());
        }
    }
    
    public async Task<T> ExecuteWriteAsync<T>(Func<Task<T>> action)
    {
        await _writeSemaphore.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _ = Task.Delay(334).ContinueWith(_ => _writeSemaphore.Release()); // ~3/s
        }
    }
}
```

---

## ✅ Checklist

- [ ] Implement order creation (market, limit, stoplimit)
- [ ] Implement get single order
- [ ] Implement list orders with filters
- [ ] Implement cancel order
- [ ] Implement cancel all (with caution)
- [ ] Pre-order validation
- [ ] Execution monitoring
- [ ] Trading strategies
- [ ] Differentiated rate limiting
- [ ] Specific error handling

**Next**: [05-OPERACOES-CARTEIRA.md](05-OPERACOES-CARTEIRA.md)

````
