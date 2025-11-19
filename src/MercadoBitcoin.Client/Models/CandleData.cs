using System;
using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.Models
{
    /// <summary>
    /// Represents candle data (OHLCV) for technical analysis.
    /// Optimized as a readonly struct to reduce heap allocations in large lists.
    /// </summary>
    public readonly struct CandleData
    {
        /// <summary>
        /// Default constructor required for deserialization in some scenarios,
        /// but System.Text.Json supports init-only properties.
        /// </summary>
        public CandleData()
        {
            Symbol = string.Empty;
            Interval = string.Empty;
            OpenTime = 0;
            CloseTime = 0;
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Volume = 0;
        }

        /// <summary>
        /// Trading pair symbol (e.g., BTC-BRL)
        /// </summary>
        [JsonPropertyName("symbol")]
        public string Symbol { get; init; }

        /// <summary>
        /// Candle interval (e.g., 1m, 5m, 15m, 1h, 1d)
        /// </summary>
        [JsonPropertyName("interval")]
        public string Interval { get; init; }

        /// <summary>
        /// Candle opening timestamp (in milliseconds)
        /// </summary>
        [JsonPropertyName("open_time")]
        public long OpenTime { get; init; }

        /// <summary>
        /// Candle closing timestamp (in milliseconds)
        /// </summary>
        [JsonPropertyName("close_time")]
        public long CloseTime { get; init; }

        /// <summary>
        /// Opening price
        /// </summary>
        [JsonPropertyName("open")]
        public decimal Open { get; init; }

        /// <summary>
        /// Highest price
        /// </summary>
        [JsonPropertyName("high")]
        public decimal High { get; init; }

        /// <summary>
        /// Lowest price
        /// </summary>
        [JsonPropertyName("low")]
        public decimal Low { get; init; }

        /// <summary>
        /// Closing price
        /// </summary>
        [JsonPropertyName("close")]
        public decimal Close { get; init; }

        /// <summary>
        /// Traded volume
        /// </summary>
        [JsonPropertyName("volume")]
        public decimal Volume { get; init; }

        /// <summary>
        /// Candle opening date/time
        /// </summary>
        [JsonIgnore]
        public DateTime OpenDateTime => DateTimeOffset.FromUnixTimeMilliseconds(OpenTime).DateTime;

        /// <summary>
        /// Candle closing date/time
        /// </summary>
        [JsonIgnore]
        public DateTime CloseDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CloseTime).DateTime;

        /// <summary>
        /// Returns a string representation of the candle
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol} {Interval} - Open: {Open}, High: {High}, Low: {Low}, Close: {Close}, Volume: {Volume}";
        }
    }
}