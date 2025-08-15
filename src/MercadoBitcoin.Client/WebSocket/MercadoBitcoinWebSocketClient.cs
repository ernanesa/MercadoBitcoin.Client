using MercadoBitcoin.Client.WebSocket.Interfaces;
using MercadoBitcoin.Client.WebSocket.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WebSocketState = MercadoBitcoin.Client.WebSocket.Models.WebSocketState;
using ErrorEventArgs = MercadoBitcoin.Client.WebSocket.Interfaces.ErrorEventArgs;

namespace MercadoBitcoin.Client.WebSocket
{
    /// <summary>
    /// Cliente WebSocket para Mercado Bitcoin
    /// </summary>
    public class MercadoBitcoinWebSocketClient : IWebSocketClient
    {
        private readonly IWebSocketConfiguration _configuration;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly ConcurrentDictionary<string, bool> _subscriptions = new();
        private System.Timers.Timer? _pingTimer;
        private System.Timers.Timer? _reconnectTimer;
        private int _reconnectAttempts = 0;
        private bool _disposed = false;
        private bool _isReconnecting = false;
        private WebSocketState _state = WebSocketState.Disconnected;

        /// <inheritdoc />
        public WebSocketState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged(value);
                }
            }
        }

        /// <inheritdoc />
        public bool IsConnected => State == WebSocketState.Connected;

        /// <inheritdoc />
        public event EventHandler? Connected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs>? Disconnected;

        /// <inheritdoc />
        public event EventHandler<ErrorEventArgs>? Error;

        /// <inheritdoc />
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        /// <inheritdoc />
        public event EventHandler<TradeData>? TradeReceived;

        /// <inheritdoc />
        public event EventHandler<OrderBookData>? OrderBookReceived;

        /// <inheritdoc />
        public event EventHandler<OrderBookUpdateData>? OrderBookUpdateReceived;

        /// <inheritdoc />
        public event EventHandler<TickerData>? TickerReceived;

        /// <inheritdoc />
        public event EventHandler<CandleData>? CandleReceived;

        /// <summary>
        /// Evento disparado quando o estado da conexão muda
        /// </summary>
        public event EventHandler<WebSocketState>? StateChanged;

        /// <summary>
        /// Cria uma nova instância do cliente WebSocket
        /// </summary>
        /// <param name="configuration">Configuração do cliente</param>
        public MercadoBitcoinWebSocketClient(IWebSocketConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Cria uma nova instância com configuração padrão
        /// </summary>
        public MercadoBitcoinWebSocketClient() : this(WebSocketConfiguration.CreateProduction())
        {
        }

        /// <inheritdoc />
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (IsConnected)
                return;

            State = WebSocketState.Connecting;

            try
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _webSocket = new ClientWebSocket();

                // Configurar cabeçalhos
                foreach (var header in _configuration.Headers)
                {
                    _webSocket.Options.SetRequestHeader(header.Key, header.Value);
                }

                // Configurar timeouts
                var connectTimeout = TimeSpan.FromSeconds(_configuration.ConnectionTimeoutSeconds);
                using var timeoutCts = new CancellationTokenSource(connectTimeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, timeoutCts.Token);

                await _webSocket.ConnectAsync(new Uri(_configuration.Url), combinedCts.Token);

                State = WebSocketState.Connected;
                _reconnectAttempts = 0;

                // Iniciar tarefas de background
                _ = Task.Run(ReceiveLoop, _cancellationTokenSource.Token);
                StartPingTimer();

                OnConnected();
            }
            catch (Exception ex)
            {
                State = WebSocketState.Error;
                OnError(ex);
                
                if (_configuration.EnableAutoReconnect && !_isReconnecting)
                {
                    _ = Task.Run(() => StartReconnectProcess());
                }
                
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                return;

            State = WebSocketState.Disconnecting;

            try
            {
                StopTimers();
                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", cancellationToken);
                }

                State = WebSocketState.Disconnected;
                OnDisconnected("Client disconnect", true);
            }
            catch (Exception ex)
            {
                State = WebSocketState.Error;
                OnError(ex);
                throw;
            }
            finally
            {
                CleanupConnection();
            }
        }

        /// <inheritdoc />
        public async Task SubscribeAsync(string channel, string? symbol = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotConnected();

            var subscribeMessage = new SubscribeMessage
            {
                Channel = channel,
                Symbol = symbol,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await SendMessageAsync(subscribeMessage, cancellationToken);

            var key = CreateSubscriptionKey(channel, symbol);
            _subscriptions.TryAdd(key, true);
        }

        /// <inheritdoc />
        public async Task UnsubscribeAsync(string channel, string? symbol = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotConnected();

            var unsubscribeMessage = new UnsubscribeMessage
            {
                Channel = channel,
                Symbol = symbol,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await SendMessageAsync(unsubscribeMessage, cancellationToken);

            var key = CreateSubscriptionKey(channel, symbol);
            _subscriptions.TryRemove(key, out _);
        }

        /// <inheritdoc />
        public async Task SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(message, Formatting.None);
            await SendTextAsync(json, cancellationToken);
        }

        /// <inheritdoc />
        public async Task SendTextAsync(string text, CancellationToken cancellationToken = default)
        {
            ThrowIfNotConnected();

            var buffer = Encoding.UTF8.GetBytes(text);
            var segment = new ArraySegment<byte>(buffer);

            await _webSocket!.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[_configuration.ReceiveBufferSize];

            try
            {
                while (_webSocket?.State == System.Net.WebSockets.WebSocketState.Open && !_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        State = WebSocketState.Disconnected;
                        OnDisconnected("Server closed connection", true, (int?)result.CloseStatus);
                        
                        if (_configuration.EnableAutoReconnect && !_isReconnecting)
                        {
                            _ = Task.Run(() => StartReconnectProcess());
                        }
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelamento normal
            }
            catch (Exception ex)
            {
                State = WebSocketState.Error;
                OnError(ex);
                
                if (_configuration.EnableAutoReconnect && !_isReconnecting)
                {
                    _ = Task.Run(() => StartReconnectProcess());
                }
            }
        }

        private async Task ProcessMessage(string messageText)
        {
            try
            {
                // Primeiro, tentar deserializar como mensagem genérica para obter o tipo
                var baseMessage = JsonConvert.DeserializeObject<WebSocketMessage>(messageText);
                if (baseMessage == null) return;

                WebSocketMessage? typedMessage = baseMessage.Type switch
                {
                    MessageTypes.Trade => JsonConvert.DeserializeObject<TradeData>(messageText),
                    MessageTypes.OrderBook => JsonConvert.DeserializeObject<OrderBookData>(messageText),
                    MessageTypes.OrderBookUpdate => JsonConvert.DeserializeObject<OrderBookUpdateData>(messageText),
                    MessageTypes.Ticker => JsonConvert.DeserializeObject<TickerData>(messageText),
                    MessageTypes.Candle => JsonConvert.DeserializeObject<CandleData>(messageText),
                    MessageTypes.Error => JsonConvert.DeserializeObject<ErrorMessage>(messageText),
                    MessageTypes.Subscribed => JsonConvert.DeserializeObject<SubscriptionConfirmMessage>(messageText),
                    _ => baseMessage
                };

                OnMessageReceived(messageText, typedMessage);

                // Disparar eventos específicos
                switch (typedMessage)
                {
                    case TradeData trade:
                        OnTradeReceived(trade);
                        break;
                    case OrderBookData orderBook:
                        OnOrderBookReceived(orderBook);
                        break;
                    case OrderBookUpdateData orderBookUpdate:
                        OnOrderBookUpdateReceived(orderBookUpdate);
                        break;
                    case TickerData ticker:
                        OnTickerReceived(ticker);
                        break;
                    case CandleData candle:
                        OnCandleReceived(candle);
                        break;
                    case ErrorMessage error:
                        OnError(new Exception($"WebSocket error: {error.Code} - {error.Message}"));
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError(ex, $"Error processing message: {messageText}");
            }
        }

        private void StartPingTimer()
        {
            if (_configuration.PingIntervalSeconds <= 0) return;

            _pingTimer = new System.Timers.Timer(_configuration.PingIntervalSeconds * 1000);
            _pingTimer.Elapsed += async (sender, e) => await SendPing();
            _pingTimer.AutoReset = true;
            _pingTimer.Start();
        }

        private async Task SendPing()
        {
            try
            {
                if (IsConnected)
                {
                    var pingMessage = new PingMessage();
                    await SendMessageAsync(pingMessage);
                }
            }
            catch (Exception ex)
            {
                OnError(ex, "Error sending ping");
            }
        }

        private async Task StartReconnectProcess()
        {
            if (_isReconnecting || _disposed) return;

            _isReconnecting = true;

            try
            {
                while (_reconnectAttempts < _configuration.MaxReconnectAttempts && !_disposed)
                {
                    _reconnectAttempts++;
                    
                    var delay = CalculateReconnectDelay();
                    await Task.Delay(delay);

                    if (_disposed) break;

                    try
                    {
                        await ConnectAsync();
                        
                        // Reinscrever em canais
                        await ResubscribeToChannels();
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnError(ex, $"Reconnect attempt {_reconnectAttempts} failed");
                    }
                }
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        private async Task ResubscribeToChannels()
        {
            foreach (var subscription in _subscriptions.Keys)
            {
                try
                {
                    var parts = subscription.Split('|');
                    var channel = parts[0];
                    var symbol = parts.Length > 1 ? parts[1] : null;
                    
                    await SubscribeAsync(channel, symbol);
                    await Task.Delay(100); // Pequeno delay entre inscrições
                }
                catch (Exception ex)
                {
                    OnError(ex, $"Error resubscribing to {subscription}");
                }
            }
        }

        private TimeSpan CalculateReconnectDelay()
        {
            var baseDelay = _configuration.ReconnectIntervalSeconds;
            var exponentialDelay = baseDelay * Math.Pow(_configuration.ReconnectBackoffMultiplier, _reconnectAttempts - 1);
            var finalDelay = Math.Min(exponentialDelay, _configuration.MaxReconnectIntervalSeconds);
            
            return TimeSpan.FromSeconds(finalDelay);
        }

        private void StopTimers()
        {
            _pingTimer?.Stop();
            _pingTimer?.Dispose();
            _pingTimer = null;

            _reconnectTimer?.Stop();
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }

        private void CleanupConnection()
        {
            _webSocket?.Dispose();
            _webSocket = null;
            
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private static string CreateSubscriptionKey(string channel, string? symbol)
        {
            return string.IsNullOrEmpty(symbol) ? channel : $"{channel}|{symbol}";
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MercadoBitcoinWebSocketClient));
        }

        private void ThrowIfNotConnected()
        {
            ThrowIfDisposed();
            if (!IsConnected)
                throw new InvalidOperationException("WebSocket is not connected");
        }

        // Event handlers
        protected virtual void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);
        protected virtual void OnDisconnected(string reason, bool wasClean, int? statusCode = null) => 
            Disconnected?.Invoke(this, new DisconnectedEventArgs(reason, wasClean, statusCode));
        protected virtual void OnError(Exception exception, string? message = null) => 
            Error?.Invoke(this, new ErrorEventArgs(exception, message));
        protected virtual void OnMessageReceived(string text, WebSocketMessage? message) => 
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(text, message));
        protected virtual void OnTradeReceived(TradeData trade) => TradeReceived?.Invoke(this, trade);
        protected virtual void OnOrderBookReceived(OrderBookData orderBook) => OrderBookReceived?.Invoke(this, orderBook);
        protected virtual void OnOrderBookUpdateReceived(OrderBookUpdateData update) => OrderBookUpdateReceived?.Invoke(this, update);
        protected virtual void OnTickerReceived(TickerData ticker) => TickerReceived?.Invoke(this, ticker);
        protected virtual void OnCandleReceived(CandleData candle) => CandleReceived?.Invoke(this, candle);
        protected virtual void OnStateChanged(WebSocketState state) => StateChanged?.Invoke(this, state);

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            
            try
            {
                DisconnectAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignorar erros durante dispose
            }
            
            StopTimers();
            CleanupConnection();
            
            GC.SuppressFinalize(this);
        }
    }
}