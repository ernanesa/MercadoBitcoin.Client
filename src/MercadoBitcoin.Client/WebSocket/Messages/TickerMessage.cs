using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// Real-time ticker update message from WebSocket stream.
/// </summary>
public sealed class TickerMessage : WebSocketMessageBase
{
    /// <summary>
    /// The ticker data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public TickerData? Data { get; init; }
}

/// <summary>
/// Ticker data containing current market prices and volume.
/// </summary>
public readonly record struct TickerData
{
    /// <summary>
    /// Last trade price.
    /// </summary>
    [JsonPropertyName("last")]
    public decimal Last { get; init; }

    /// <summary>
    /// Highest price in the last 24 hours.
    /// </summary>
    [JsonPropertyName("high")]
    public decimal High { get; init; }

    /// <summary>
    /// Lowest price in the last 24 hours.
    /// </summary>
    [JsonPropertyName("low")]
    public decimal Low { get; init; }

    /// <summary>
    /// Trading volume in the last 24 hours.
    /// </summary>
    [JsonPropertyName("vol")]
    public decimal Volume { get; init; }

    /// <summary>
    /// Best bid (buy) price.
    /// </summary>
    [JsonPropertyName("buy")]
    public decimal BestBid { get; init; }

    /// <summary>
    /// Best ask (sell) price.
    /// </summary>
    [JsonPropertyName("sell")]
    public decimal BestAsk { get; init; }

    /// <summary>
    /// Opening price for the current period.
    /// </summary>
    [JsonPropertyName("open")]
    public decimal Open { get; init; }

    /// <summary>
    /// Calculates the spread between best ask and best bid.
    /// </summary>
    public decimal Spread => BestAsk - BestBid;

    /// <summary>
    /// Calculates the 24-hour price change percentage.
    /// </summary>
    public decimal ChangePercent => Open > 0 ? ((Last - Open) / Open) * 100 : 0;
}
