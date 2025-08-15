using Newtonsoft.Json;
using System;

namespace MercadoBitcoin.Client.WebSocket.Models
{
    /// <summary>
    /// Mensagem base para comunicação WebSocket
    /// </summary>
    public abstract class WebSocketMessage
    {
        /// <summary>
        /// Tipo da mensagem
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp da mensagem
        /// </summary>
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// Data e hora da mensagem
        /// </summary>
        [JsonIgnore]
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).DateTime;
    }

    /// <summary>
    /// Mensagem de inscrição em canal
    /// </summary>
    public class SubscribeMessage : WebSocketMessage
    {
        public SubscribeMessage()
        {
            Type = "subscribe";
        }

        /// <summary>
        /// Canal a ser inscrito
        /// </summary>
        [JsonProperty("channel")]
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// Símbolo do par de negociação (ex: BTC-BRL)
        /// </summary>
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }
    }

    /// <summary>
    /// Mensagem de cancelamento de inscrição em canal
    /// </summary>
    public class UnsubscribeMessage : WebSocketMessage
    {
        public UnsubscribeMessage()
        {
            Type = "unsubscribe";
        }

        /// <summary>
        /// Canal a ser desinscrito
        /// </summary>
        [JsonProperty("channel")]
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// Símbolo do par de negociação (ex: BTC-BRL)
        /// </summary>
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }
    }

    /// <summary>
    /// Mensagem de erro
    /// </summary>
    public class ErrorMessage : WebSocketMessage
    {
        public ErrorMessage()
        {
            Type = "error";
        }

        /// <summary>
        /// Código do erro
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Mensagem de erro
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mensagem de confirmação de inscrição
    /// </summary>
    public class SubscriptionConfirmMessage : WebSocketMessage
    {
        public SubscriptionConfirmMessage()
        {
            Type = "subscribed";
        }

        /// <summary>
        /// Canal inscrito
        /// </summary>
        [JsonProperty("channel")]
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// Símbolo do par de negociação
        /// </summary>
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }
    }

    /// <summary>
    /// Mensagem de ping para manter a conexão ativa
    /// </summary>
    public class PingMessage : WebSocketMessage
    {
        public PingMessage()
        {
            Type = MessageTypes.Ping;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}