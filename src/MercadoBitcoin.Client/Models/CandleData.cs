using System;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;

namespace MercadoBitcoin.Client.Models
{
    /// <summary>
    /// Represents an optimized OHLCV candle data structure for high-performance scenarios (Beast Mode).
    /// </summary>
    /// <remarks>
    /// Implemented as a readonly record struct with StructLayout to ensure
    /// contiguous memory allocation and zero-class overhead.
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

        // Optimized constructor
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