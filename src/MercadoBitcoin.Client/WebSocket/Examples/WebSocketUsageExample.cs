using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.WebSocket.Extensions;
using MercadoBitcoin.Client.WebSocket.Interfaces;
using MercadoBitcoin.Client.WebSocket.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.WebSocket.Examples
{
    /// <summary>
    /// Exemplos práticos de uso do WebSocket do Mercado Bitcoin
    /// </summary>
    public static class WebSocketUsageExample
    {
        /// <summary>
        /// Exemplo básico de conexão e inscrição em canais
        /// </summary>
        public static async Task BasicUsageExample()
        {
            // Criar cliente com configuração padrão
            using var client = MercadoBitcoinClientExtensions.CreateForTrading();
            
            // Configurar eventos
            client.WebSocket.Connected += (sender, args) => 
                Console.WriteLine("WebSocket conectado!");
                
            client.WebSocket.Disconnected += (sender, args) => 
                Console.WriteLine($"WebSocket desconectado: {args.Reason}");
                
            client.WebSocket.Error += (sender, args) => 
                Console.WriteLine($"Erro no WebSocket: {args.Exception.Message}");

            // Conectar e inscrever-se em canais para BTC-BRL
            await client.WebSocket.ConnectAndSubscribeAsync(
                "BTC-BRL",
                includeTrades: true,
                includeOrderBook: true,
                includeTicker: true,
                includeCandles: true,
                candleInterval: CandleIntervals.OneMinute);

            Console.WriteLine("Pressione qualquer tecla para parar...");
            Console.ReadKey();
        }

        /// <summary>
        /// Exemplo avançado com tratamento de eventos específicos
        /// </summary>
        public static async Task AdvancedUsageExample()
        {
            // Criar cliente com configuração personalizada
            var webSocketConfig = MercadoBitcoinClientExtensions.CreateWebSocketConfig(
                enableAutoReconnect: true,
                reconnectInterval: TimeSpan.FromSeconds(2),
                maxReconnectAttempts: 10,
                enableDetailedLogging: true);
                
            using var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies(
                webSocketConfig: webSocketConfig);

            // Configurar eventos específicos para cada tipo de dados
            client.WebSocket.TradeReceived += OnTradeReceived;
            client.WebSocket.OrderBookReceived += OnOrderBookReceived;
            client.WebSocket.TickerReceived += OnTickerReceived;
            client.WebSocket.CandleReceived += OnCandleReceived;

            // Conectar
            await client.WebSocket.ConnectAsync();
            
            // Aguardar conexão estável
            await Task.Delay(1000);

            // Inscrever-se em múltiplos símbolos
            var symbols = new[] { "BTC-BRL", "ETH-BRL", "LTC-BRL" };
            
            foreach (var symbol in symbols)
            {
                await client.WebSocket.SubscribeToTradesAsync(symbol);
                await client.WebSocket.SubscribeToTickerAsync(symbol);
                await Task.Delay(100); // Pequeno delay entre inscrições
            }

            // Inscrever-se em orderbook apenas para BTC-BRL
            await client.WebSocket.SubscribeToOrderBookAsync("BTC-BRL");
            
            // Inscrever-se em candles de diferentes intervalos para BTC-BRL
            await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.OneMinute);
            await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.FiveMinutes);
            await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.OneHour);

            Console.WriteLine("WebSocket configurado e rodando. Pressione 'q' para sair...");
            
            // Loop para manter o programa rodando
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    break;
                    
                if (key.KeyChar == 's')
                {
                    // Mostrar estatísticas
                    Console.WriteLine($"Estado da conexão: {client.WebSocket.State}");
                    Console.WriteLine($"Conectado: {client.WebSocket.IsConnected}");
                }
            }
        }

        /// <summary>
        /// Exemplo de uso com CancellationToken
        /// </summary>
        public static async Task CancellationTokenExample()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5 minutos timeout
            using var client = MercadoBitcoinClientExtensions.CreateForDevelopment();

            try
            {
                // Configurar eventos
                client.WebSocket.TradeReceived += (sender, args) =>
                {
                    Console.WriteLine($"Trade: {args.Symbol} - {args.Price} - {args.Amount}");
                };

                // Conectar com timeout
                await client.WebSocket.ConnectAsync(cts.Token);
                
                // Inscrever-se com timeout
                await client.WebSocket.SubscribeToTradesAsync("BTC-BRL", cts.Token);
                await client.WebSocket.SubscribeToTickerAsync("BTC-BRL", cts.Token);

                // Aguardar até o timeout ou cancelamento manual
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operação cancelada por timeout ou solicitação do usuário.");
            }
        }

        /// <summary>
        /// Exemplo de reconexão manual
        /// </summary>
        public static async Task ManualReconnectionExample()
        {
            // Criar cliente sem reconexão automática
            var config = MercadoBitcoinClientExtensions.CreateWebSocketConfig(
                enableAutoReconnect: false);
                
            using var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies(
                webSocketConfig: config);

            client.WebSocket.Disconnected += async (sender, args) =>
            {
                Console.WriteLine($"Desconectado: {args.Reason}. Tentando reconectar em 5 segundos...");
                
                await Task.Delay(5000);
                
                try
                {
                    await client.WebSocket.ConnectAsync();
                    await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
                    Console.WriteLine("Reconectado com sucesso!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Falha na reconexão: {ex.Message}");
                }
            };

            await client.WebSocket.ConnectAsync();
            await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");

            Console.WriteLine("Pressione qualquer tecla para parar...");
            Console.ReadKey();
        }

        #region Event Handlers

        private static void OnTradeReceived(object? sender, TradeData trade)
        {
            Console.WriteLine($"[TRADE] {trade.Symbol}: {trade.Price:F2} x {trade.Amount:F8} ({trade.Side}) - {trade.TradeDateTime:HH:mm:ss.fff}");
        }

        private static void OnOrderBookReceived(object? sender, OrderBookData orderBook)
        {
            Console.WriteLine($"[ORDERBOOK] {orderBook.Symbol}: {orderBook.Bids?.Count ?? 0} bids, {orderBook.Asks?.Count ?? 0} asks");
            
            // Mostrar melhor bid e ask
            if (orderBook.Bids?.Count > 0)
                Console.WriteLine($"  Melhor Bid: {orderBook.Bids[0].Price:F2} x {orderBook.Bids[0].Amount:F8}");
                
            if (orderBook.Asks?.Count > 0)
                Console.WriteLine($"  Melhor Ask: {orderBook.Asks[0].Price:F2} x {orderBook.Asks[0].Amount:F8}");
        }

        private static void OnTickerReceived(object? sender, TickerData ticker)
        {
            Console.WriteLine($"[TICKER] {ticker.Symbol}: Last={ticker.Last:F2}, High={ticker.High:F2}, Low={ticker.Low:F2}, Vol={ticker.Volume:F2}");
        }

        private static void OnCandleReceived(object? sender, CandleData candle)
        {
            Console.WriteLine($"[CANDLE] {candle.Symbol} ({candle.Interval}): O={candle.Open:F2}, H={candle.High:F2}, L={candle.Low:F2}, C={candle.Close:F2}, V={candle.Volume:F2} - {candle.OpenDateTime:HH:mm:ss.fff}");
        }

        #endregion
    }
}