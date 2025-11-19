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

        public static async Task<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken ct)
        {
            var typeInfo = GetTypeInfo<T>() ?? throw new NotSupportedException($"Type not registered for source-gen JSON: {typeof(T).FullName}. Add [JsonSerializable] to context.");
            return await JsonSerializer.DeserializeAsync(utf8Json, typeInfo, ct).ConfigureAwait(false);
        }

        private static System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>? GetTypeInfo<T>()
        {
            // Main map: add new DTOs if necessary
            return typeof(T) switch
            {
                var t when t == typeof(ErrorResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.ErrorResponse,
                var t when t == typeof(AccountResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AccountResponse,
                var t when t == typeof(AuthorizeRequest) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AuthorizeRequest,
                var t when t == typeof(AuthorizeResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AuthorizeResponse,
                var t when t == typeof(CancelOpenOrdersResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.CancelOpenOrdersResponse,
                var t when t == typeof(GetTierResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.GetTierResponse,
                var t when t == typeof(GetMarketFeesResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.GetMarketFeesResponse,
                var t when t == typeof(CryptoBalanceResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.CryptoBalanceResponse,
                var t when t == typeof(PlaceOrderRequest) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.PlaceOrderRequest,
                var t when t == typeof(PlaceOrderResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.PlaceOrderResponse,
                var t when t == typeof(CancelOrderResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.CancelOrderResponse,
                var t when t == typeof(OrderBookResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.OrderBookResponse,
                var t when t == typeof(TickerResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TickerResponse,
                var t when t == typeof(TickerResponse[]) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TickerResponseArray,
                var t when t == typeof(TradeResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TradeResponse,
                var t when t == typeof(TradeResponse[]) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.TradeResponseArray,
                var t when t == typeof(ListCandlesResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.ListCandlesResponse,
                var t when t == typeof(ListSymbolInfoResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse,
                var t when t == typeof(PositionResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.PositionResponse,
                var t when t == typeof(OrderResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.OrderResponse,
                // Collection of OrderResponse: use fallback (or add manually when necessary)
                var t when t == typeof(AssetFee) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AssetFee,
                var t when t == typeof(Deposit) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Deposit,
                var t when t == typeof(Network) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Network,
                var t when t == typeof(FiatDeposit) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.FiatDeposit,
                var t when t == typeof(DepositAddresses) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.DepositAddresses,
                var t when t == typeof(Addresses) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Addresses,
                var t when t == typeof(Response) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Response,
                var t when t == typeof(Source) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Source,
                var t when t == typeof(Config) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Config,
                var t when t == typeof(Extra) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Extra,
                var t when t == typeof(Qrcode) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Qrcode,
                var t when t == typeof(Withdraw) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Withdraw,
                var t when t == typeof(WithdrawCoinRequest) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.WithdrawCoinRequest,
                var t when t == typeof(BankAccount) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.BankAccount,
                var t when t == typeof(Fees) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.Fees,
                var t when t == typeof(BRLWithdrawConfig) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.BRLWithdrawConfig,
                _ => null
            };
        }
    }
}