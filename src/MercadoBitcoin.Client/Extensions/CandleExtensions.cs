using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Models;


namespace MercadoBitcoin.Client.Extensions
{
    /// <summary>
    /// Extensions for working with candle data
    /// </summary>
    public static class CandleExtensions
    {
        private static readonly Dictionary<string, string> ResolutionMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Mapping of different formats to the API default format
            { "1m", "1m" },
            { "1min", "1m" },
            { "1minute", "1m" },
            { "5m", "5m" },
            { "5min", "5m" },
            { "5minutes", "5m" },
            { "15m", "15m" },
            { "15min", "15m" },
            { "15minutes", "15m" },
            { "30m", "30m" },
            { "30min", "30m" },
            { "30minutes", "30m" },
            { "1h", "1h" },
            { "1hour", "1h" },
            { "3h", "3h" },
            { "3hour", "3h" },
            { "4h", "4h" },
            { "4hour", "4h" },
            { "6h", "6h" },
            { "6hour", "6h" },
            { "12h", "12h" },
            { "12hour", "12h" },
            { "1d", "1d" },
            { "1day", "1d" },
            { "daily", "1d" },
            { "1w", "1w" },
            { "1week", "1w" },
            { "weekly", "1w" },
            { "1month", "1M" },
            { "monthly", "1M" }
        };

        /// <summary>
        /// Normalizes the symbol to the format expected by the API (BTC-BRL)
        /// </summary>
        /// <param name="symbol">Symbol in original format</param>
        /// <returns>Normalized symbol</returns>
        public static string NormalizeSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));

            // Removes spaces and converts to uppercase
            var normalizedSymbol = symbol.Trim().ToUpperInvariant();

            // If already in correct format (contains hyphen), return as is
            if (normalizedSymbol.Contains("-"))
                return normalizedSymbol;

            // Tries to map common formats without hyphen to with hyphen
            // Ex: BTCBRL -> BTC-BRL, btcbrl -> BTC-BRL
            var commonMappings = new Dictionary<string, string>
            {
                { "BTCBRL", "BTC-BRL" },
                { "ETHBRL", "ETH-BRL" },
                { "LTCBRL", "LTC-BRL" },
                { "XRPBRL", "XRP-BRL" },
                { "BCHBRL", "BCH-BRL" },
                { "ADABRL", "ADA-BRL" },
                { "DOTBRL", "DOT-BRL" },
                { "LINKBRL", "LINK-BRL" },
                { "USDCBRL", "USDC-BRL" },
                { "USDTBRL", "USDT-BRL" }
            };

            if (commonMappings.TryGetValue(normalizedSymbol, out var mappedSymbol))
                return mappedSymbol;

            // If cannot map, assumes it needs to add hyphen in the middle
            // For 6-character symbols (3+3), adds hyphen in the middle
            if (normalizedSymbol.Length == 6)
            {
                return $"{normalizedSymbol.Substring(0, 3)}-{normalizedSymbol.Substring(3)}";
            }

            // For other cases, returns as received
            return normalizedSymbol;
        }

        /// <summary>
        /// Normalizes the resolution/timeframe to the format expected by the API
        /// </summary>
        /// <param name="resolution">Resolution in original format</param>
        /// <returns>Normalized resolution</returns>
        public static string NormalizeResolution(string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
                throw new ArgumentException("Resolution cannot be null or empty.", nameof(resolution));

            var trimmedResolution = resolution.Trim();

            if (ResolutionMapping.TryGetValue(trimmedResolution, out var normalizedResolution))
                return normalizedResolution;

            // If mapping not found, returns as received
            return trimmedResolution;
        }

        /// <summary>
        /// Converts ListCandlesResponse to CandleData list
        /// </summary>
        /// <param name="response">Candles API response</param>
        /// <param name="symbol">Trading pair symbol</param>
        /// <param name="interval">Candle interval</param>
        /// <returns>List of CandleData</returns>
        public static List<CandleData> ToCandleDataList(this ListCandlesResponse response, string symbol, string interval)
        {
            if (response == null)
                return new List<CandleData>();

            var candleCount = response.T?.Count ?? 0;
            if (candleCount == 0)
                return new List<CandleData>();

            var candles = new List<CandleData>();

            // Converts parallel arrays into CandleData objects
            for (int i = 0; i < candleCount; i++)
            {
                var candle = new CandleData
                {
                    Symbol = symbol,
                    Interval = interval,
                    OpenTime = GetValueAtIndex(response.T, i) * 1000L, // Converts to milliseconds
                    CloseTime = GetValueAtIndex(response.T, i) * 1000L + GetIntervalInMilliseconds(interval),
                    Open = ParseDecimal(GetValueAtIndex(response.O, i)),
                    High = ParseDecimal(GetValueAtIndex(response.H, i)),
                    Low = ParseDecimal(GetValueAtIndex(response.L, i)),
                    Close = ParseDecimal(GetValueAtIndex(response.C, i)),
                    Volume = ParseDecimal(GetValueAtIndex(response.V, i))
                };

                candles.Add(candle);
            }

            return candles;
        }

        /// <summary>
        /// Gets the value at the specified index of a collection, or default value if not exists
        /// </summary>
        private static T? GetValueAtIndex<T>(ICollection<T>? collection, int index)
        {
            if (collection == null || index >= collection.Count)
                return default(T);

            return collection.ElementAtOrDefault(index);
        }

        /// <summary>
        /// Safely converts string to decimal
        /// </summary>
        private static decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            return 0m;
        }

        /// <summary>
        /// Calculates the interval duration in milliseconds
        /// </summary>
        private static long GetIntervalInMilliseconds(string interval)
        {
            var normalizedInterval = NormalizeResolution(interval);

            return normalizedInterval switch
            {
                "1m" => 60 * 1000L,
                "5m" => 5 * 60 * 1000L,
                "15m" => 15 * 60 * 1000L,
                "30m" => 30 * 60 * 1000L,
                "1h" => 60 * 60 * 1000L,
                "3h" => 3 * 60 * 60 * 1000L,
                "4h" => 4 * 60 * 60 * 1000L,
                "6h" => 6 * 60 * 60 * 1000L,
                "12h" => 12 * 60 * 60 * 1000L,
                "1d" => 24 * 60 * 60 * 1000L,
                "1w" => 7 * 24 * 60 * 60 * 1000L,
                "1M" => 30 * 24 * 60 * 60 * 1000L, // Approximation
                _ => 60 * 1000L // Default to 1 minute
            };
        }

        /// <summary>
        /// Validates if the resolution is supported by the API
        /// </summary>
        /// <param name="resolution">Resolution to be validated</param>
        /// <returns>True if resolution is valid</returns>
        public static bool IsValidResolution(string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
                return false;

            var normalizedResolution = NormalizeResolution(resolution);
            var validResolutions = new[] { "1m", "5m", "15m", "30m", "1h", "3h", "4h", "6h", "12h", "1d", "1w", "1M" };

            return validResolutions.Contains(normalizedResolution);
        }

        /// <summary>
        /// Validates if the symbol has a valid format
        /// </summary>
        /// <param name="symbol">Symbol to be validated</param>
        /// <returns>True if symbol is valid</returns>
        public static bool IsValidSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            var normalizedSymbol = NormalizeSymbol(symbol);

            // Checks if it has BASE-QUOTE format
            return normalizedSymbol.Contains("-") && normalizedSymbol.Split('-').Length == 2;
        }
    }
}