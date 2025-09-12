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
[JsonSerializable(typeof(System.Collections.Generic.ICollection<TickerResponse>))]
[JsonSerializable(typeof(TradeResponse))]
[JsonSerializable(typeof(TradeResponse[]))]
[JsonSerializable(typeof(System.Collections.Generic.ICollection<TradeResponse>))]
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
[JsonSerializable(typeof(Withdraw))]
[JsonSerializable(typeof(WithdrawCoinRequest))]
[JsonSerializable(typeof(BankAccount))]
[JsonSerializable(typeof(Fees))]
[JsonSerializable(typeof(BRLWithdrawConfig))]
[JsonSerializable(typeof(ApiException))]
[JsonSerializable(typeof(ApiException<object>))]
public partial class MercadoBitcoinJsonSerializerContext : JsonSerializerContext
{
    // Ajuda o IL Trimmer/AOT a preservar membros acessados indiretamente pelo código gerado/NSwag
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(AccountResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(AuthorizeResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(PlaceOrderResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(OrderResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(TradeResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(TickerResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(ListCandlesResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(ListSymbolInfoResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(PositionResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(CryptoBalanceResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(GetMarketFeesResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(GetTierResponse))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(Withdraw))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(Deposit))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(FiatDeposit))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(DepositAddresses))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(BankAccount))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(Fees))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(BRLWithdrawConfig))]
    private static void Preserve() { }
}