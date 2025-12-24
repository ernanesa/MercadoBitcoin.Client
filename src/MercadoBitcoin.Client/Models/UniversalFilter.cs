using System;
using System.Collections.Generic;

namespace MercadoBitcoin.Client.Models
{
    /// <summary>
    /// A universal filter for API requests, supporting symbols, pagination, and time ranges.
    /// </summary>
    public record UniversalFilter
    {
        /// <summary>
        /// List of symbols to filter by (e.g., BTC-BRL, ETH-BRL).
        /// If null or empty, may imply "all symbols" depending on the endpoint.
        /// </summary>
        public IEnumerable<string>? Symbols { get; init; }

        /// <summary>
        /// Start time for the filter (Unix timestamp in seconds).
        /// </summary>
        public long? From { get; init; }

        /// <summary>
        /// End time for the filter (Unix timestamp in seconds).
        /// </summary>
        public long? To { get; init; }

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int? Limit { get; init; }

        /// <summary>
        /// Offset for pagination.
        /// </summary>
        public int? Offset { get; init; }

        /// <summary>
        /// Page number for pagination.
        /// </summary>
        public int? Page { get; init; }

        /// <summary>
        /// Whether to include details in the response.
        /// </summary>
        public bool? IncludeDetails { get; init; }

        /// <summary>
        /// Creates a filter for a single symbol.
        /// </summary>
        public static UniversalFilter ForSymbol(string symbol) => new() { Symbols = new[] { symbol } };

        /// <summary>
        /// Creates a filter for multiple symbols.
        /// </summary>
        public static UniversalFilter ForSymbols(params string[] symbols) => new() { Symbols = symbols };
    }
}
