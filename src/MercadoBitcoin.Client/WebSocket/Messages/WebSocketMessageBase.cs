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
    /// Alias for instrument used in some message types.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the effective instrument name from either Instrument or Id property.
    /// </summary>
    [JsonIgnore]
    public string? EffectiveInstrument => !string.IsNullOrEmpty(Instrument) ? Instrument : Id;

    /// <summary>
    /// Server timestamp in milliseconds since Unix epoch.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    /// <summary>
    /// Alias for timestamp used in some message types (e.g. ticker).
    /// </summary>
    [JsonPropertyName("ts")]
    public long Ts { get; init; }

    /// <summary>
    /// Gets the effective timestamp from either Timestamp or Ts property.
    /// </summary>
    [JsonIgnore]
    public long EffectiveTimestamp => Timestamp > 0 ? Timestamp : Ts;
}
