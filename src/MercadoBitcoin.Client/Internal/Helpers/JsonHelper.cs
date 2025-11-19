using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;

namespace MercadoBitcoin.Client.Internal.Helpers
{
    /// <summary>
    /// Helper centralizes serialization using Source Generation to avoid dynamic code in AOT.
    /// For unregistered types, falls back to default options (may still generate warning if uncontrolled, but minimizes main surfaces).
    /// </summary>
    internal static class JsonHelper
    {
        public static byte[] SerializeToUtf8Bytes<T>(T value)
        {
            var typeInfo = GetTypeInfo<T>() ?? throw new NotSupportedException($"Type not registered for source-gen JSON: {typeof(T).FullName}. Add [JsonSerializable] to context.");
            using var buffer = new MemoryStream();
            using var writer = new Utf8JsonWriter(buffer);
            JsonSerializer.Serialize(writer, value, typeInfo);
            writer.Flush();
            return buffer.ToArray();
        }

        public static T? Deserialize<T>(string json)
        {
            var typeInfo = GetTypeInfo<T>() ?? throw new NotSupportedException($"Type not registered for source-gen JSON: {typeof(T).FullName}. Add [JsonSerializable] to context.");
            return (T?)JsonSerializer.Deserialize(json, typeInfo);
        }

        public static async Task<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken cancellationToken)
        {
            var typeInfo = GetTypeInfo<T>() ?? throw new NotSupportedException($"Type not registered for source-gen JSON: {typeof(T).FullName}. Add [JsonSerializable] to context.");
            return await JsonSerializer.DeserializeAsync(utf8Json, typeInfo, cancellationToken).ConfigureAwait(false);
        }

        private static System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>? GetTypeInfo<T>()
        {
            // Main map: add new DTOs if necessary
            return typeof(T) switch
            {
                var type when type == typeof(ErrorResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.ErrorResponse,
                var type when type == typeof(AccountResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AccountResponse,
                var type when type == typeof(AuthorizeRequest) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AuthorizeRequest,
                var type when type == typeof(AuthorizeResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AuthorizeResponse,
                var type when type == typeof(CancelOpenOrdersResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.CancelOpenOrdersResponse,
                var type when type == typeof(GetTierResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.GetTierResponse,
                var type when type == typeof(GetMarketFeesResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.GetMarketFeesResponse,
                var type when type == typeof(CryptoBalanceResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.CryptoBalanceResponse,
                var type when type == typeof(PlaceOrderRequest) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.PlaceOrderRequest,
                var type when type == typeof(PlaceOrderResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.PlaceOrderResponse,
                var type when type == typeof(CancelOrderResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.CancelOrderResponse,
                var type when type == typeof(OrderBookResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.OrderBookResponse,
                var type when type == typeof(TickerResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TickerResponse,
                var type when type == typeof(TickerResponse[]) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TickerResponseArray,
                var type when type == typeof(TradeResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TradeResponse,
                var type when type == typeof(TradeResponse[]) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TradeResponseArray,
                var type when type == typeof(ListCandlesResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.ListCandlesResponse,
                var type when type == typeof(ListSymbolInfoResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse,
                var type when type == typeof(PositionResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.PositionResponse,
                var type when type == typeof(OrderResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.OrderResponse,
                // Collection of OrderResponse: use fallback (or add manually when necessary)
                var type when type == typeof(AssetFee) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AssetFee,
                var type when type == typeof(Deposit) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Deposit,
                var type when type == typeof(Network) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Network,
                var type when type == typeof(FiatDeposit) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.FiatDeposit,
                var type when type == typeof(DepositAddresses) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.DepositAddresses,
                var type when type == typeof(Addresses) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Addresses,
                var type when type == typeof(Response) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Response,
                var type when type == typeof(Source) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Source,
                var type when type == typeof(Config) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Config,
                var type when type == typeof(Extra) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Extra,
                var type when type == typeof(Qrcode) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Qrcode,
                var type when type == typeof(Withdraw) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Withdraw,
                var type when type == typeof(WithdrawCoinRequest) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.WithdrawCoinRequest,
                var type when type == typeof(BankAccount) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.BankAccount,
                var type when type == typeof(Fees) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Fees,
                var type when type == typeof(BRLWithdrawConfig) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.BRLWithdrawConfig,
                _ => null
            };
        }
    }
}