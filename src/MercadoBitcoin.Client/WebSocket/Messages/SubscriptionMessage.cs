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
/// </summary>
public sealed class SubscriptionDetails
{
    /// <summary>
    /// The channel to subscribe to: "ticker", "trades", or "orderbook".
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// Alias for channel.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The trading instrument (e.g., "BTC-BRL").
    /// </summary>
    [JsonPropertyName("instrument")]
    public string Instrument { get; init; } = string.Empty;

    /// <summary>
    /// Alias for instrument.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }
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
