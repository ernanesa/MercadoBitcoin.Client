using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace MercadoBitcoin.Client.Extensions;

/// <summary>
/// C# 14 extension members for Span&lt;T&gt; and ReadOnlySpan&lt;T&gt; providing
/// high-performance, zero-allocation operations for trading data processing.
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    /// Extension members for ReadOnlySpan&lt;char&gt; - optimized string operations.
    /// </summary>
    extension(ReadOnlySpan<char> span)
    {
        /// <summary>
        /// Attempts to parse a decimal value from the span without allocation.
        /// Returns 0m if parsing fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal ParseDecimalOrZero()
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return 0m;

            return decimal.TryParse(span, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
        }

        /// <summary>
        /// Attempts to parse a long value from the span without allocation.
        /// Returns 0 if parsing fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ParseInt64OrZero()
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return 0L;

            return long.TryParse(span, out var result) ? result : 0L;
        }

        /// <summary>
        /// Splits the span once on the specified separator.
        /// </summary>
        public bool TrySplitOnce(char separator, out ReadOnlySpan<char> left, out ReadOnlySpan<char> right)
        {
            int index = span.IndexOf(separator);
            if (index < 0)
            {
                left = span;
                right = ReadOnlySpan<char>.Empty;
                return false;
            }

            left = span[..index];
            right = span[(index + 1)..];
            return true;
        }

        /// <summary>
        /// Checks if the span equals the specified string (ordinal, case-insensitive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualsIgnoreCase(ReadOnlySpan<char> other)
        {
            return span.Equals(other, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Extension members for ReadOnlySpan&lt;byte&gt; - optimized binary data operations.
    /// </summary>
    extension(ReadOnlySpan<byte> span)
    {
        /// <summary>
        /// Converts UTF-8 bytes to string using pooled buffers for intermediate conversion.
        /// More efficient than direct Encoding.UTF8.GetString for hot paths.
        /// </summary>
        public string ToUtf8String()
        {
            if (span.IsEmpty)
                return string.Empty;

            // For small spans, use stack allocation
            if (span.Length <= 256)
            {
                Span<char> charBuffer = stackalloc char[span.Length];
                int charCount = Encoding.UTF8.GetChars(span, charBuffer);
                return new string(charBuffer[..charCount]);
            }

            // For larger spans, use pooled buffer
            char[] pooledBuffer = ArrayPool<char>.Shared.Rent(span.Length);
            try
            {
                int charCount = Encoding.UTF8.GetChars(span, pooledBuffer);
                return new string(pooledBuffer, 0, charCount);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(pooledBuffer);
            }
        }

        /// <summary>
        /// Finds the first occurrence of a UTF-8 encoded character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfUtf8Char(char character)
        {
            // For ASCII characters (single byte in UTF-8)
            if (character <= 127)
            {
                return span.IndexOf((byte)character);
            }

            // For non-ASCII, encode and search
            Span<byte> encoded = stackalloc byte[4];
            int byteCount = Encoding.UTF8.GetBytes(stackalloc char[1] { character }, encoded);
            return span.IndexOf(encoded[..byteCount]);
        }
    }

    /// <summary>
    /// Extension members for Span&lt;byte&gt; - mutable binary data operations.
    /// </summary>
    extension(Span<byte> span)
    {
        /// <summary>
        /// Writes a UTF-8 encoded string to the span.
        /// Returns the number of bytes written.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int WriteUtf8(ReadOnlySpan<char> value)
        {
            return Encoding.UTF8.GetBytes(value, span);
        }

        /// <summary>
        /// Writes a UTF-8 encoded string to the span and returns remaining space.
        /// </summary>
        public Span<byte> WriteUtf8AndSlice(ReadOnlySpan<char> value)
        {
            int bytesWritten = Encoding.UTF8.GetBytes(value, span);
            return span[bytesWritten..];
        }

        /// <summary>
        /// Fills the span with zeros efficiently.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ZeroFill()
        {
            span.Clear();
        }
    }

    /// <summary>
    /// Extension members for Memory&lt;byte&gt; - async-compatible binary operations.
    /// </summary>
    extension(Memory<byte> memory)
    {
        /// <summary>
        /// Gets a span view of the memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsWritableSpan() => memory.Span;

        /// <summary>
        /// Creates an ArraySegment from the memory (useful for older APIs).
        /// </summary>
        public ArraySegment<byte> ToArraySegment()
        {
            if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment))
            {
                return segment;
            }

            // Fallback: copy to new array
            return new ArraySegment<byte>(memory.ToArray());
        }
    }

    /// <summary>
    /// Extension members for ReadOnlyMemory&lt;byte&gt; - readonly async-compatible operations.
    /// </summary>
    extension(ReadOnlyMemory<byte> memory)
    {
        /// <summary>
        /// Converts the memory to a UTF-8 string.
        /// </summary>
        public string ToUtf8String()
        {
            return memory.Span.ToUtf8String();
        }
    }
}
