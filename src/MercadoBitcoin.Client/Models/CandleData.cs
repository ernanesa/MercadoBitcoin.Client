using System;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;

namespace MercadoBitcoin.Client.Models
{
    /// <summary>
    /// Representa uma vela (candle) OHLCV otimizada para alta performance (Beast Mode).
    /// </summary>
    /// <remarks>
    /// Transformado em 'readonly record struct' com 'StructLayout' para garantir
    /// alocação contígua em memória e zero-overhead de classe.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct CandleData
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; init; }

        [JsonPropertyName("interval")]
        public string Interval { get; init; }

        [JsonPropertyName("openTime")]
        public long OpenTime { get; init; }

        [JsonPropertyName("closeTime")]
        public long CloseTime { get; init; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; init; }

        [JsonPropertyName("open")]
        public decimal Open { get; init; }

        [JsonPropertyName("high")]
        public decimal High { get; init; }

        [JsonPropertyName("low")]
        public decimal Low { get; init; }

        [JsonPropertyName("close")]
        public decimal Close { get; init; }

        [JsonPropertyName("volume")]
        public decimal Volume { get; init; }

        // Construtor otimizado
        public CandleData(string symbol, string interval, long openTime, long closeTime, long timestamp, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            Symbol = symbol;
            Interval = interval;
            OpenTime = openTime;
            CloseTime = closeTime;
            Timestamp = timestamp;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
    }
}