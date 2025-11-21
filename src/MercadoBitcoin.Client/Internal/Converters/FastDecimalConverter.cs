using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.Internal.Converters
{
    /// <summary>
    /// A high-performance, zero-allocation JSON converter for decimals.
    /// Reads numbers from both JSON strings and JSON numbers directly from Utf8JsonReader.
    /// </summary>
    public class FastDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                // Try to parse from the internal span of the reader if possible
                if (reader.HasValueSequence)
                {
                    // Fallback for sequences (rare for simple strings)
                    // We can use a stack buffer if small enough
                    long length = reader.ValueSequence.Length;
                    if (length < 64)
                    {
                        Span<byte> buffer = stackalloc byte[(int)length];
                        reader.ValueSequence.CopyTo(buffer);
                        if (Utf8Parser.TryParse(buffer, out decimal value, out _, 'G'))
                        {
                            return value;
                        }
                    }
                }
                else
                {
                    // Zero-allocation path
                    var span = reader.ValueSpan;
                    if (Utf8Parser.TryParse(span, out decimal value, out _, 'G'))
                    {
                        return value;
                    }
                }

                // Fallback to string allocation if Utf8Parser fails (e.g. custom format)
                var stringValue = reader.GetString();
                if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }

            throw new JsonException($"Unable to convert {reader.TokenType} to decimal.");
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            // Write as number
            writer.WriteNumberValue(value);
        }
    }
}
