using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// Real-time trade execution message from WebSocket stream.
/// </summary>
public sealed class TradeMessage : WebSocketMessageBase
{
    /// <summary>
    /// The trade data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public TradeData? Data { get; init; }
}

/// <summary>
/// Trade data containing execution details.
/// </summary>
public readonly record struct TradeData
{
    /// <summary>
    /// Unique trade identifier.
    /// </summary>
    [JsonPropertyName("tid")]
    public long TradeId { get; init; }

    /// <summary>
    /// Trade execution price.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    /// <summary>
    /// Trade quantity/amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    /// <summary>
    /// Trade side: "buy" or "sell".
    /// </summary>
    [JsonPropertyName("side")]
    public string Side { get; init; }

    /// <summary>
    /// Trade timestamp in milliseconds since Unix epoch.
    /// </summary>
    [JsonPropertyName("date")]
    public long Date { get; init; }

    /// <summary>
    /// Returns true if this was a buy trade.
    /// </summary>
    public bool IsBuy => string.Equals(Side, "buy", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if this was a sell trade.
    /// </summary>
    public bool IsSell => string.Equals(Side, "sell", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Calculates the total value of the trade.
    /// </summary>
    public decimal TotalValue => Price * Amount;
}
