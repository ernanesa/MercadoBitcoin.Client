using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client.Extensions
{
    /// <summary>
    /// Extension helpers to convert the weakly-typed withdraw limits <see cref="Response"/> model
    /// returned by the API into a strongly-typed dictionary structure.
    /// </summary>
    public static class WithdrawLimitsExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Attempts to parse a <see cref="Response"/> (withdraw limits endpoint raw model) into a dictionary
        /// mapping SYMBOL -&gt; LIMIT (as decimal). If parsing of an individual entry fails it is skipped
        /// unless <paramref name="throwOnError"/> is true.
        /// </summary>
        /// <param name="raw">Raw model returned by <c>GetWithdrawLimitsAsync</c>.</param>
        /// <param name="throwOnError">If true, any parsing issue will throw; otherwise faulty entries are ignored.</param>
        /// <returns>Dictionary of symbol to limit quantity (decimal) or empty dictionary if input is null.</returns>
        public static IReadOnlyDictionary<string, decimal> ToWithdrawLimitsDictionary(this Response? raw, bool throwOnError = false)
        {
            if (raw == null)
                return new Dictionary<string, decimal>();

            // The generated "Response" class has properties "Symbol" and "Example" due to an ambiguous schema.
            // The actual payload is expected to be a JSON object of the form { "BTC-BRL": "1.234", "ETH-BRL": "10" }
            // (or numeric values). We therefore serialize the raw object back to JSON and inspect nodes.
            try
            {
                // Usa contexto gerado para evitar reflection AOT; Response já está incluído no contexto
                var json = JsonSerializer.Serialize<Response>(raw, MercadoBitcoinJsonSerializerContext.Default.Response);
                var node = JsonNode.Parse(json);
                if (node is not JsonObject obj)
                    return new Dictionary<string, decimal>();

                var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in obj)
                {
                    // Skip known structural properties produced by the generator.
                    if (string.Equals(kv.Key, "symbol", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(kv.Key, "example", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (kv.Value is null)
                        continue;

                    if (TryConvertToDecimal(kv.Value, out var val))
                        result[kv.Key] = val;
                    else if (throwOnError)
                        throw new FormatException($"Could not parse withdraw limit value for symbol '{kv.Key}': {kv.Value}");
                }
                return result;
            }
            catch when (!throwOnError)
            {
                return new Dictionary<string, decimal>();
            }
        }

        private static bool TryConvertToDecimal(JsonNode valueNode, out decimal value)
        {
            switch (valueNode)
            {
                case JsonValue jsonValue:
                    if (jsonValue.TryGetValue(out decimal dec)) { value = dec; return true; }
                    if (jsonValue.TryGetValue(out double dbl)) { value = (decimal)dbl; return true; }
                    if (jsonValue.TryGetValue(out string? str) && str != null &&
                        decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out dec))
                    { value = dec; return true; }
                    break;
            }
            value = default;
            return false;
        }
    }
}
