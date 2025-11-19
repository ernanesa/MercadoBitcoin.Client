using System.Buffers;

namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class ArrayPoolManager
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Rents a byte array from the pool.
    /// </summary>
    /// <param name="minimumLength">Minimum length needed</param>
    /// <returns>Byte array (may be larger than requested)</returns>
    public static byte[] RentBytes(int minimumLength)
    {
        return BytePool.Rent(minimumLength);
    }

    /// <summary>
    /// Returns a rented byte array to the pool.
    /// </summary>
    /// <param name="array">Array to return</param>
    /// <param name="clearArray">Whether to clear before returning</param>
    public static void ReturnBytes(byte[] array, bool clearArray = true)
    {
        BytePool.Return(array, clearArray);
    }
}
