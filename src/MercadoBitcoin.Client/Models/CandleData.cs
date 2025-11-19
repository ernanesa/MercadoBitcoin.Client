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
        public string Symbol
        {
            get;
            init => field = string.Intern(value);
        }

        /// <summary>
        /// Candle interval (e.g., 1m, 5m, 15m, 1h, 1d)
        /// </summary>
        [JsonPropertyName("interval")]
        public string Interval
        {
            get;
            init => field = string.Intern(value);
        }

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
        public DateTime OpenDateTime
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => DateTimeOffset.FromUnixTimeMilliseconds(OpenTime).DateTime;
        }

        /// <summary>
        /// Candle closing date/time
        /// </summary>
        [JsonIgnore]
        public DateTime CloseDateTime
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => DateTimeOffset.FromUnixTimeMilliseconds(CloseTime).DateTime;
        }

        /// <summary>
        /// Returns a string representation of the candle
        /// </summary>
        public override string ToString()
        {
            // Stack allocation for zero-allocation formatting
            Span<char> buffer = stackalloc char[256];
            var handler = new System.Runtime.CompilerServices.DefaultInterpolatedStringHandler(
                literalLength: 45,
                formattedCount: 5,
                provider: null,
                initialBuffer: buffer);

            handler.AppendFormatted(Symbol);
            handler.AppendLiteral(" ");
            handler.AppendFormatted(Interval);
            handler.AppendLiteral(" - Open: ");
            handler.AppendFormatted(Open);
            handler.AppendLiteral(", High: ");
            handler.AppendFormatted(High);
            handler.AppendLiteral(", Low: ");
            handler.AppendFormatted(Low);
            handler.AppendLiteral(", Close: ");
            handler.AppendFormatted(Close);
            handler.AppendLiteral(", Volume: ");
            handler.AppendFormatted(Volume);

            return handler.ToStringAndClear();
        }
    }
}