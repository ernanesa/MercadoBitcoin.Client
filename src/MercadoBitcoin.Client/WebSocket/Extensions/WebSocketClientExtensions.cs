using MercadoBitcoin.Client.WebSocket.Interfaces;
using MercadoBitcoin.Client.WebSocket.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.WebSocket.Extensions
{
    /// <summary>
    /// Métodos de extensão para facilitar o uso do cliente WebSocket
    /// </summary>
    public static class WebSocketClientExtensions
    {
        /// <summary>
        /// Inscreve-se no canal de trades para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task SubscribeToTradesAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
            
            return client.SubscribeAsync(WebSocketChannels.Trades, symbol, cancellationToken);
        }

        /// <summary>
        /// Inscreve-se no canal de orderbook para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task SubscribeToOrderBookAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
            
            return client.SubscribeAsync(WebSocketChannels.OrderBook, symbol, cancellationToken);
        }

        /// <summary>
        /// Inscreve-se no canal de atualizações do orderbook para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task SubscribeToOrderBookUpdatesAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            return client.SubscribeAsync(WebSocketChannels.OrderBookUpdate, symbol, cancellationToken);
        }

        /// <summary>
        /// Inscreve-se no canal de ticker para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task SubscribeToTickerAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
            
            return client.SubscribeAsync(WebSocketChannels.Ticker, symbol, cancellationToken);
        }

        /// <summary>
        /// Inscreve-se no canal de candles para um símbolo e intervalo específicos
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="interval">Intervalo do candle (ex: 1m, 5m, 1h)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task SubscribeToCandlesAsync(this IWebSocketClient client, string symbol, string interval = CandleIntervals.OneMinute, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
            
            if (string.IsNullOrEmpty(interval))
                throw new ArgumentNullException(nameof(interval), "Interval cannot be null or empty.");
            
            var channel = $"{WebSocketChannels.Candles}_{interval}";
            return client.SubscribeAsync(channel, symbol, cancellationToken);
        }

        /// <summary>
        /// Cancela inscrição no canal de trades para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task UnsubscribeFromTradesAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            return client.UnsubscribeAsync(WebSocketChannels.Trades, symbol, cancellationToken);
        }

        /// <summary>
        /// Cancela inscrição no canal de orderbook para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task UnsubscribeFromOrderBookAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            return client.UnsubscribeAsync(WebSocketChannels.OrderBook, symbol, cancellationToken);
        }

        /// <summary>
        /// Cancela inscrição no canal de atualizações do orderbook para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task UnsubscribeFromOrderBookUpdatesAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            return client.UnsubscribeAsync(WebSocketChannels.OrderBookUpdate, symbol, cancellationToken);
        }

        /// <summary>
        /// Cancela inscrição no canal de ticker para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task UnsubscribeFromTickerAsync(this IWebSocketClient client, string symbol, CancellationToken cancellationToken = default)
        {
            return client.UnsubscribeAsync(WebSocketChannels.Ticker, symbol, cancellationToken);
        }

        /// <summary>
        /// Cancela inscrição no canal de candles para um símbolo e intervalo específicos
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="interval">Intervalo do candle (ex: 1m, 5m, 1h)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static Task UnsubscribeFromCandlesAsync(this IWebSocketClient client, string symbol, string interval = CandleIntervals.OneMinute, CancellationToken cancellationToken = default)
        {
            var channel = $"{WebSocketChannels.Candles}_{interval}";
            return client.UnsubscribeAsync(channel, symbol, cancellationToken);
        }

        /// <summary>
        /// Inscreve-se em múltiplos canais para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="includeTrades">Incluir canal de trades</param>
        /// <param name="includeOrderBook">Incluir canal de orderbook</param>
        /// <param name="includeOrderBookUpdates">Incluir canal de atualizações do orderbook</param>
        /// <param name="includeTicker">Incluir canal de ticker</param>
        /// <param name="includeCandles">Incluir canal de candles</param>
        /// <param name="candleInterval">Intervalo dos candles</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static async Task SubscribeToMultipleChannelsAsync(
            this IWebSocketClient client,
            string symbol,
            bool includeTrades = true,
            bool includeOrderBook = true,
            bool includeOrderBookUpdates = false,
            bool includeTicker = true,
            bool includeCandles = false,
            string candleInterval = CandleIntervals.OneMinute,
            CancellationToken cancellationToken = default)
        {
            if (includeTrades)
                await client.SubscribeToTradesAsync(symbol, cancellationToken);

            if (includeOrderBook)
                await client.SubscribeToOrderBookAsync(symbol, cancellationToken);

            if (includeOrderBookUpdates)
                await client.SubscribeToOrderBookUpdatesAsync(symbol, cancellationToken);

            if (includeTicker)
                await client.SubscribeToTickerAsync(symbol, cancellationToken);

            if (includeCandles)
                await client.SubscribeToCandlesAsync(symbol, candleInterval, cancellationToken);
        }

        /// <summary>
        /// Cancela inscrição em múltiplos canais para um símbolo específico
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="includeTrades">Incluir canal de trades</param>
        /// <param name="includeOrderBook">Incluir canal de orderbook</param>
        /// <param name="includeOrderBookUpdates">Incluir canal de atualizações do orderbook</param>
        /// <param name="includeTicker">Incluir canal de ticker</param>
        /// <param name="includeCandles">Incluir canal de candles</param>
        /// <param name="candleInterval">Intervalo dos candles</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static async Task UnsubscribeFromMultipleChannelsAsync(
            this IWebSocketClient client,
            string symbol,
            bool includeTrades = true,
            bool includeOrderBook = true,
            bool includeOrderBookUpdates = false,
            bool includeTicker = true,
            bool includeCandles = false,
            string candleInterval = CandleIntervals.OneMinute,
            CancellationToken cancellationToken = default)
        {
            if (includeTrades)
                await client.UnsubscribeFromTradesAsync(symbol, cancellationToken);

            if (includeOrderBook)
                await client.UnsubscribeFromOrderBookAsync(symbol, cancellationToken);

            if (includeOrderBookUpdates)
                await client.UnsubscribeFromOrderBookUpdatesAsync(symbol, cancellationToken);

            if (includeTicker)
                await client.UnsubscribeFromTickerAsync(symbol, cancellationToken);

            if (includeCandles)
                await client.UnsubscribeFromCandlesAsync(symbol, candleInterval, cancellationToken);
        }

        /// <summary>
        /// Conecta e inscreve-se automaticamente em canais para um símbolo
        /// </summary>
        /// <param name="client">Cliente WebSocket</param>
        /// <param name="symbol">Símbolo do par de negociação (ex: BTC-BRL)</param>
        /// <param name="includeTrades">Incluir canal de trades</param>
        /// <param name="includeOrderBook">Incluir canal de orderbook</param>
        /// <param name="includeOrderBookUpdates">Incluir canal de atualizações do orderbook</param>
        /// <param name="includeTicker">Incluir canal de ticker</param>
        /// <param name="includeCandles">Incluir canal de candles</param>
        /// <param name="candleInterval">Intervalo dos candles</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static async Task ConnectAndSubscribeAsync(
            this IWebSocketClient client,
            string symbol,
            bool includeTrades = true,
            bool includeOrderBook = true,
            bool includeOrderBookUpdates = false,
            bool includeTicker = true,
            bool includeCandles = false,
            string candleInterval = CandleIntervals.OneMinute,
            CancellationToken cancellationToken = default)
        {
            await client.ConnectAsync(cancellationToken);
            
            // Pequeno delay para garantir que a conexão está estável
            await Task.Delay(500, cancellationToken);
            
            await client.SubscribeToMultipleChannelsAsync(
                symbol,
                includeTrades,
                includeOrderBook,
                includeOrderBookUpdates,
                includeTicker,
                includeCandles,
                candleInterval,
                cancellationToken);
        }
    }
}