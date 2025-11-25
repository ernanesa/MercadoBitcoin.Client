using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MercadoBitcoin.Client.Internal.Optimization;

/// <summary>
/// A high-performance, stack-allocated string builder that can grow using ArrayPool when needed.
/// </summary>
/// <remarks>
/// This struct is optimized for scenarios where you want to build strings without heap allocations.
/// It starts with a stack-allocated buffer and automatically rents from ArrayPool if more space is needed.
/// Always call Dispose() when done to return any rented arrays back to the pool.
/// </remarks>
public ref struct ValueStringBuilder
{
    private char[]? _arrayFromPool;
    private Span<char> _chars;
    private int _position;

    /// <summary>
    /// Creates a ValueStringBuilder with the specified initial buffer.
    /// </summary>
    /// <param name="initialBuffer">A stack-allocated or pre-existing buffer to use initially.</param>
    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayFromPool = null;
        _chars = initialBuffer;
        _position = 0;
    }

    /// <summary>
    /// Creates a ValueStringBuilder with a pooled buffer of the specified capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity to rent from the pool.</param>
    public ValueStringBuilder(int initialCapacity)
    {
        _arrayFromPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayFromPool;
        _position = 0;
    }

    /// <summary>
    /// Gets the current length of the string being built.
    /// </summary>
    public int Length => _position;

    /// <summary>
    /// Gets the current capacity of the buffer.
    /// </summary>
    public int Capacity => _chars.Length;

    /// <summary>
    /// Gets the remaining space in the buffer.
    /// </summary>
    public int RemainingCapacity => _chars.Length - _position;

    /// <summary>
    /// Gets a reference to the character at the specified index.
    /// </summary>
    public ref char this[int index] => ref _chars[index];

    /// <summary>
    /// Appends a span of characters to the builder.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            return;

        if (_position + value.Length > _chars.Length)
            Grow(value.Length);

        value.CopyTo(_chars.Slice(_position));
        _position += value.Length;
    }

    /// <summary>
    /// Appends a single character to the builder.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char value)
    {
        if (_position >= _chars.Length)
            Grow(1);

        _chars[_position++] = value;
    }

    /// <summary>
    /// Appends a string to the builder.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        Append(value.AsSpan());
    }

    /// <summary>
    /// Appends a character repeated the specified number of times.
    /// </summary>
    public void Append(char value, int repeatCount)
    {
        if (repeatCount <= 0)
            return;

        if (_position + repeatCount > _chars.Length)
            Grow(repeatCount);

        _chars.Slice(_position, repeatCount).Fill(value);
        _position += repeatCount;
    }

    /// <summary>
    /// Appends an integer value to the builder without allocation.
    /// </summary>
    public void Append(int value)
    {
        // Ensure we have enough space for int (max 11 chars including sign)
        if (_position + 11 > _chars.Length)
            Grow(11);

        if (value.TryFormat(_chars.Slice(_position), out int charsWritten))
        {
            _position += charsWritten;
        }
    }

    /// <summary>
    /// Appends a long value to the builder without allocation.
    /// </summary>
    public void Append(long value)
    {
        // Ensure we have enough space for long (max 20 chars including sign)
        if (_position + 20 > _chars.Length)
            Grow(20);

        if (value.TryFormat(_chars.Slice(_position), out int charsWritten))
        {
            _position += charsWritten;
        }
    }

    /// <summary>
    /// Appends a decimal value to the builder without allocation.
    /// </summary>
    public void Append(decimal value)
    {
        // Ensure we have enough space for decimal (max 30 chars)
        if (_position + 30 > _chars.Length)
            Grow(30);

        if (value.TryFormat(_chars.Slice(_position), out int charsWritten))
        {
            _position += charsWritten;
        }
    }

    /// <summary>
    /// Appends a line terminator to the builder.
    /// </summary>
    public void AppendLine()
    {
        Append(Environment.NewLine);
    }

    /// <summary>
    /// Appends a string followed by a line terminator.
    /// </summary>
    public void AppendLine(string? value)
    {
        Append(value);
        Append(Environment.NewLine);
    }

    /// <summary>
    /// Clears the builder, resetting the position to 0.
    /// </summary>
    public void Clear()
    {
        _position = 0;
    }

    /// <summary>
    /// Ensures the builder has at least the specified capacity.
    /// </summary>
    public void EnsureCapacity(int capacity)
    {
        if (capacity > _chars.Length)
        {
            Grow(capacity - _chars.Length);
        }
    }

    /// <summary>
    /// Gets a span representing the current content.
    /// </summary>
    public ReadOnlySpan<char> AsSpan() => _chars.Slice(0, _position);

    /// <summary>
    /// Gets a span representing a portion of the current content.
    /// </summary>
    public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _position - start);

    /// <summary>
    /// Gets a span representing a portion of the current content.
    /// </summary>
    public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    /// <summary>
    /// Converts the builder content to a string.
    /// </summary>
    public override string ToString() => _chars.Slice(0, _position).ToString();

    /// <summary>
    /// Grows the buffer to accommodate additional characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacity)
    {
        // Calculate new capacity (at least double, or enough for the new content)
        int newCapacity = Math.Max(_chars.Length * 2, _chars.Length + additionalCapacity);
        newCapacity = Math.Max(newCapacity, 256); // Minimum 256 chars

        // Rent a new array from the pool
        char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);

        // Copy existing content
        _chars.Slice(0, _position).CopyTo(newArray);

        // Return the old array if it was from the pool
        if (_arrayFromPool != null)
        {
            ArrayPool<char>.Shared.Return(_arrayFromPool);
        }

        _arrayFromPool = newArray;
        _chars = newArray;
    }

    /// <summary>
    /// Returns any rented arrays back to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_arrayFromPool != null)
        {
            ArrayPool<char>.Shared.Return(_arrayFromPool);
            _arrayFromPool = null;
        }
    }
}

