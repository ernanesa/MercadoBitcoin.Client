using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// WebSocket subscription request message.
/// </summary>
internal sealed class WebSocketSubscriptionRequest
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("subscription")]
    public required SubscriptionDetails Subscription { get; init; }
}

/// <summary>
/// Subscription details.
/// </summary>
internal sealed class SubscriptionDetails
{
    [JsonPropertyName("channel")]
    public required string Channel { get; init; }

    [JsonPropertyName("instrument")]
    public required string Instrument { get; init; }
}
