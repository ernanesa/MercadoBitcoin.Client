using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> Pool =
        new DefaultObjectPoolProvider().CreateStringBuilderPool();

    public static StringBuilder Rent()
    {
        return Pool.Get();
    }

    public static void Return(StringBuilder sb)
    {
        Pool.Return(sb);
    }
}
