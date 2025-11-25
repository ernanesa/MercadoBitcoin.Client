using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// Real-time order book update message from WebSocket stream.
/// </summary>
public sealed class OrderBookMessage : WebSocketMessageBase
{
    /// <summary>
    /// The order book data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public OrderBookData? Data { get; init; }
}

/// <summary>
/// Order book data containing bids and asks.
/// </summary>
public sealed class OrderBookData
{
    /// <summary>
    /// List of bid orders [price, quantity].
    /// </summary>
    [JsonPropertyName("bids")]
    public IReadOnlyList<decimal[]>? Bids { get; init; }

    /// <summary>
    /// List of ask orders [price, quantity].
    /// </summary>
    [JsonPropertyName("asks")]
    public IReadOnlyList<decimal[]>? Asks { get; init; }

    /// <summary>
    /// Gets the best bid price (highest buy order).
    /// </summary>
    public decimal? BestBidPrice => Bids?.Count > 0 ? Bids[0][0] : null;

    /// <summary>
    /// Gets the best bid quantity.
    /// </summary>
    public decimal? BestBidQuantity => Bids?.Count > 0 ? Bids[0][1] : null;

    /// <summary>
    /// Gets the best ask price (lowest sell order).
    /// </summary>
    public decimal? BestAskPrice => Asks?.Count > 0 ? Asks[0][0] : null;

    /// <summary>
    /// Gets the best ask quantity.
    /// </summary>
    public decimal? BestAskQuantity => Asks?.Count > 0 ? Asks[0][1] : null;

    /// <summary>
    /// Calculates the spread between best ask and best bid.
    /// </summary>
    public decimal? Spread => BestAskPrice.HasValue && BestBidPrice.HasValue
        ? BestAskPrice.Value - BestBidPrice.Value
        : null;

    /// <summary>
    /// Calculates the mid-market price.
    /// </summary>
    public decimal? MidPrice => BestAskPrice.HasValue && BestBidPrice.HasValue
        ? (BestAskPrice.Value + BestBidPrice.Value) / 2
        : null;
}
