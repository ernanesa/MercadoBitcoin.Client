# 🚀 High Performance Trading Guide - MercadoBitcoin.Client

## ⚠️ Aviso Importante: HFT vs Alta Performance

**Este documento aborda Trading de Alta Performance, NÃO High-Frequency Trading (HFT) verdadeiro.**

### Por que HFT Verdadeiro NÃO é Possível no Mercado Bitcoin?

| Aspecto | HFT Real (Wall Street) | Mercado Bitcoin | Gap |
|---------|------------------------|-----------------|-----|
| **Latência** | 1-10 microsegundos | 10-50 milissegundos | 1000-50000x maior |
| **Protocolo** | FIX/SBE (binário) | REST/JSON | Overhead de parsing |
| **Rate Limit** | Ilimitado (co-location) | 500 req/min (~8/s) | Bloqueio para HFT |
| **Trading Limit** | Ilimitado | 3 ordens/segundo | Impossibilita HFT |
| **Co-location** | Mesmo rack do matching engine | Não disponível | Latência de rede |
| **Market Data** | Tick-by-tick, delta updates | Snapshot full | Desperdício de banda |
| **Acesso** | Matching engine direto | API pública | Camadas intermediárias |

### O Que Este Documento Cobre

- ✅ Trading Algorítmico de **Média-Alta Frequência** (dezenas de ordens/minuto)
- ✅ Otimização de latência de **milissegundos para centenas de microsegundos** onde possível
- ✅ **Market Making** com spreads adequados
- ✅ **Arbitragem** com janelas > 500ms
- ✅ Maximização do **throughput** dentro dos rate limits
- ✅ **Resiliência** e recuperação rápida de falhas

---

## 📊 Arquitetura para Alta Performance

### Diagrama de Fluxo Otimizado

```
┌─────────────────────────────────────────────────────────────────────┐
│                     APLICAÇÃO DE TRADING                            │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐  │
│  │   Strategy  │  │   Signal    │  │   Order     │  │   Risk     │  │
│  │   Engine    │──│  Generator  │──│   Manager   │──│   Manager  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│                    HOT PATH (Zero-Allocation)                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐  │
│  │  Pre-built  │  │   Object    │  │  Lock-Free  │  │   SIMD     │  │
│  │   Orders    │  │    Pool     │  │   Queues    │  │   Math     │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│                 MERCADOBITCOIN.CLIENT (Otimizado)                   │
│  ┌──────────────────────────────┐  ┌────────────────────────────┐   │
│  │      REST API (HTTP/2)       │  │   WebSocket (Real-time)    │   │
│  │  - Connection Pool (100+)    │  │  - Ticker Stream           │   │
│  │  - Keep-Alive Persistent     │  │  - Trades Stream           │   │
│  │  - Brotli Compression        │  │  - OrderBook Stream        │   │
│  │  - Request Coalescing        │  │  - Auto-Reconnect          │   │
│  └──────────────────────────────┘  └────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    OTIMIZAÇÕES DE REDE                              │
│  - Servidor em São Paulo (mesmo datacenter se possível)             │
│  - TCP_NODELAY habilitado                                           │
│  - Buffer sizes otimizados                                          │
│  - TLS 1.3 com session resumption                                   │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                 MERCADO BITCOIN API                                 │
│  - Rate Limit: 500 req/min global                                   │
│  - Trading: 3 req/s (place/cancel)                                  │
│  - Public Data: 1 req/s                                             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Configurações Otimizadas

### 1. Cliente HTTP para Trading

```csharp
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Extensions;

public static class HighPerformanceClientFactory
{
    /// <summary>
    /// Cria cliente ultra-otimizado para trading de alta performance.
    /// </summary>
    public static MercadoBitcoinClient CreateHighPerformanceClient()
    {
        var options = new MercadoBitcoinClientOptions
        {
            BaseUrl = "https://api.mercadobitcoin.net/api/v4",
            
            // HTTP Configuration otimizada
            HttpConfiguration = new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                TimeoutSeconds = 5, // Timeout agressivo - fail fast
                EnableCompression = false, // Desabilita para menor latência
                MaxConnectionsPerServer = 200, // Pool grande
                ConnectionLifetimeSeconds = 600, // Conexões duradouras
            },
            
            // Retry desabilitado para ordens críticas
            RetryPolicyConfig = new RetryPolicyConfig
            {
                MaxRetryAttempts = 0, // SEM RETRY para ordens
                EnableCircuitBreaker = false, // Controle manual
                RetryOnTimeout = false,
                RetryOnRateLimit = false,
                RetryOnServerErrors = false,
            },
            
            // Rate limiter conservador
            RateLimiterConfig = new RateLimiterConfig
            {
                PermitLimit = 100,
                TokensPerPeriod = 3, // 3 ordens/segundo
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                QueueLimit = 0, // Fail fast, não enfileira
                AutoReplenishment = true,
            },
            
            // Cache agressivo para dados públicos
            CacheConfig = new CacheConfig
            {
                EnableL1Cache = true,
                DefaultL1Expiration = TimeSpan.FromMilliseconds(100), // Cache curto
                EnableRequestCoalescing = true,
                EnableNegativeCaching = false,
            }
        };
        
        return new MercadoBitcoinClient(options);
    }
    
    /// <summary>
    /// Cria cliente separado apenas para dados públicos (market data).
    /// </summary>
    public static MercadoBitcoinClient CreateMarketDataClient()
    {
        var options = new MercadoBitcoinClientOptions
        {
            BaseUrl = "https://api.mercadobitcoin.net/api/v4",
            
            HttpConfiguration = new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                TimeoutSeconds = 10,
                EnableCompression = true, // OK para market data
                MaxConnectionsPerServer = 50,
                ConnectionLifetimeSeconds = 300,
            },
            
            RetryPolicyConfig = new RetryPolicyConfig
            {
                MaxRetryAttempts = 2,
                BaseDelaySeconds = 0.1,
                BackoffMultiplier = 1.5,
                MaxDelaySeconds = 1.0,
            },
            
            CacheConfig = new CacheConfig
            {
                EnableL1Cache = true,
                DefaultL1Expiration = TimeSpan.FromMilliseconds(500),
                EnableRequestCoalescing = true,
            }
        };
        
        return new MercadoBitcoinClient(options);
    }
}
```

### 2. WebSocket Otimizado

```csharp
using MercadoBitcoin.Client.WebSocket;

public static class HighPerformanceWebSocketFactory
{
    public static WebSocketClientOptions CreateOptimizedOptions()
    {
        return new WebSocketClientOptions
        {
            WebSocketUrl = "wss://ws.mercadobitcoin.net/ws",
            
            // Keep-alive agressivo
            KeepAliveInterval = TimeSpan.FromSeconds(15), // Mais frequente
            KeepAliveTimeout = TimeSpan.FromSeconds(5),   // Timeout curto
            
            // Reconexão rápida
            AutoReconnect = true,
            MaxReconnectAttempts = 100, // Persistente
            InitialReconnectDelay = TimeSpan.FromMilliseconds(100), // Rápido
            MaxReconnectDelay = TimeSpan.FromSeconds(5), // Cap baixo
            
            // Buffers otimizados
            ReceiveBufferSize = 16 * 1024, // 16KB para orderbook
            SendBufferSize = 1024,          // 1KB suficiente para subscribe
            
            // Conexão rápida
            ConnectionTimeout = TimeSpan.FromSeconds(5),
        };
    }
}
```

### 3. Configuração de Runtime (.NET)

```xml
<!-- runtimeconfig.template.json -->
{
  "configProperties": {
    "System.GC.Server": true,
    "System.GC.Concurrent": true,
    "System.GC.RetainVM": true,
    "System.GC.HeapHardLimit": 2147483648,
    "System.Threading.ThreadPool.MinThreads": 50,
    "System.Threading.ThreadPool.MaxThreads": 200,
    "System.Net.Http.SocketsHttpHandler.Http2Support": true,
    "System.Net.Http.SocketsHttpHandler.Http3Support": true
  }
}
```

```xml
<!-- .csproj para AOT e otimizações -->
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <TieredCompilation>true</TieredCompilation>
    <TieredCompilationQuickJit>false</TieredCompilationQuickJit>
    <TieredPGO>true</TieredPGO>
    <ServerGarbageCollection>true</ServerGarbageCollection>
    <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
    <RetainVMGarbageCollection>true</RetainVMGarbageCollection>
    <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

---

## ⚡ Padrões de Código para Alta Performance

### 1. Order Manager com Pre-Built Orders

```csharp
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Generated;

/// <summary>
/// Gerenciador de ordens otimizado para alta frequência.
/// Usa templates pré-construídos para minimizar alocações.
/// </summary>
public sealed class HighPerformanceOrderManager : IDisposable
{
    private readonly MercadoBitcoinClient _tradingClient;
    private readonly string _accountId;
    
    // Pool de requests pré-alocados
    private readonly ConcurrentBag<PlaceOrderRequest> _orderPool = new();
    
    // Templates de ordem pré-construídos
    private readonly Dictionary<string, PlaceOrderRequest> _buyTemplates = new();
    private readonly Dictionary<string, PlaceOrderRequest> _sellTemplates = new();
    
    // Contador para external IDs únicos (lock-free)
    private long _orderCounter;
    
    public HighPerformanceOrderManager(
        MercadoBitcoinClient tradingClient,
        string accountId,
        IEnumerable<string> symbols)
    {
        _tradingClient = tradingClient;
        _accountId = accountId;
        
        // Pré-constrói templates para cada símbolo
        foreach (var symbol in symbols)
        {
            _buyTemplates[symbol] = new PlaceOrderRequest
            {
                Type = "limit",
                Side = "buy",
                Async = true // Sempre async para menor latência
            };
            
            _sellTemplates[symbol] = new PlaceOrderRequest
            {
                Type = "limit",
                Side = "sell",
                Async = true
            };
        }
        
        // Pré-aloca pool de requests
        for (int i = 0; i < 100; i++)
        {
            _orderPool.Add(new PlaceOrderRequest { Async = true });
        }
    }
    
    /// <summary>
    /// Coloca ordem de compra com latência mínima.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PlaceOrderResponse> PlaceBuyOrderAsync(
        string symbol,
        decimal quantity,
        decimal price,
        CancellationToken ct = default)
    {
        var request = GetOrCreateRequest();
        request.Side = "buy";
        request.Type = "limit";
        request.Qty = quantity.ToString("F8");
        request.LimitPrice = price;
        request.ExternalId = GenerateOrderId();
        
        return new ValueTask<PlaceOrderResponse>(
            _tradingClient.PlaceOrderAsync(symbol, _accountId, request, ct));
    }
    
    /// <summary>
    /// Coloca ordem de venda com latência mínima.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PlaceOrderResponse> PlaceSellOrderAsync(
        string symbol,
        decimal quantity,
        decimal price,
        CancellationToken ct = default)
    {
        var request = GetOrCreateRequest();
        request.Side = "sell";
        request.Type = "limit";
        request.Qty = quantity.ToString("F8");
        request.LimitPrice = price;
        request.ExternalId = GenerateOrderId();
        
        return new ValueTask<PlaceOrderResponse>(
            _tradingClient.PlaceOrderAsync(symbol, _accountId, request, ct));
    }
    
    /// <summary>
    /// Coloca ordem a mercado para execução imediata.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PlaceOrderResponse> PlaceMarketOrderAsync(
        string symbol,
        string side,
        decimal cost, // Valor em BRL para compra
        CancellationToken ct = default)
    {
        var request = GetOrCreateRequest();
        request.Side = side;
        request.Type = "market";
        request.Cost = cost;
        request.ExternalId = GenerateOrderId();
        
        return new ValueTask<PlaceOrderResponse>(
            _tradingClient.PlaceOrderAsync(symbol, _accountId, request, ct));
    }
    
    /// <summary>
    /// Cancela ordem com timeout agressivo.
    /// </summary>
    public async ValueTask<bool> CancelOrderFastAsync(
        string symbol,
        string orderId,
        CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2)); // Timeout de 2s
            
            await _tradingClient.CancelOrderAsync(
                _accountId, symbol, orderId, async: true, cts.Token);
            
            return true;
        }
        catch (OperationCanceledException)
        {
            return false; // Timeout - ordem pode ou não ter sido cancelada
        }
    }
    
    /// <summary>
    /// Cancela múltiplas ordens em paralelo.
    /// </summary>
    public async ValueTask<int> CancelOrdersBatchAsync(
        string symbol,
        IEnumerable<string> orderIds,
        CancellationToken ct = default)
    {
        var tasks = orderIds.Select(id => CancelOrderFastAsync(symbol, id, ct));
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlaceOrderRequest GetOrCreateRequest()
    {
        if (_orderPool.TryTake(out var request))
        {
            return request;
        }
        return new PlaceOrderRequest { Async = true };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GenerateOrderId()
    {
        var id = Interlocked.Increment(ref _orderCounter);
        return $"HP{id:X16}"; // Formato hexadecimal compacto
    }
    
    public void ReturnRequest(PlaceOrderRequest request)
    {
        // Limpa e retorna ao pool
        request.Qty = null;
        request.Cost = null;
        request.LimitPrice = null;
        request.StopPrice = null;
        request.ExternalId = null;
        _orderPool.Add(request);
    }
    
    public void Dispose()
    {
        _orderPool.Clear();
        _buyTemplates.Clear();
        _sellTemplates.Clear();
    }
}
```

### 2. Market Data Aggregator (Zero-Allocation)

```csharp
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MercadoBitcoin.Client.WebSocket;
using MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// Agregador de market data otimizado para zero-allocation no hot path.
/// </summary>
public sealed class HighPerformanceMarketData : IAsyncDisposable
{
    private readonly MercadoBitcoinWebSocketClient _wsClient;
    private readonly CancellationTokenSource _cts = new();
    
    // Channels lock-free para cada tipo de dado
    private readonly Channel<TickerUpdate> _tickerChannel;
    private readonly Channel<TradeUpdate> _tradeChannel;
    private readonly Channel<OrderBookUpdate> _orderBookChannel;
    
    // Último estado conhecido (para acesso rápido)
    private readonly ConcurrentDictionary<string, TickerSnapshot> _lastTickers = new();
    private readonly ConcurrentDictionary<string, OrderBookSnapshot> _lastOrderBooks = new();
    
    public HighPerformanceMarketData()
    {
        _wsClient = new MercadoBitcoinWebSocketClient(
            HighPerformanceWebSocketFactory.CreateOptimizedOptions());
        
        // Channels bounded para backpressure
        var channelOptions = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = true,
        };
        
        _tickerChannel = Channel.CreateBounded<TickerUpdate>(channelOptions);
        _tradeChannel = Channel.CreateBounded<TradeUpdate>(channelOptions);
        _orderBookChannel = Channel.CreateBounded<OrderBookUpdate>(channelOptions);
    }
    
    public async Task StartAsync(IEnumerable<string> symbols)
    {
        await _wsClient.ConnectAsync(_cts.Token);
        
        foreach (var symbol in symbols)
        {
            // Inicia subscriptions em paralelo
            _ = SubscribeTickerAsync(symbol);
            _ = SubscribeTradesAsync(symbol);
            _ = SubscribeOrderBookAsync(symbol);
        }
    }
    
    /// <summary>
    /// Obtém último ticker conhecido (acesso O(1), zero-allocation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLastTicker(string symbol, out TickerSnapshot ticker)
    {
        return _lastTickers.TryGetValue(symbol, out ticker);
    }
    
    /// <summary>
    /// Obtém último orderbook conhecido (acesso O(1)).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLastOrderBook(string symbol, out OrderBookSnapshot orderBook)
    {
        return _lastOrderBooks.TryGetValue(symbol, out orderBook);
    }
    
    /// <summary>
    /// Calcula spread atual de forma inline.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetSpread(string symbol)
    {
        if (_lastTickers.TryGetValue(symbol, out var ticker))
        {
            return ticker.BestAsk - ticker.BestBid;
        }
        return decimal.MaxValue; // Indica spread desconhecido
    }
    
    /// <summary>
    /// Calcula mid-price de forma inline.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetMidPrice(string symbol)
    {
        if (_lastTickers.TryGetValue(symbol, out var ticker))
        {
            return (ticker.BestAsk + ticker.BestBid) / 2m;
        }
        return 0m;
    }
    
    /// <summary>
    /// Reader para consumir ticker updates (para estratégias).
    /// </summary>
    public ChannelReader<TickerUpdate> TickerUpdates => _tickerChannel.Reader;
    
    /// <summary>
    /// Reader para consumir trade updates.
    /// </summary>
    public ChannelReader<TradeUpdate> TradeUpdates => _tradeChannel.Reader;
    
    /// <summary>
    /// Reader para consumir orderbook updates.
    /// </summary>
    public ChannelReader<OrderBookUpdate> OrderBookUpdates => _orderBookChannel.Reader;
    
    private async Task SubscribeTickerAsync(string symbol)
    {
        try
        {
            await foreach (var msg in _wsClient.SubscribeTickerAsync(symbol, _cts.Token))
            {
                if (msg.Data is { } data)
                {
                    // Atualiza snapshot
                    var snapshot = new TickerSnapshot
                    {
                        Symbol = symbol,
                        Last = data.Last,
                        BestBid = data.BestBid,
                        BestAsk = data.BestAsk,
                        Volume = data.Volume,
                        Timestamp = Environment.TickCount64
                    };
                    _lastTickers[symbol] = snapshot;
                    
                    // Publica para consumers
                    _tickerChannel.Writer.TryWrite(new TickerUpdate
                    {
                        Symbol = symbol,
                        Data = data
                    });
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    
    private async Task SubscribeTradesAsync(string symbol)
    {
        try
        {
            await foreach (var msg in _wsClient.SubscribeTradesAsync(symbol, _cts.Token))
            {
                if (msg.Data is { } data)
                {
                    _tradeChannel.Writer.TryWrite(new TradeUpdate
                    {
                        Symbol = symbol,
                        Data = data
                    });
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    
    private async Task SubscribeOrderBookAsync(string symbol)
    {
        try
        {
            await foreach (var msg in _wsClient.SubscribeOrderBookAsync(symbol, _cts.Token))
            {
                if (msg.Data is { } data)
                {
                    var snapshot = new OrderBookSnapshot
                    {
                        Symbol = symbol,
                        BestBidPrice = data.BestBidPrice ?? 0,
                        BestBidQty = data.BestBidQuantity ?? 0,
                        BestAskPrice = data.BestAskPrice ?? 0,
                        BestAskQty = data.BestAskQuantity ?? 0,
                        Spread = data.Spread ?? 0,
                        MidPrice = data.MidPrice ?? 0,
                        Timestamp = Environment.TickCount64
                    };
                    _lastOrderBooks[symbol] = snapshot;
                    
                    _orderBookChannel.Writer.TryWrite(new OrderBookUpdate
                    {
                        Symbol = symbol,
                        Data = data
                    });
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    
    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _tickerChannel.Writer.Complete();
        _tradeChannel.Writer.Complete();
        _orderBookChannel.Writer.Complete();
        await _wsClient.DisposeAsync();
        _cts.Dispose();
    }
}

// Structs para zero-allocation
public readonly record struct TickerSnapshot
{
    public required string Symbol { get; init; }
    public required decimal Last { get; init; }
    public required decimal BestBid { get; init; }
    public required decimal BestAsk { get; init; }
    public required decimal Volume { get; init; }
    public required long Timestamp { get; init; }
    
    public decimal Spread => BestAsk - BestBid;
    public decimal MidPrice => (BestAsk + BestBid) / 2m;
}

public readonly record struct OrderBookSnapshot
{
    public required string Symbol { get; init; }
    public required decimal BestBidPrice { get; init; }
    public required decimal BestBidQty { get; init; }
    public required decimal BestAskPrice { get; init; }
    public required decimal BestAskQty { get; init; }
    public required decimal Spread { get; init; }
    public required decimal MidPrice { get; init; }
    public required long Timestamp { get; init; }
}

public readonly record struct TickerUpdate
{
    public required string Symbol { get; init; }
    public required TickerData Data { get; init; }
}

public readonly record struct TradeUpdate
{
    public required string Symbol { get; init; }
    public required TradeData Data { get; init; }
}

public readonly record struct OrderBookUpdate
{
    public required string Symbol { get; init; }
    public required OrderBookData Data { get; init; }
}
```

### 3. Strategy Engine Base

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Base para estratégias de trading de alta performance.
/// </summary>
public abstract class HighPerformanceStrategy : IAsyncDisposable
{
    protected readonly HighPerformanceMarketData MarketData;
    protected readonly HighPerformanceOrderManager OrderManager;
    protected readonly string Symbol;
    
    private readonly CancellationTokenSource _cts = new();
    private Task? _runTask;
    
    // Métricas de performance
    private long _ticksProcessed;
    private long _ordersPlaced;
    private long _ordersCancelled;
    private readonly Stopwatch _latencyWatch = new();
    
    protected HighPerformanceStrategy(
        HighPerformanceMarketData marketData,
        HighPerformanceOrderManager orderManager,
        string symbol)
    {
        MarketData = marketData;
        OrderManager = orderManager;
        Symbol = symbol;
    }
    
    public void Start()
    {
        _runTask = RunAsync(_cts.Token);
    }
    
    public async Task StopAsync()
    {
        _cts.Cancel();
        if (_runTask != null)
        {
            await _runTask;
        }
    }
    
    private async Task RunAsync(CancellationToken ct)
    {
        // Aguarda dados iniciais
        await WarmUpAsync(ct);
        
        // Loop principal de estratégia
        await foreach (var update in MarketData.TickerUpdates.ReadAllAsync(ct))
        {
            if (update.Symbol != Symbol) continue;
            
            _latencyWatch.Restart();
            
            try
            {
                await OnTickAsync(update, ct);
                Interlocked.Increment(ref _ticksProcessed);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            
            _latencyWatch.Stop();
            
            // Log se latência > 1ms (problema)
            if (_latencyWatch.ElapsedMilliseconds > 1)
            {
                OnSlowTick(_latencyWatch.ElapsedMilliseconds);
            }
        }
    }
    
    /// <summary>
    /// Aquecimento inicial (carregar dados, calcular indicadores, etc).
    /// </summary>
    protected virtual Task WarmUpAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Processamento de cada tick de mercado.
    /// DEVE SER RÁPIDO (< 1ms idealmente).
    /// </summary>
    protected abstract Task OnTickAsync(TickerUpdate update, CancellationToken ct);
    
    /// <summary>
    /// Chamado quando ocorre erro no processamento.
    /// </summary>
    protected virtual void OnError(Exception ex)
    {
        Console.Error.WriteLine($"[{Symbol}] Error: {ex.Message}");
    }
    
    /// <summary>
    /// Chamado quando tick demora mais que 1ms.
    /// </summary>
    protected virtual void OnSlowTick(long elapsedMs)
    {
        Console.WriteLine($"[{Symbol}] Slow tick: {elapsedMs}ms");
    }
    
    // Helpers para ordens com tracking
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async ValueTask<PlaceOrderResponse?> PlaceBuyAsync(
        decimal qty, decimal price, CancellationToken ct)
    {
        var result = await OrderManager.PlaceBuyOrderAsync(Symbol, qty, price, ct);
        Interlocked.Increment(ref _ordersPlaced);
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async ValueTask<PlaceOrderResponse?> PlaceSellAsync(
        decimal qty, decimal price, CancellationToken ct)
    {
        var result = await OrderManager.PlaceSellOrderAsync(Symbol, qty, price, ct);
        Interlocked.Increment(ref _ordersPlaced);
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async ValueTask<bool> CancelAsync(string orderId, CancellationToken ct)
    {
        var result = await OrderManager.CancelOrderFastAsync(Symbol, orderId, ct);
        if (result) Interlocked.Increment(ref _ordersCancelled);
        return result;
    }
    
    // Métricas
    public long TicksProcessed => Interlocked.Read(ref _ticksProcessed);
    public long OrdersPlaced => Interlocked.Read(ref _ordersPlaced);
    public long OrdersCancelled => Interlocked.Read(ref _ordersCancelled);
    
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
    }
}
```

### 4. Exemplo: Simple Market Maker

```csharp
/// <summary>
/// Market maker simples de alta performance.
/// ATENÇÃO: Exemplo educacional - não usar em produção sem ajustes!
/// </summary>
public sealed class SimpleMarketMaker : HighPerformanceStrategy
{
    private readonly decimal _spreadMultiplier;
    private readonly decimal _orderSize;
    private readonly decimal _maxPosition;
    
    private decimal _position;
    private string? _activeBuyOrderId;
    private string? _activeSellOrderId;
    private decimal _lastMidPrice;
    
    public SimpleMarketMaker(
        HighPerformanceMarketData marketData,
        HighPerformanceOrderManager orderManager,
        string symbol,
        decimal spreadMultiplier = 1.5m,  // 1.5x o spread de mercado
        decimal orderSize = 0.001m,        // Tamanho de cada ordem
        decimal maxPosition = 0.01m)       // Posição máxima
        : base(marketData, orderManager, symbol)
    {
        _spreadMultiplier = spreadMultiplier;
        _orderSize = orderSize;
        _maxPosition = maxPosition;
    }
    
    protected override async Task OnTickAsync(TickerUpdate update, CancellationToken ct)
    {
        var data = update.Data;
        var midPrice = (data.BestAsk + data.BestBid) / 2m;
        var marketSpread = data.BestAsk - data.BestBid;
        
        // Se preço mudou significativamente, atualiza ordens
        if (Math.Abs(midPrice - _lastMidPrice) > marketSpread * 0.1m)
        {
            _lastMidPrice = midPrice;
            await UpdateQuotesAsync(midPrice, marketSpread, ct);
        }
    }
    
    private async Task UpdateQuotesAsync(
        decimal midPrice, decimal marketSpread, CancellationToken ct)
    {
        // Cancela ordens antigas
        if (_activeBuyOrderId != null)
        {
            await CancelAsync(_activeBuyOrderId, ct);
            _activeBuyOrderId = null;
        }
        if (_activeSellOrderId != null)
        {
            await CancelAsync(_activeSellOrderId, ct);
            _activeSellOrderId = null;
        }
        
        // Calcula novos preços
        var halfSpread = (marketSpread * _spreadMultiplier) / 2m;
        var buyPrice = midPrice - halfSpread;
        var sellPrice = midPrice + halfSpread;
        
        // Coloca nova ordem de compra (se não estiver muito comprado)
        if (_position < _maxPosition)
        {
            var result = await PlaceBuyAsync(_orderSize, buyPrice, ct);
            _activeBuyOrderId = result?.OrderId;
        }
        
        // Coloca nova ordem de venda (se não estiver muito vendido)
        if (_position > -_maxPosition)
        {
            var result = await PlaceSellAsync(_orderSize, sellPrice, ct);
            _activeSellOrderId = result?.OrderId;
        }
    }
    
    // Chamado quando ordem é executada (via polling ou tracking)
    public void OnOrderFilled(string orderId, string side, decimal qty)
    {
        if (side == "buy")
        {
            _position += qty;
            _activeBuyOrderId = null;
        }
        else
        {
            _position -= qty;
            _activeSellOrderId = null;
        }
    }
}
```

---

## 📊 Rate Limit Management

### Budget Calculator

```csharp
/// <summary>
/// Gerenciador de budget de rate limit para trading.
/// </summary>
public sealed class RateLimitBudget
{
    // Limites do Mercado Bitcoin
    private const int GlobalLimitPerMinute = 500;
    private const int TradingLimitPerSecond = 3;
    private const int PublicDataLimitPerSecond = 1;
    
    private readonly SemaphoreSlim _tradingSemaphore;
    private readonly SemaphoreSlim _publicSemaphore;
    private readonly Timer _replenishTimer;
    
    private int _tradingTokens;
    private int _publicTokens;
    private int _globalUsedThisMinute;
    
    public RateLimitBudget()
    {
        _tradingSemaphore = new SemaphoreSlim(TradingLimitPerSecond);
        _publicSemaphore = new SemaphoreSlim(PublicDataLimitPerSecond);
        _tradingTokens = TradingLimitPerSecond;
        _publicTokens = PublicDataLimitPerSecond;
        
        // Replenish a cada segundo
        _replenishTimer = new Timer(Replenish, null, 
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    /// <summary>
    /// Adquire token para operação de trading.
    /// Retorna false se não há budget disponível.
    /// </summary>
    public bool TryAcquireTrading()
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
            return false;
        
        if (_tradingSemaphore.Wait(0))
        {
            Interlocked.Increment(ref _globalUsedThisMinute);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Adquire token para dados públicos.
    /// </summary>
    public bool TryAcquirePublic()
    {
        if (Interlocked.Read(ref _globalUsedThisMinute) >= GlobalLimitPerMinute)
            return false;
        
        if (_publicSemaphore.Wait(0))
        {
            Interlocked.Increment(ref _globalUsedThisMinute);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Budget disponível para trading.
    /// </summary>
    public int AvailableTradingBudget => 
        Math.Min(_tradingSemaphore.CurrentCount, 
                 GlobalLimitPerMinute - (int)Interlocked.Read(ref _globalUsedThisMinute));
    
    private void Replenish(object? state)
    {
        // Replenish trading tokens
        while (_tradingSemaphore.CurrentCount < TradingLimitPerSecond)
        {
            _tradingSemaphore.Release();
        }
        
        // Replenish public tokens
        while (_publicSemaphore.CurrentCount < PublicDataLimitPerSecond)
        {
            _publicSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Reset do contador global (chamar a cada minuto).
    /// </summary>
    public void ResetMinuteCounter()
    {
        Interlocked.Exchange(ref _globalUsedThisMinute, 0);
    }
}
```

---

## 🔬 Benchmarks e Métricas

### Latências Esperadas

| Operação | Latência Típica | Latência Otimizada | Meta |
|----------|-----------------|--------------------| -----|
| REST Place Order | 50-100ms | 20-50ms | <30ms |
| REST Cancel Order | 50-100ms | 20-50ms | <30ms |
| REST Get Ticker | 30-80ms | 15-40ms | <20ms |
| WebSocket Ticker | 50-200ms | 50-150ms | <100ms |
| WebSocket Trade | 50-200ms | 50-150ms | <100ms |
| Internal Processing | 1-5ms | <0.1ms | <0.1ms |

### Throughput Máximo

| Operação | Limite API | Throughput Otimizado |
|----------|------------|----------------------|
| Place/Cancel Orders | 3/segundo | 2.5/segundo (margem) |
| Get Orders | 10/segundo | 8/segundo |
| Public Data | 1/segundo | 0.8/segundo |
| **Total Global** | **500/minuto** | **400/minuto** |

### Como Medir Performance

```csharp
using System.Diagnostics;

public static class PerformanceMonitor
{
    private static readonly Stopwatch _sw = new();
    
    public static async Task<(T Result, long ElapsedMicroseconds)> MeasureAsync<T>(
        Func<Task<T>> action)
    {
        _sw.Restart();
        var result = await action();
        _sw.Stop();
        return (result, _sw.ElapsedTicks * 1_000_000 / Stopwatch.Frequency);
    }
    
    public static void LogLatency(string operation, long microseconds)
    {
        var color = microseconds switch
        {
            < 1000 => ConsoleColor.Green,      // < 1ms = excelente
            < 10000 => ConsoleColor.Yellow,    // < 10ms = ok
            < 50000 => ConsoleColor.DarkYellow, // < 50ms = aceitável
            _ => ConsoleColor.Red               // > 50ms = problema
        };
        
        Console.ForegroundColor = color;
        Console.WriteLine($"[{operation}] {microseconds:N0}μs ({microseconds/1000.0:F2}ms)");
        Console.ResetColor();
    }
}

// Uso
var (result, latency) = await PerformanceMonitor.MeasureAsync(
    () => client.PlaceOrderAsync(symbol, accountId, request));
PerformanceMonitor.LogLatency("PlaceOrder", latency);
```

---

## ⚠️ Considerações Importantes

### O Que Esta Lib Suporta Bem

1. ✅ **Trading Algorítmico de Média Frequência**
   - Dezenas de ordens por minuto
   - Estratégias com horizonte > 1 segundo

2. ✅ **Market Making com Spreads Adequados**
   - Spreads > 0.1% para compensar latência
   - Posições pequenas para risco controlado

3. ✅ **Arbitragem com Janelas > 500ms**
   - Arbitragem entre pares no MB
   - Arbitragem cross-exchange com margem

4. ✅ **Automação de Estratégias**
   - DCA, Grid Trading, Rebalanceamento
   - Signal-based trading

### O Que NÃO é Possível

1. ❌ **HFT Verdadeiro (< 1ms)**
   - Latência de rede impossibilita
   - Rate limits bloqueiam

2. ❌ **Scalping de Centavos**
   - Spread mínimo come lucro
   - Execução não garantida

3. ❌ **Arbitragem de Microsegundos**
   - Janelas fecham antes da execução

4. ❌ **Front-Running**
   - Não há acesso ao matching engine

### Recomendações Finais

1. **Use WebSocket para market data** - Mais rápido que polling REST
2. **Pré-construa ordens** - Minimize alocações no hot path
3. **Mantenha conexões abertas** - Connection pooling é essencial
4. **Monitore latência** - Identifique gargalos
5. **Respeite rate limits** - Evite bans
6. **Teste em sandbox primeiro** - MB tem ambiente de teste
7. **Comece conservador** - Aumente frequência gradualmente

---

## 📚 Referências

- [Mercado Bitcoin API v4 Docs](https://api.mercadobitcoin.net/api/v4/docs)
- [WebSocket API Docs](https://ws.mercadobitcoin.net/docs/v0/)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/performance/)
- [High-Performance .NET](https://www.writinghighperf.net/)

---

*Documento criado para MercadoBitcoin.Client v5.2.0*  
*Última atualização: Janeiro 2026*