using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// Message sent to subscribe or unsubscribe from a WebSocket channel.
/// </summary>
public sealed class SubscriptionRequest
{
    /// <summary>
    /// The action type: "subscribe" or "unsubscribe".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "subscribe";

    /// <summary>
    /// The subscription details.
    /// </summary>
    [JsonPropertyName("subscription")]
    public SubscriptionDetails Subscription { get; init; } = new();
}

/// <summary>
/// Details of a subscription request.
/// Per official API: https://ws.mercadobitcoin.net/docs/v0/#/api/GeneralMessages
/// Required fields: id (market e.g., "BRLBTC"), name (subscription type: ticker, orderbook, trades)
/// </summary>
public sealed class SubscriptionDetails
{
    /// <summary>
    /// Available market (e.g., "BRLBTC"). Note: No hyphen, format is QUOTEBASE.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Subscription type: "ticker", "orderbook", or "trades".
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Limit of items for orderbook channel. Possible values: 10, 20, 50, 100, 200.
    /// </summary>
    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Limit { get; init; }
}

/// <summary>
/// Response message confirming subscription status.
/// </summary>
public sealed class SubscriptionResponse : WebSocketMessageBase
{
    /// <summary>
    /// The channel that was subscribed/unsubscribed.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; init; }

    /// <summary>
    /// Status of the subscription: "subscribed" or "unsubscribed".
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Error message if subscription failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Returns true if the subscription was successful.
    /// </summary>
    public bool IsSuccess => string.Equals(Status, "subscribed", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(Type, "subscribed", StringComparison.OrdinalIgnoreCase);
}
