```markdown
# Performance and Optimization - Mercado Bitcoin API

## 🚀 HTTP/2: Foundation of Performance

### Benefits

| Feature | HTTP/1.1 | HTTP/2 | Gain |
|---------|----------|--------|------:|
| Connections per request | 1 | 1 (multiplexed) | -80% overhead |
| Header compression | ❌ | ✅ HPACK | -30% bandwidth |
| Latency (100 req) | ~2.3s | ~0.8s | +65% |
| Memory (100 req) | ~50MB | ~32MB | -36% |

### Optimized Configuration

```csharp
var config = new HttpConfiguration
{
    HttpVersion = new Version(2, 0),
    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
    MaxConnectionsPerServer = 100,
    EnableCompression = true,
    TimeoutSeconds = 30
};
```

## ⚡ Optimization Strategies

### 1. Batching - Group Requests

```csharp
// ❌ Inefficient: 3 separate requests
var btc = await client.GetTickersAsync("BTC-BRL");
var eth = await client.GetTickersAsync("ETH-BRL");
var usdc = await client.GetTickersAsync("USDC-BRL");

// ✅ Efficient: 1 request
var tickers = await client.GetTickersAsync("BTC-BRL,ETH-BRL,USDC-BRL");
```

### 2. Parallelism - Independent Requests

```csharp
// Fetch unrelated data in parallel
var tasks = new[]
{
    client.GetTickersAsync("BTC-BRL"),
    client.GetOrderBookAsync("BTC-BRL", limit: "10"),
    client.GetBalancesAsync(accountId)
};

var results = await Task.WhenAll(tasks);
```

### 3. Smart Caching

```csharp
public class SmartCache<T>
{
    private readonly Dictionary<string, (DateTime, T)> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public async Task<T> GetOrFetchAsync(
        string key,
        Func<Task<T>> fetcher,
        TimeSpan ttl)
    {
        await _lock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                if (DateTime.UtcNow - cached.Item1 < ttl)
                {
                    return cached.Item2;
                }
            }
            
            var result = await fetcher();
            _cache[key] = (DateTime.UtcNow, result);
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }
}

// Usage
var cache = new SmartCache<ListSymbolInfoResponse>();
var symbols = await cache.GetOrFetchAsync(
    "symbols",
    () => client.GetSymbolsAsync(),
    TimeSpan.FromHours(1)
);
```

### Recommended TTLs

| Data | TTL | Reason |
|------|-----|--------|
| Symbols | 1h | Changes rarely |
| Asset Fees | 24h | Mostly static |
| Tickers | 1-5s | Real-time |
| OrderBook | 0-1s | Highly volatile |
| Balances | 2-5s | Changes with orders |
| Tier | 1h | Changes slowly |

### 4. Connection Pooling

```csharp
// Configure IHttpClientFactory
services.AddHttpClient<MercadoBitcoinClient>(client =>
{
    client.DefaultRequestVersion = new Version(2, 0);
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
    MaxConnectionsPerServer = 100
});
```

### 5. Compression

```csharp
var handler = new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
};
```

Reduces payload by ~70% for large JSON responses.

### 6. Streaming for Large Volumes

```csharp
public async IAsyncEnumerable<TradeResponse> StreamTradesAsync(
    string symbol,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    int? lastTid = null;
    
    while (!ct.IsCancellationRequested)
    {
        var trades = await client.GetTradesAsync(symbol, since: lastTid, limit: 1000, ct);
        
        foreach (var trade in trades)
        {
            yield return trade;
        }
        
        if (trades.Any())
        {
            lastTid = trades.Max(t => t.Tid);
        }
        else
        {
            await Task.Delay(1000, ct);
        }
    }
}
```

### 7. Prefetching - Anticipate Needs

```csharp
public class PrefetchingClient
{
    private Task<List<TickerResponse>>? _tickersTask;
    private Task<ListSymbolInfoResponse>? _symbolsTask;
    
    public void Prefetch()
    {
        _tickersTask = client.GetTickersAsync("BTC-BRL,ETH-BRL");
        _symbolsTask = client.GetSymbolsAsync();
    }
    
    public async Task<List<TickerResponse>> GetTickersAsync()
    {
        if (_tickersTask == null)
        {
            return await client.GetTickersAsync("BTC-BRL,ETH-BRL");
        }
        
        var result = await _tickersTask;
        _tickersTask = null; // Invalidate cache
        return result;
    }
}
```

## 📊 Optimized Rate Limiting

### Hierarchical Rate Limiter

```csharp
public class HierarchicalRateLimiter
{
    private readonly AsyncRateLimiter _globalLimiter = new(8);  // 500/min ≈ 8/s
    private readonly AsyncRateLimiter _publicLimiter = new(1);   // 1/s
    private readonly AsyncRateLimiter _privateLimiter = new(3);  // 3/s
    private readonly AsyncRateLimiter _readLimiter = new(10);    // 10/s
    
    public async Task<T> ExecutePublicAsync<T>(Func<Task<T>> action)
    {
        await _globalLimiter.WaitAsync();
        await _publicLimiter.WaitAsync();
        return await action();
    }
    
    public async Task<T> ExecutePrivateReadAsync<T>(Func<Task<T>> action)
    {
        await _globalLimiter.WaitAsync();
        await _privateLimiter.WaitAsync();
        await _readLimiter.WaitAsync();
        return await action();
    }
    
    public async Task<T> ExecutePrivateWriteAsync<T>(Func<Task<T>> action)
    {
        await _globalLimiter.WaitAsync();
        await _privateLimiter.WaitAsync();
        return await action();
    }
}
```

### Request Prioritization

```csharp
public class PriorityQueue
{
    private readonly SemaphoreSlim _semaphore = new(3, 3);
    private readonly PriorityQueue<Func<Task>, int> _queue = new();
    
    public async Task<T> EnqueueAsync<T>(Func<Task<T>> action, int priority)
    {
        var tcs = new TaskCompletionSource<T>();
        _queue.Enqueue(async () =>
        {
            try
            {
                var result = await action();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, priority);
        
        _ = ProcessQueueAsync();
        return await tcs.Task;
    }
    
    private async Task ProcessQueueAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_queue.TryDequeue(out var action, out _))
            {
                await action();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

// Usage
// Priority 1 = High (trading)
// Priority 5 = Normal (queries)
// Priority 10 = Low (background)

await priorityQueue.EnqueueAsync(() => client.PlaceOrderAsync(...), priority: 1);
await priorityQueue.EnqueueAsync(() => client.GetTickersAsync(...), priority: 5);
```

## 🎯 Specific Optimizations

### Optimized OrderBook

```csharp
// Only top 10 levels (≈90% less data)
var orderBook = await client.GetOrderBookAsync("BTC-BRL", limit: "10");

// Calculate only what's necessary
var spread = decimal.Parse(orderBook.Asks[0][0]) - decimal.Parse(orderBook.Bids[0][0]);
var midPrice = (decimal.Parse(orderBook.Asks[0][0]) + decimal.Parse(orderBook.Bids[0][0])) / 2;
```

### Efficient Candles

```csharp
// Use countback instead of from/to
var candles = await client.GetCandlesAsync(
    "BTC-BRL",
    "1h",
    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    countback: 24  // More efficient
);

// Cache historical candles (they are immutable)
var cacheKey = $"candles_{symbol}_{resolution}_{from}_{to}";
```

### Specific Filters

```csharp
// ❌ Fetch everything and filter locally
var allSymbols = await client.GetSymbolsAsync();
var btc = allSymbols.Symbol.Where(s => s.Contains("BTC"));

// ✅ Filter on server
var symbols = await client.GetSymbolsAsync("BTC-BRL,BTC-USDC");
```

## 📈 Benchmarks and Metrics

### Performance Monitoring

```csharp
public class PerformanceMonitor
{
    private readonly Histogram<double> _latencyHistogram;
    private readonly Counter<long> _requestCounter;
    
    public async Task<T> MeasureAsync<T>(
        string operation,
        Func<Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            _latencyHistogram.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", operation),
                new KeyValuePair<string, object?>("status", "success"));
            _requestCounter.Add(1);
            return result;
        }
        catch (Exception ex)
        {
            _latencyHistogram.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", operation),
                new KeyValuePair<string, object?>("status", "error"));
            throw;
        }
    }
}
```

### Typical Benchmarks

```
Endpoint            | p50    | p95    | p99
--------------------|--------|--------|--------
GetTickers (1)      | 85ms   | 150ms  | 250ms
GetTickers (10)     | 95ms   | 180ms  | 300ms
GetOrderBook (10)   | 75ms   | 140ms  | 230ms
GetOrderBook (100)  | 120ms  | 220ms  | 380ms
PlaceOrder          | 180ms  | 350ms  | 600ms
GetBalances         | 95ms   | 170ms  | 280ms
```

## 🔧 Production Configuration

```csharp
services.AddMercadoBitcoinClient(options =>
{
    // HTTP/2 optimized
    options.HttpVersion = new Version(2, 0);
    options.TimeoutSeconds = 30;
    
    // Conservative rate limiting
    options.RequestsPerSecond = 5;
    
    // Optimized retry
    options.MaxRetryAttempts = 3;
    options.BaseDelaySeconds = 1.0;
    options.BackoffMultiplier = 2.0;
    options.EnableJitter = true;
    options.JitterMillisecondsMax = 250;
    
    // Circuit breaker
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailuresBeforeBreaking = 8;
    options.CircuitBreakerDurationSeconds = 30;
    
    // Metrics
    options.EnableMetrics = true;
});
```

## ✅ Optimization Checklist

### Basic
- [ ] HTTP/2 enabled
- [ ] Gzip/deflate compression
- [ ] Connection pooling
- [ ] Timeouts configured

### Intermediate
- [ ] Request batching
- [ ] Parallelism where appropriate
- [ ] Cache with TTL
- [ ] Hierarchical rate limiting

### Advanced
- [ ] Smart prefetching
- [ ] Request prioritization
- [ ] Streaming for large volumes
- [ ] Metrics and observability
- [ ] Circuit breaker configured

**Next**: [07-ERROR-HANDLING.md](07-ERROR-HANDLING.md)

```
