using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// Message sent to keep the WebSocket connection alive.
/// </summary>
public sealed class PingRequest
{
    /// <summary>
    /// The message type, always "ping".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "ping";

    /// <summary>
    /// Optional timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; init; }
}
