using System.Text.Json.Serialization;
using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
)]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(AccountResponse))]
[JsonSerializable(typeof(AuthorizeRequest))]
[JsonSerializable(typeof(AuthorizeResponse))]
[JsonSerializable(typeof(CancelOpenOrdersResponse))]
[JsonSerializable(typeof(GetTierResponse))]
[JsonSerializable(typeof(GetMarketFeesResponse))]
[JsonSerializable(typeof(CryptoBalanceResponse))]
[JsonSerializable(typeof(PlaceOrderRequest))]
[JsonSerializable(typeof(PlaceOrderResponse))]
[JsonSerializable(typeof(CancelOrderResponse))]
[JsonSerializable(typeof(OrderBookResponse))]
[JsonSerializable(typeof(TickerResponse))]
[JsonSerializable(typeof(TickerResponse[]))]
[JsonSerializable(typeof(TradeResponse))]
[JsonSerializable(typeof(TradeResponse[]))]
[JsonSerializable(typeof(ListCandlesResponse))]
[JsonSerializable(typeof(ListSymbolInfoResponse))]
[JsonSerializable(typeof(PositionResponse))]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(System.Collections.Generic.ICollection<OrderResponse>))]
[JsonSerializable(typeof(AssetFee))]
[JsonSerializable(typeof(Deposit))]
[JsonSerializable(typeof(Network))]
[JsonSerializable(typeof(FiatDeposit))]
[JsonSerializable(typeof(DepositAddresses))]
[JsonSerializable(typeof(Addresses))]
[JsonSerializable(typeof(Response))]
[JsonSerializable(typeof(Source))]
[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(Extra))]
[JsonSerializable(typeof(Qrcode))]
[JsonSerializable(typeof(ApiException))]
[JsonSerializable(typeof(ApiException<object>))]
public partial class MercadoBitcoinJsonSerializerContext : JsonSerializerContext
{
}