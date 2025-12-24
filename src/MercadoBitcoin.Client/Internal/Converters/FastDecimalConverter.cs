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
            // Fast path: numeric token already parsed by Utf8JsonReader
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetDecimal(out var numeric))
                {
                    return numeric;
                }

                // Fallback to span parsing when TryGetDecimal fails (rare edge cases)
                var span = reader.HasValueSequence
                    ? reader.ValueSequence.ToArray().AsSpan()
                    : reader.ValueSpan;

                if (TryParseDecimal(span, out var parsedFromNumber))
                {
                    return parsedFromNumber;
                }

                throw new JsonException("Unable to parse numeric token as decimal.");
            }

            // String token: parse directly from UTF-8 bytes to avoid allocating a string
            if (reader.TokenType == JsonTokenType.String)
            {
                if (!reader.HasValueSequence)
                {
                    var span = reader.ValueSpan;
                    if (TryParseDecimal(span, out var value))
                    {
                        return value;
                    }
                }
                else
                {
                    // Handle segmented value using a pooled buffer when necessary
                    var length = (int)reader.ValueSequence.Length;
                    if (length <= 256)
                    {
                        Span<byte> buffer = stackalloc byte[length];
                        reader.ValueSequence.CopyTo(buffer);
                        if (TryParseDecimal(buffer, out var value))
                        {
                            return value;
                        }
                    }
                    else
                    {
                        var rented = ArrayPool<byte>.Shared.Rent(length);
                        try
                        {
                            var span = rented.AsSpan(0, length);
                            reader.ValueSequence.CopyTo(span);
                            if (TryParseDecimal(span, out var value))
                            {
                                return value;
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(rented);
                        }
                    }
                }

                // Last-resort fallback: allocate string for unusual formats
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
            writer.WriteNumberValue(value);
        }

        private static bool TryParseDecimal(ReadOnlySpan<byte> span, out decimal value)
        {
            return Utf8Parser.TryParse(span, out value, out _, standardFormat: 'G');
        }
    }
}
