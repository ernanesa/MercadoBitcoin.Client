using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client.WebSocket.Messages;

/// <summary>
/// Base class for all WebSocket messages from Mercado Bitcoin.
/// </summary>
public abstract class WebSocketMessageBase
{
    /// <summary>
    /// The type of the message (e.g., "ticker", "trades", "orderbook", "subscribed", "error").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// The trading instrument this message relates to (e.g., "BTC-BRL").
    /// </summary>
    [JsonPropertyName("instrument")]
    public string? Instrument { get; init; }

    /// <summary>
    /// Server timestamp in milliseconds since Unix epoch.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }
}
