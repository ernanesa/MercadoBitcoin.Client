using System.Text.Json.Serialization;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.WebSocket.Messages;
using MercadoBitcoin.Client.Models;

namespace MercadoBitcoin.Client;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
)]
// WebSocket Messages
[JsonSerializable(typeof(TickerMessage))]
[JsonSerializable(typeof(TickerData))]
[JsonSerializable(typeof(TradeMessage))]
[JsonSerializable(typeof(TradeData))]
[JsonSerializable(typeof(OrderBookMessage))]
[JsonSerializable(typeof(OrderBookData))]
[JsonSerializable(typeof(SubscriptionRequest))]
[JsonSerializable(typeof(SubscriptionDetails))]
[JsonSerializable(typeof(SubscriptionResponse))]
[JsonSerializable(typeof(WebSocketMessageBase))]
// High-Performance Models
[JsonSerializable(typeof(CandleData))]
[JsonSerializable(typeof(CandleData[]))]
[JsonSerializable(typeof(ICollection<CandleData>))]
// API Types
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
[JsonSerializable(typeof(ICollection<TickerResponse>))]
[JsonSerializable(typeof(ICollection<AccountResponse>))]
[JsonSerializable(typeof(ICollection<Deposit>))]
[JsonSerializable(typeof(ICollection<FiatDeposit>))]
[JsonSerializable(typeof(ICollection<GetTierResponse>))]
[JsonSerializable(typeof(ICollection<PositionResponse>))]
[JsonSerializable(typeof(ICollection<Withdraw>))]
[JsonSerializable(typeof(ICollection<CryptoBalanceResponse>))]
[JsonSerializable(typeof(ICollection<BankAccount>))]
[JsonSerializable(typeof(ICollection<CryptoWalletAddress>))]
[JsonSerializable(typeof(TradeResponse))]
[JsonSerializable(typeof(TradeResponse[]))]
[JsonSerializable(typeof(ICollection<TradeResponse>))]
[JsonSerializable(typeof(ListCandlesResponse))]
[JsonSerializable(typeof(ListSymbolInfoResponse))]
[JsonSerializable(typeof(ListAllOrdersResponse))]
[JsonSerializable(typeof(Orders))]
[JsonSerializable(typeof(PositionResponse))]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(ICollection<OrderResponse>))]
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
[JsonSerializable(typeof(CryptoWalletAddress))]
[JsonSerializable(typeof(Fees))]
[JsonSerializable(typeof(BRLWithdrawConfig))]
[JsonSerializable(typeof(ApiException))]
[JsonSerializable(typeof(ApiException<object>))]
public partial class MercadoBitcoinJsonSerializerContext : JsonSerializerContext
{
    // Helps IL Trimmer/AOT preserve members accessed indirectly by generated code/NSwag
    // WebSocket Message Types
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(TickerMessage))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(TickerData))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(TradeMessage))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(TradeData))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(OrderBookMessage))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(OrderBookData))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(SubscriptionRequest))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(SubscriptionDetails))]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(SubscriptionResponse))]
    // High-Performance Models
    [System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(CandleData))]
    // API Types
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