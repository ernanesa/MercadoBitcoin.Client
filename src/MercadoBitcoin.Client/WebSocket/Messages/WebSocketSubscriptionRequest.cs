using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// WebSocket subscription request message.
/// </summary>
public sealed class WebSocketSubscriptionRequest
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("subscription")]
    public required SubscriptionDetails Subscription { get; init; }
}
