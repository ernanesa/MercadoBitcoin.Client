# WebSocket Client - Mercado Bitcoin

Este módulo fornece um cliente WebSocket completo para a API do Mercado Bitcoin, permitindo receber dados de mercado em tempo real.

## Características

- ✅ **Conexão WebSocket estável** com reconexão automática
- ✅ **Suporte a todos os canais públicos** (trades, orderbook, ticker, candles)
- ✅ **Eventos tipados** para cada tipo de dados
- ✅ **Configuração flexível** com diferentes perfis (produção, desenvolvimento, trading)
- ✅ **Tratamento de erros robusto** com retry automático
- ✅ **Extensões de conveniência** para facilitar o uso
- ✅ **Suporte a CancellationToken** para operações assíncronas
- ✅ **Logging detalhado** (opcional)

## Uso Básico

### Configuração Simples

```csharp
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.WebSocket.Extensions;

// Criar cliente otimizado para trading
using var client = MercadoBitcoinClientExtensions.CreateForTrading();

// Configurar eventos
client.WebSocket.TradeReceived += (sender, args) =>
{
    var trade = args.Data;
    Console.WriteLine($"Trade: {trade.Symbol} - {trade.Price} x {trade.Quantity}");
};

// Conectar e inscrever-se automaticamente
await client.WebSocket.ConnectAndSubscribeAsync(
    "BTC-BRL",
    includeTrades: true,
    includeOrderBook: true,
    includeTicker: true);
```

### Configuração Avançada

```csharp
// Criar configuração personalizada
var webSocketConfig = MercadoBitcoinClientExtensions.CreateWebSocketConfig(
    enableAutoReconnect: true,
    reconnectInterval: TimeSpan.FromSeconds(2),
    maxReconnectAttempts: 10,
    enableDetailedLogging: true);

using var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies(
    webSocketConfig: webSocketConfig);

// Configurar eventos específicos
client.WebSocket.Connected += (sender, args) => Console.WriteLine("Conectado!");
client.WebSocket.Disconnected += (sender, args) => Console.WriteLine($"Desconectado: {args.Reason}");
client.WebSocket.Error += (sender, args) => Console.WriteLine($"Erro: {args.Exception.Message}");

// Conectar manualmente
await client.WebSocket.ConnectAsync();

// Inscrever-se em canais específicos
await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
await client.WebSocket.SubscribeToOrderBookAsync("BTC-BRL");
await client.WebSocket.SubscribeToTickerAsync("ETH-BRL");
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.FiveMinutes);
```

## Canais Disponíveis

### 1. Trades
Recebe informações sobre negociações executadas em tempo real.

```csharp
client.WebSocket.TradeReceived += (sender, args) =>
{
    var trade = args.Data;
    Console.WriteLine($"Trade: {trade.Symbol} - {trade.Price} x {trade.Quantity} ({trade.Side})");
};

await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
```

### 2. Order Book
Recebe o livro de ofertas completo.

```csharp
client.WebSocket.OrderBookReceived += (sender, args) =>
{
    var orderBook = args.Data;
    Console.WriteLine($"OrderBook: {orderBook.Symbol} - {orderBook.Bids?.Count} bids, {orderBook.Asks?.Count} asks");
    
    // Melhor bid e ask
    if (orderBook.Bids?.Count > 0)
        Console.WriteLine($"Melhor Bid: {orderBook.Bids[0].Price}");
    if (orderBook.Asks?.Count > 0)
        Console.WriteLine($"Melhor Ask: {orderBook.Asks[0].Price}");
};

await client.WebSocket.SubscribeToOrderBookAsync("BTC-BRL");
```

### 3. Ticker
Recebe informações de preço e volume das últimas 24 horas.

```csharp
client.WebSocket.TickerReceived += (sender, args) =>
{
    var ticker = args.Data;
    Console.WriteLine($"Ticker: {ticker.Symbol} - Last: {ticker.Last}, High: {ticker.High}, Low: {ticker.Low}");
};

await client.WebSocket.SubscribeToTickerAsync("BTC-BRL");
```

### 4. Candles
Recebe dados de candles (OHLCV) em diferentes intervalos.

```csharp
client.WebSocket.CandleReceived += (sender, args) =>
{
    var candle = args.Data;
    Console.WriteLine($"Candle: {candle.Symbol} ({candle.Interval}) - O: {candle.Open}, H: {candle.High}, L: {candle.Low}, C: {candle.Close}");
};

// Intervalos disponíveis
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.OneMinute);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.FiveMinutes);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.FifteenMinutes);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.ThirtyMinutes);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.OneHour);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.FourHours);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.SixHours);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.TwelveHours);
await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.OneDay);
```

## Configurações Predefinidas

### Para Trading
```csharp
// Configuração otimizada para trading com reconexão rápida
using var client = MercadoBitcoinClientExtensions.CreateForTrading();
```

### Para Desenvolvimento
```csharp
// Configuração com logging detalhado e timeouts maiores
using var client = MercadoBitcoinClientExtensions.CreateForDevelopment();
```

### Personalizada
```csharp
var config = MercadoBitcoinClientExtensions.CreateWebSocketConfig(
    enableAutoReconnect: true,
    reconnectInterval: TimeSpan.FromSeconds(5),
    maxReconnectAttempts: 3,
    enableDetailedLogging: false);

using var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies(
    webSocketConfig: config);
```

## Gerenciamento de Conexão

### Estados da Conexão
```csharp
// Verificar estado atual
Console.WriteLine($"Estado: {client.WebSocket.State}");
Console.WriteLine($"Conectado: {client.WebSocket.IsConnected}");

// Estados possíveis:
// - None: Não inicializado
// - Connecting: Conectando
// - Open: Conectado e pronto
// - CloseSent: Fechamento iniciado
// - CloseReceived: Fechamento recebido
// - Closed: Desconectado
// - Aborted: Conexão abortada
```

### Reconexão Automática
```csharp
// A reconexão automática é habilitada por padrão
// Você pode configurar o comportamento:
var config = WebSocketConfiguration.CreateProduction();
config.EnableAutoReconnect = true;
config.ReconnectInterval = TimeSpan.FromSeconds(5);
config.MaxReconnectAttempts = 10;
config.BackoffMultiplier = 1.5; // Aumenta o intervalo a cada tentativa
```

### Reconexão Manual
```csharp
// Desabilitar reconexão automática
var config = MercadoBitcoinClientExtensions.CreateWebSocketConfig(
    enableAutoReconnect: false);

using var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies(
    webSocketConfig: config);

// Implementar lógica personalizada
client.WebSocket.Disconnected += async (sender, args) =>
{
    Console.WriteLine($"Desconectado: {args.Reason}");
    
    // Aguardar antes de tentar reconectar
    await Task.Delay(TimeSpan.FromSeconds(10));
    
    try
    {
        await client.WebSocket.ConnectAsync();
        // Re-inscrever em canais necessários
        await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Falha na reconexão: {ex.Message}");
    }
};
```

## Tratamento de Erros

```csharp
client.WebSocket.Error += (sender, args) =>
{
    Console.WriteLine($"Erro WebSocket: {args.Exception.Message}");
    
    // Tipos de erro comuns:
    // - Timeout de conexão
    // - Erro de rede
    // - Erro de autenticação (se aplicável)
    // - Erro de parsing de mensagem
};

// Usar try-catch para operações críticas
try
{
    await client.WebSocket.ConnectAsync();
    await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Timeout na conexão: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro geral: {ex.Message}");
}
```

## Uso com CancellationToken

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    await client.WebSocket.ConnectAsync(cts.Token);
    await client.WebSocket.SubscribeToTradesAsync("BTC-BRL", cts.Token);
    
    // Aguardar indefinidamente ou até cancelamento
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operação cancelada");
}
```

## Múltiplos Símbolos

```csharp
var symbols = new[] { "BTC-BRL", "ETH-BRL", "LTC-BRL", "XRP-BRL" };

// Inscrever-se em trades para múltiplos símbolos
foreach (var symbol in symbols)
{
    await client.WebSocket.SubscribeToTradesAsync(symbol);
    await Task.Delay(100); // Pequeno delay entre inscrições
}

// Ou usar o método de conveniência
await client.WebSocket.SubscribeToMultipleChannelsAsync(
    "BTC-BRL",
    includeTrades: true,
    includeOrderBook: true,
    includeTicker: true,
    includeCandles: false);
```

## Performance e Boas Práticas

### 1. Gerenciamento de Recursos
```csharp
// Sempre usar 'using' para garantir limpeza adequada
using var client = MercadoBitcoinClientExtensions.CreateForTrading();

// Ou dispose manual
var client = MercadoBitcoinClientExtensions.CreateForTrading();
try
{
    // usar cliente
}
finally
{
    client.Dispose();
}
```

### 2. Evitar Sobrecarga
```csharp
// Não se inscrever em muitos canais desnecessários
// Foque apenas nos dados que você realmente precisa

// ❌ Ruim - muitos canais
await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
await client.WebSocket.SubscribeToOrderBookAsync("BTC-BRL");
await client.WebSocket.SubscribeToTickerAsync("BTC-BRL");
// ... para 20+ símbolos

// ✅ Bom - apenas o necessário
await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
await client.WebSocket.SubscribeToTickerAsync("BTC-BRL");
```

### 3. Processamento Assíncrono
```csharp
// Para processamento pesado, use Task.Run
client.WebSocket.TradeReceived += async (sender, args) =>
{
    // Processamento rápido no thread principal
    var trade = args.Data;
    
    // Processamento pesado em background
    _ = Task.Run(async () =>
    {
        await ProcessTradeAsync(trade);
    });
};
```

## Exemplos Completos

Veja a classe `WebSocketUsageExample` para exemplos completos e funcionais de:
- Uso básico
- Configuração avançada
- Tratamento de eventos
- Reconexão manual
- Uso com CancellationToken

## Troubleshooting

### Problema: Conexão não estabelece
```csharp
// Verificar configuração de rede
// Habilitar logging detalhado
var config = MercadoBitcoinClientExtensions.CreateWebSocketConfig(
    enableDetailedLogging: true);
```

### Problema: Reconexão constante
```csharp
// Aumentar timeouts
var config = WebSocketConfiguration.CreateProduction();
config.ConnectionTimeout = TimeSpan.FromSeconds(30);
config.PingTimeout = TimeSpan.FromSeconds(20);
```

### Problema: Perda de mensagens
```csharp
// Verificar se o processamento de eventos não está bloqueando
// Use processamento assíncrono para operações pesadas
client.WebSocket.TradeReceived += async (sender, args) =>
{
    // ❌ Não faça isso - bloqueia o recebimento
    Thread.Sleep(1000);
    
    // ✅ Faça isso - não bloqueia
    _ = Task.Run(() => ProcessTrade(args.Data));
};
```

## Limitações

- **Apenas canais públicos**: Este cliente suporta apenas dados públicos de mercado
- **Rate limiting**: Respeite os limites da API para evitar bloqueios
- **Conexão única**: Uma instância do cliente mantém uma única conexão WebSocket
- **Threading**: Os eventos são disparados no thread do WebSocket - evite processamento pesado

## Suporte

Para dúvidas, problemas ou sugestões:
1. Verifique os exemplos na pasta `Examples/`
2. Consulte a documentação oficial da API do Mercado Bitcoin
3. Abra uma issue no repositório do projeto