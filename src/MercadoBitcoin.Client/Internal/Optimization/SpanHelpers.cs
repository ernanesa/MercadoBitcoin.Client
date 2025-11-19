using System;

namespace MercadoBitcoin.Client.Internal.Optimization;

internal static class SpanHelpers
{
    public static bool TrySplitOnce(
        ReadOnlySpan<char> source,
        char separator,
        out ReadOnlySpan<char> left,
        out ReadOnlySpan<char> right)
    {
        int index = source.IndexOf(separator);
        if (index < 0)
        {
            left = source;
            right = ReadOnlySpan<char>.Empty;
            return false;
        }

        left = source[..index];
        right = source[(index + 1)..];
        return true;
    }

    public static ReadOnlySpan<char> Trim(ReadOnlySpan<char> span)
    {
        return span.Trim();
    }

    public static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        return first.Equals(second, StringComparison.OrdinalIgnoreCase);
    }
}
