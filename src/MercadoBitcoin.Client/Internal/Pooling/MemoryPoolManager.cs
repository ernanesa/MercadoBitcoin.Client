using System.Buffers;

namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class MemoryPoolManager
{
    /// <summary>
    /// Rents memory from the pool.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="minimumLength">Minimum length needed</param>
    /// <returns>IMemoryOwner auto-disposing</returns>
    public static IMemoryOwner<T> Rent<T>(int minimumLength)
    {
        return MemoryPool<T>.Shared.Rent(minimumLength);
    }
}
