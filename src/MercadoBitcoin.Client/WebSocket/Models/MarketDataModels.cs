using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MercadoBitcoin.Client.WebSocket.Models
{
    /// <summary>
    /// Dados de trade em tempo real
    /// </summary>
    public class TradeData : WebSocketMessage
    {
        public TradeData()
        {
            Type = "trade";
        }

        /// <summary>
        /// Símbolo do par de negociação
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// ID único do trade
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Preço do trade
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Quantidade negociada
        /// </summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Lado do trade (buy/sell)
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp do trade
        /// </summary>
        [JsonProperty("trade_timestamp")]
        public long TradeTimestamp { get; set; }

        /// <summary>
        /// Data e hora do trade
        /// </summary>
        [JsonIgnore]
        public DateTime TradeDateTime => DateTimeOffset.FromUnixTimeMilliseconds(TradeTimestamp).DateTime;
    }

    /// <summary>
    /// Entrada do livro de ofertas
    /// </summary>
    public class OrderBookEntry
    {
        /// <summary>
        /// Preço da oferta
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Quantidade disponível no preço
        /// </summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Número de ordens no preço
        /// </summary>
        [JsonProperty("orders")]
        public int Orders { get; set; }
    }

    /// <summary>
    /// Dados do livro de ofertas (orderbook)
    /// </summary>
    public class OrderBookData : WebSocketMessage
    {
        public OrderBookData()
        {
            Type = "orderbook";
        }

        /// <summary>
        /// Símbolo do par de negociação
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Ofertas de compra (bids)
        /// </summary>
        [JsonProperty("bids")]
        public List<OrderBookEntry> Bids { get; set; } = new List<OrderBookEntry>();

        /// <summary>
        /// Ofertas de venda (asks)
        /// </summary>
        [JsonProperty("asks")]
        public List<OrderBookEntry> Asks { get; set; } = new List<OrderBookEntry>();

        /// <summary>
        /// Sequência do orderbook para controle de ordem
        /// </summary>
        [JsonProperty("sequence")]
        public long Sequence { get; set; }
    }

    /// <summary>
    /// Atualização incremental do livro de ofertas
    /// </summary>
    public class OrderBookUpdateData : WebSocketMessage
    {
        public OrderBookUpdateData()
        {
            Type = "orderbook_update";
        }

        /// <summary>
        /// Símbolo do par de negociação
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Lado da atualização (bid/ask)
        /// </summary>
        [JsonProperty("side")]
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Preço da atualização
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Nova quantidade (0 = remover nível)
        /// </summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Sequência da atualização
        /// </summary>
        [JsonProperty("sequence")]
        public long Sequence { get; set; }
    }

    /// <summary>
    /// Dados do ticker em tempo real
    /// </summary>
    public class TickerData : WebSocketMessage
    {
        public TickerData()
        {
            Type = "ticker";
        }

        /// <summary>
        /// Símbolo do par de negociação
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Último preço negociado
        /// </summary>
        [JsonProperty("last")]
        public decimal Last { get; set; }

        /// <summary>
        /// Melhor oferta de compra
        /// </summary>
        [JsonProperty("bid")]
        public decimal Bid { get; set; }

        /// <summary>
        /// Melhor oferta de venda
        /// </summary>
        [JsonProperty("ask")]
        public decimal Ask { get; set; }

        /// <summary>
        /// Preço de abertura (24h)
        /// </summary>
        [JsonProperty("open")]
        public decimal Open { get; set; }

        /// <summary>
        /// Preço máximo (24h)
        /// </summary>
        [JsonProperty("high")]
        public decimal High { get; set; }

        /// <summary>
        /// Preço mínimo (24h)
        /// </summary>
        [JsonProperty("low")]
        public decimal Low { get; set; }

        /// <summary>
        /// Volume negociado (24h)
        /// </summary>
        [JsonProperty("volume")]
        public decimal Volume { get; set; }

        /// <summary>
        /// Variação percentual (24h)
        /// </summary>
        [JsonProperty("change")]
        public decimal Change { get; set; }
    }

    /// <summary>
    /// Dados de candle (OHLCV)
    /// </summary>
    public class CandleData : WebSocketMessage
    {
        public CandleData()
        {
            Type = "candle";
        }

        /// <summary>
        /// Símbolo do par de negociação
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Intervalo do candle (1m, 5m, 1h, etc.)
        /// </summary>
        [JsonProperty("interval")]
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp de abertura do candle
        /// </summary>
        [JsonProperty("open_time")]
        public long OpenTime { get; set; }

        /// <summary>
        /// Timestamp de fechamento do candle
        /// </summary>
        [JsonProperty("close_time")]
        public long CloseTime { get; set; }

        /// <summary>
        /// Preço de abertura
        /// </summary>
        [JsonProperty("open")]
        public decimal Open { get; set; }

        /// <summary>
        /// Preço máximo
        /// </summary>
        [JsonProperty("high")]
        public decimal High { get; set; }

        /// <summary>
        /// Preço mínimo
        /// </summary>
        [JsonProperty("low")]
        public decimal Low { get; set; }

        /// <summary>
        /// Preço de fechamento
        /// </summary>
        [JsonProperty("close")]
        public decimal Close { get; set; }

        /// <summary>
        /// Volume negociado
        /// </summary>
        [JsonProperty("volume")]
        public decimal Volume { get; set; }

        /// <summary>
        /// Data e hora de abertura
        /// </summary>
        [JsonIgnore]
        public DateTime OpenDateTime => DateTimeOffset.FromUnixTimeMilliseconds(OpenTime).DateTime;

        /// <summary>
        /// Data e hora de fechamento
        /// </summary>
        [JsonIgnore]
        public DateTime CloseDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CloseTime).DateTime;
    }
}