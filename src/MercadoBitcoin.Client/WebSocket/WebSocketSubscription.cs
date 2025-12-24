namespace MercadoBitcoin.Client.WebSocket;

/// <summary>
/// Represents a WebSocket subscription that can be disposed to unsubscribe.
/// </summary>
public sealed class WebSocketSubscription : IAsyncDisposable
{
    private readonly Func<ValueTask> _unsubscribeAction;
    private bool _disposed;

    internal WebSocketSubscription(string channel, string symbol, Func<ValueTask> unsubscribeAction)
    {
        Channel = channel;
        Symbol = symbol;
        _unsubscribeAction = unsubscribeAction;
    }

    /// <summary>
    /// Gets the subscription channel name.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Gets the subscribed symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Unsubscribes and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _unsubscribeAction().ConfigureAwait(false);
    }
}
