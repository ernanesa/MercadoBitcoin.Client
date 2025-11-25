namespace MercadoBitcoin.Client.WebSocket;

/// <summary>
/// Represents the available WebSocket channels for subscription.
/// </summary>
public static class WebSocketChannel
{
    /// <summary>
    /// Real-time ticker updates with price and volume information.
    /// </summary>
    public const string Ticker = "ticker";

    /// <summary>
    /// Real-time trade execution updates.
    /// </summary>
    public const string Trades = "trades";

    /// <summary>
    /// Real-time order book updates with bids and asks.
    /// </summary>
    public const string OrderBook = "orderbook";
}

/// <summary>
/// Connection state of the WebSocket client.
/// </summary>
public enum WebSocketConnectionState
{
    /// <summary>
    /// Not connected to the server.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Currently attempting to connect.
    /// </summary>
    Connecting,

    /// <summary>
    /// Successfully connected and ready.
    /// </summary>
    Connected,

    /// <summary>
    /// Currently attempting to reconnect after disconnection.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// Connection closed gracefully.
    /// </summary>
    Closed,

    /// <summary>
    /// Connection failed with an error.
    /// </summary>
    Failed
}
