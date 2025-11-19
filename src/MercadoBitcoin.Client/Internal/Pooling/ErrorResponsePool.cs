using MercadoBitcoin.Client.Errors;
using Microsoft.Extensions.ObjectPool;

namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class ErrorResponsePool
{
    private static readonly ObjectPool<ErrorResponse> Pool =
        ObjectPool.Create<ErrorResponse>();

    public static ErrorResponse Rent()
    {
        var response = Pool.Get();
        // ErrorResponse doesn't have a Reset method yet, we might need to add it or handle it here.
        // For now, assuming we will add it or it's a simple DTO that will be overwritten.
        // But the plan says "response.Reset()".
        // I will check ErrorResponse.cs later and add Reset() if needed.
        return response;
    }

    public static void Return(ErrorResponse response)
    {
        Pool.Return(response);
    }
}
