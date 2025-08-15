using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.WebSocket.Models;
using MercadoBitcoin.Client.WebSocket.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testando funcionalidade WebSocket do MercadoBitcoin.Client...");
        
        try
        {
            // Criar cliente com configuração para trading
            var client = MercadoBitcoinClientExtensions.CreateForTrading();
            
            // Configurar eventos
            client.WebSocket.TradeReceived += OnTradeReceived;
            client.WebSocket.OrderBookReceived += OnOrderBookReceived;
            client.WebSocket.TickerReceived += OnTickerReceived;
            client.WebSocket.CandleReceived += OnCandleReceived;
            
            // Conectar
            Console.WriteLine("Conectando ao WebSocket...");
            await client.WebSocket.ConnectAsync();
            
            // Inscrever-se em canais
            Console.WriteLine("Inscrevendo-se nos canais...");
            await client.WebSocket.SubscribeToTradesAsync("BTC-BRL");
            await client.WebSocket.SubscribeToOrderBookAsync("BTC-BRL");
            await client.WebSocket.SubscribeToTickerAsync("BTC-BRL");
            await client.WebSocket.SubscribeToCandlesAsync("BTC-BRL", CandleIntervals.OneMinute);
            
            Console.WriteLine("Conectado e inscrito! Pressione qualquer tecla para sair...");
            
            // Aguardar por 30 segundos ou até o usuário pressionar uma tecla
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var keyTask = Task.Run(() => Console.ReadKey());
            var timeoutTask = Task.Delay(30000, cts.Token);
            
            await Task.WhenAny(keyTask, timeoutTask);
            
            // Desconectar
            Console.WriteLine("\nDesconectando...");
            await client.WebSocket.DisconnectAsync();
            
            Console.WriteLine("Teste concluído com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro durante o teste: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    private static void OnTradeReceived(object? sender, TradeData trade)
    {
        Console.WriteLine($"Trade recebido: {trade.Symbol} - Preço: {trade.Price}, Quantidade: {trade.Amount}");
    }
    
    private static void OnOrderBookReceived(object? sender, OrderBookData orderBook)
    {
        Console.WriteLine($"OrderBook recebido: {orderBook.Symbol} - Bids: {orderBook.Bids?.Count ?? 0}, Asks: {orderBook.Asks?.Count ?? 0}");
    }
    
    private static void OnTickerReceived(object? sender, TickerData ticker)
    {
        Console.WriteLine($"Ticker recebido: {ticker.Symbol} - Último preço: {ticker.Last}");
    }
    
    private static void OnCandleReceived(object? sender, CandleData candle)
    {
        Console.WriteLine($"Candle recebido: {candle.Symbol} - Open: {candle.Open}, Close: {candle.Close}");
    }
}
