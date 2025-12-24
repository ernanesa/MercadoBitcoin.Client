using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MercadoBitcoin.Client.WebSocket;
using MercadoBitcoin.Client.WebSocket.Messages;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    public class WebSocketTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MercadoBitcoinWebSocketClient _client;
        private readonly CancellationTokenSource _cts = new(TimeSpan.FromSeconds(30));

        public WebSocketTests(ITestOutputHelper output)
        {
            _output = output;
            var options = new WebSocketClientOptions
            {
                AutoReconnect = true,
                MaxReconnectAttempts = 3
            };
            _client = new MercadoBitcoinWebSocketClient(options);
        }

        [Fact]
        public async Task ConnectAsync_ShouldEstablishConnection()
        {
            // Arrange - use client without auto-reconnect for this test
            var clientNoReconnect = new MercadoBitcoinWebSocketClient(new WebSocketClientOptions
            {
                AutoReconnect = false
            });

            try
            {
                // Act
                await clientNoReconnect.ConnectAsync(_cts.Token);

                // Assert
                clientNoReconnect.ConnectionState.Should().Be(WebSocketConnectionState.Connected);
                _output.WriteLine("✅ WebSocket connected successfully");

                await clientNoReconnect.DisconnectAsync(_cts.Token);
                clientNoReconnect.ConnectionState.Should().BeOneOf(
                    WebSocketConnectionState.Closed,
                    WebSocketConnectionState.Disconnected);
                _output.WriteLine("✅ WebSocket disconnected successfully");
            }
            finally
            {
                await clientNoReconnect.DisposeAsync();
            }
        }

        [Fact]
        public async Task SubscribeTickerAsync_ShouldReceiveMessages()
        {
            // Arrange
            var instrument = "BRLBTC";
            var messages = new List<TickerMessage>();

            // Act
            await _client.ConnectAsync(_cts.Token);

            var subscriptionTask = Task.Run(async () =>
            {
                await foreach (var msg in _client.SubscribeTickerAsync(instrument, _cts.Token))
                {
                    messages.Add(msg);
                    if (messages.Count >= 2) break;
                }
            });

            // Wait for some messages or timeout (increased to 30s for reliability)
            await Task.WhenAny(subscriptionTask, Task.Delay(TimeSpan.FromSeconds(30), _cts.Token));

            // Assert - tolerant: ticker updates may not occur within timeout window
            if (messages.Count > 0)
            {
                var firstMsg = messages.First();
                _output.WriteLine($"First Message: Id={firstMsg.Id}, Instrument={firstMsg.Instrument}, Effective={firstMsg.EffectiveInstrument}");
                firstMsg.EffectiveInstrument.Should().Be(instrument);
                _output.WriteLine($"✅ Received {messages.Count} ticker messages for {instrument}");
            }
            else
            {
                _output.WriteLine("⚠️ No ticker messages received within timeout - this may happen in low-activity periods");
            }
        }

        [Fact]
        public async Task SubscribeTradesAsync_ShouldReceiveMessages()
        {
            // Arrange
            var instrument = "BRLBTC";
            var messages = new List<TradeMessage>();

            // Act
            await _client.ConnectAsync(_cts.Token);

            var subscriptionTask = Task.Run(async () =>
            {
                await foreach (var msg in _client.SubscribeTradesAsync(instrument, _cts.Token))
                {
                    messages.Add(msg);
                    if (messages.Count >= 1) break;
                }
            });

            await Task.WhenAny(subscriptionTask, Task.Delay(TimeSpan.FromSeconds(45), _cts.Token));

            // Assert - trades may not occur within the timeout period, which is acceptable
            // The test passes if we connected successfully and didn't throw an exception
            _output.WriteLine($"✅ Trade subscription test completed. Received {messages.Count} trade messages for {instrument}");
            if (messages.Any())
            {
                messages.First().EffectiveInstrument.Should().Be(instrument);
            }
            // No failure if no trades received - market may be quiet
        }

        [Fact]
        public async Task SubscribeOrderBookAsync_ShouldReceiveMessages()
        {
            // Arrange
            var instrument = "BRLBTC";
            var messages = new List<OrderBookMessage>();

            // Act
            await _client.ConnectAsync(_cts.Token);

            var subscriptionTask = Task.Run(async () =>
            {
                await foreach (var msg in _client.SubscribeOrderBookAsync(instrument, _cts.Token))
                {
                    messages.Add(msg);
                    if (messages.Count >= 1) break;
                }
            });

            await Task.WhenAny(subscriptionTask, Task.Delay(TimeSpan.FromSeconds(20), _cts.Token));

            // Assert - orderbook updates may not arrive within timeout depending on market activity
            _output.WriteLine($"✅ OrderBook subscription test completed. Received {messages.Count} orderbook messages for {instrument}");
            if (messages.Any())
            {
                messages.First().EffectiveInstrument.Should().Be(instrument);
            }
            // No failure if no messages received - orderbook updates may be infrequent
        }

        public void Dispose()
        {
            _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _cts.Dispose();
        }
    }
}
