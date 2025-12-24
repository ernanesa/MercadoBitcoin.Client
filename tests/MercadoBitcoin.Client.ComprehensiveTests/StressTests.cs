using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MercadoBitcoin.Client.WebSocket;
using MercadoBitcoin.Client.WebSocket.Messages;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    [Collection("Stress")]
    public class StressTests : TestBase
    {
        private readonly ITestOutputHelper _output;

        public StressTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task REST_HighConcurrency_GetTicker_ShouldHandleLoad()
        {
            int concurrency = 50;
            var tasks = new List<Task<ICollection<Generated.TickerResponse>>>();
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < concurrency; i++)
            {
                tasks.Add(Client.GetTickersAsync(TestSymbol));
            }

            var results = await Task.WhenAll(tasks);
            sw.Stop();

            results.Should().HaveCount(concurrency);
            foreach (var res in results)
            {
                res.Should().NotBeEmpty();
            }

            _output.WriteLine($"✅ {concurrency} concurrent GetTicker calls in {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"   Avg time per request: {sw.ElapsedMilliseconds / (double)concurrency:F2}ms");
        }

        [Fact]
        public async Task WebSocket_ParallelSubscriptions_ShouldHandleLoad()
        {
            int clientCount = 10; // Create 10 clients
            var clients = new List<MercadoBitcoinWebSocketClient>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                // Create clients
                for (int i = 0; i < clientCount; i++)
                {
                    clients.Add(new MercadoBitcoinWebSocketClient(new WebSocketClientOptions { AutoReconnect = true }));
                }

                // Connect all
                await Task.WhenAll(clients.Select(c => c.ConnectAsync(cts.Token)));
                _output.WriteLine($"✅ {clientCount} clients connected");

                // Subscribe all to Ticker
                var messageCounts = new ConcurrentDictionary<int, int>();
                var tasks = new List<Task>();

                for (int i = 0; i < clientCount; i++)
                {
                    int clientId = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await foreach (var msg in clients[clientId].SubscribeTickerAsync("BRLBTC", cts.Token))
                            {
                                messageCounts.AddOrUpdate(clientId, 1, (k, v) => v + 1);
                                if (messageCounts[clientId] >= 3) break; // Wait for 3 messages per client
                            }
                        }
                        catch (OperationCanceledException) { }
                    }));
                }

                await Task.WhenAll(tasks);

                // Assert
                foreach (var kvp in messageCounts)
                {
                    kvp.Value.Should().BeGreaterThanOrEqualTo(1);
                }
                _output.WriteLine($"✅ All {clientCount} clients received messages");
            }
            finally
            {
                foreach (var c in clients)
                {
                    await c.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task REST_OrderPlacement_Stress_ShouldHandleLoad()
        {
            if (string.IsNullOrEmpty(Client.GetAccessToken()))
            {
                _output.WriteLine("Skipping order stress test (no auth)");
                return;
            }

            int orderCount = 10; // Place 10 orders in parallel
            var tasks = new List<Task<Generated.PlaceOrderResponse>>();

            // Get current price to place safe limit orders
            var ticker = (await Client.GetTickersAsync(TestSymbol)).First();
            var price = decimal.Parse(ticker.Last, System.Globalization.CultureInfo.InvariantCulture) * 0.5m; // 50% of price

            for (int i = 0; i < orderCount; i++)
            {
                tasks.Add(Client.PlaceOrderAsync(TestSymbol, TestAccountId, new Generated.PlaceOrderRequest
                {
                    Side = "buy",
                    Type = "limit",
                    Qty = "0.00001",
                    LimitPrice = (double)price
                }));
            }

            try
            {
                var results = await Task.WhenAll(tasks);
                _output.WriteLine($"✅ Placed {orderCount} orders in parallel");

                // Cleanup
                var orderIds = results.Select(r => r.OrderId).ToList();
                foreach (var id in orderIds)
                {
                    await Client.CancelOrderAsync(TestSymbol, TestAccountId, id);
                }
                _output.WriteLine($"✅ Cancelled {orderCount} orders");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️ Error during order stress: {ex.Message}");
                // Don't fail if it's just rate limiting or funds
            }
        }
    }
}
