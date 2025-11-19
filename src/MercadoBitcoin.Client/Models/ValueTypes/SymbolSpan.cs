using System;

namespace MercadoBitcoin.Client.Models.ValueTypes;

public readonly struct SymbolSpan
{
    public ReadOnlyMemory<char> Value { get; }

    public SymbolSpan(ReadOnlyMemory<char> value)
    {
        Value = value;
    }

    public SymbolSpan(string value)
    {
        Value = value.AsMemory();
    }

    public override string ToString() => Value.ToString();

    public ReadOnlySpan<char> Span => Value.Span;

    public static implicit operator SymbolSpan(string value) => new SymbolSpan(value);
    public static implicit operator ReadOnlyMemory<char>(SymbolSpan symbol) => symbol.Value;
}
