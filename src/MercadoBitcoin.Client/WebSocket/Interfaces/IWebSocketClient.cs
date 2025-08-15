using MercadoBitcoin.Client.WebSocket.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.WebSocket.Interfaces
{
    /// <summary>
    /// Interface para cliente WebSocket do Mercado Bitcoin
    /// </summary>
    public interface IWebSocketClient : IDisposable
    {
        /// <summary>
        /// Estado atual da conexão
        /// </summary>
        WebSocketState State { get; }

        /// <summary>
        /// Indica se a conexão está ativa
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Evento disparado quando a conexão é estabelecida
        /// </summary>
        event EventHandler? Connected;

        /// <summary>
        /// Evento disparado quando a conexão é perdida
        /// </summary>
        event EventHandler<DisconnectedEventArgs>? Disconnected;

        /// <summary>
        /// Evento disparado quando ocorre um erro
        /// </summary>
        event EventHandler<ErrorEventArgs>? Error;

        /// <summary>
        /// Evento disparado quando uma mensagem é recebida
        /// </summary>
        event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        /// <summary>
        /// Evento disparado quando dados de trade são recebidos
        /// </summary>
        event EventHandler<TradeData>? TradeReceived;

        /// <summary>
        /// Evento disparado quando dados do orderbook são recebidos
        /// </summary>
        event EventHandler<OrderBookData>? OrderBookReceived;

        /// <summary>
        /// Evento disparado quando atualizações do orderbook são recebidas
        /// </summary>
        event EventHandler<OrderBookUpdateData>? OrderBookUpdateReceived;

        /// <summary>
        /// Evento disparado quando dados do ticker são recebidos
        /// </summary>
        event EventHandler<TickerData>? TickerReceived;

        /// <summary>
        /// Evento disparado quando dados de candle são recebidos
        /// </summary>
        event EventHandler<CandleData>? CandleReceived;

        /// <summary>
        /// Conecta ao WebSocket
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Desconecta do WebSocket
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Inscreve-se em um canal
        /// </summary>
        /// <param name="channel">Canal a ser inscrito</param>
        /// <param name="symbol">Símbolo do par de negociação (opcional)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task SubscribeAsync(string channel, string? symbol = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela inscrição em um canal
        /// </summary>
        /// <param name="channel">Canal a ser desinscrito</param>
        /// <param name="symbol">Símbolo do par de negociação (opcional)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task UnsubscribeAsync(string channel, string? symbol = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia uma mensagem personalizada
        /// </summary>
        /// <param name="message">Mensagem a ser enviada</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia uma mensagem de texto
        /// </summary>
        /// <param name="text">Texto a ser enviado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task SendTextAsync(string text, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Argumentos do evento de desconexão
    /// </summary>
    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Motivo da desconexão
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Indica se foi uma desconexão limpa
        /// </summary>
        public bool WasClean { get; }

        /// <summary>
        /// Código de status da desconexão
        /// </summary>
        public int? StatusCode { get; }

        public DisconnectedEventArgs(string reason, bool wasClean, int? statusCode = null)
        {
            Reason = reason;
            WasClean = wasClean;
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Argumentos do evento de erro
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Exceção que causou o erro
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Mensagem de erro
        /// </summary>
        public string Message { get; }

        public ErrorEventArgs(Exception exception, string? message = null)
        {
            Exception = exception;
            Message = message ?? exception.Message;
        }
    }

    /// <summary>
    /// Argumentos do evento de mensagem recebida
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Mensagem recebida em formato de texto
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Mensagem deserializada (se possível)
        /// </summary>
        public WebSocketMessage? Message { get; }

        public MessageReceivedEventArgs(string text, WebSocketMessage? message = null)
        {
            Text = text;
            Message = message;
        }
    }

    /// <summary>
    /// Argumentos do evento de trade recebido
    /// </summary>
    public class TradeReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Dados do trade
        /// </summary>
        public TradeData Trade { get; }

        public TradeReceivedEventArgs(TradeData trade)
        {
            Trade = trade;
        }
    }

    /// <summary>
    /// Argumentos do evento de order book recebido
    /// </summary>
    public class OrderBookReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Dados do order book
        /// </summary>
        public OrderBookData OrderBook { get; }

        public OrderBookReceivedEventArgs(OrderBookData orderBook)
        {
            OrderBook = orderBook;
        }
    }

    /// <summary>
    /// Argumentos do evento de ticker recebido
    /// </summary>
    public class TickerReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Dados do ticker
        /// </summary>
        public TickerData Ticker { get; }

        public TickerReceivedEventArgs(TickerData ticker)
        {
            Ticker = ticker;
        }
    }

    /// <summary>
    /// Argumentos do evento de candle recebido
    /// </summary>
    public class CandleReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Dados do candle
        /// </summary>
        public CandleData Candle { get; }

        public CandleReceivedEventArgs(CandleData candle)
        {
            Candle = candle;
        }
    }
}