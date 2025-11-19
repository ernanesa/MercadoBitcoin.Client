using System;

namespace MercadoBitcoin.Client.Internal.Optimization;

public ref struct ValueStringBuilder
{
    private Span<char> _buffer;
    private int _position;

    public ValueStringBuilder(Span<char> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        value.CopyTo(_buffer.Slice(_position));
        _position += value.Length;
    }

    public void Append(char value)
    {
        _buffer[_position++] = value;
    }

    public int Length => _position;

    public override string ToString()
    {
        return _buffer.Slice(0, _position).ToString();
    }

    public void Dispose()
    {
        // No-op for stack-allocated buffer
    }
}
