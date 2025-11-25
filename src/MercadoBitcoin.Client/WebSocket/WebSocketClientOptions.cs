namespace MercadoBitcoin.Client.WebSocket;

/// <summary>
/// Configuration options for the WebSocket client.
/// </summary>
public sealed class WebSocketClientOptions
{
    /// <summary>
    /// Default WebSocket endpoint for Mercado Bitcoin.
    /// </summary>
    public const string DefaultWebSocketUrl = "wss://ws.mercadobitcoin.net/ws";

    /// <summary>
    /// The WebSocket server URL.
    /// </summary>
    public string WebSocketUrl
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? DefaultWebSocketUrl : value;
    } = DefaultWebSocketUrl;

    /// <summary>
    /// Interval between ping messages to keep the connection alive.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan KeepAliveInterval
    {
        get;
        set => field = value < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : value;
    } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for waiting for pong responses.
    /// Default is 10 seconds.
    /// </summary>
    public TimeSpan KeepAliveTimeout
    {
        get;
        set => field = value < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : value;
    } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Whether to automatically reconnect on disconnection.
    /// Default is true.
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Maximum number of reconnection attempts before giving up.
    /// Default is 10.
    /// </summary>
    public int MaxReconnectAttempts
    {
        get;
        set => field = value < 0 ? 0 : value;
    } = 10;

    /// <summary>
    /// Initial delay before first reconnection attempt.
    /// Default is 1 second.
    /// </summary>
    public TimeSpan InitialReconnectDelay
    {
        get;
        set => field = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between reconnection attempts (exponential backoff cap).
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan MaxReconnectDelay
    {
        get;
        set => field = value < InitialReconnectDelay ? InitialReconnectDelay : value;
    } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Size of the receive buffer in bytes.
    /// Default is 8KB.
    /// </summary>
    public int ReceiveBufferSize
    {
        get;
        set => field = value < 1024 ? 1024 : value;
    } = 8 * 1024;

    /// <summary>
    /// Size of the send buffer in bytes.
    /// Default is 4KB.
    /// </summary>
    public int SendBufferSize
    {
        get;
        set => field = value < 512 ? 512 : value;
    } = 4 * 1024;

    /// <summary>
    /// Connection timeout for establishing WebSocket connection.
    /// Default is 10 seconds.
    /// </summary>
    public TimeSpan ConnectionTimeout
    {
        get;
        set => field = value < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : value;
    } = TimeSpan.FromSeconds(10);
}
