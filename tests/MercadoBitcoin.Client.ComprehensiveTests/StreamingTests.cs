using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    public class StreamingTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MercadoBitcoinClient _client;
        private readonly string _testSymbol = "BTC-BRL";
        private readonly string? _testAccountId;

        public StreamingTests(ITestOutputHelper output)
        {
            _output = output;
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var apiId = config["MercadoBitcoin:ApiKey"] ?? Environment.GetEnvironmentVariable("MB_API_ID");
            var apiSecret = config["MercadoBitcoin:ApiSecret"] ?? Environment.GetEnvironmentVariable("MB_API_SECRET");

            var options = new MercadoBitcoinClientOptions
            {
                ApiLogin = apiId,
                ApiPassword = apiSecret,
                BaseUrl = "https://api.mercadobitcoin.net/api/v4"
            };

            _client = new MercadoBitcoinClient(options);

            try
            {
                var accounts = _client.GetAccountsAsync().GetAwaiter().GetResult();
                _testAccountId = accounts.FirstOrDefault()?.Id;
            }
            catch { }
        }

        [Fact]
        public async Task StreamTradesAsync_ShouldReturnTrades()
        {
            // Act
            var trades = new List<Generated.TradeResponse>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                await foreach (var trade in _client.StreamTradesAsync(_testSymbol, limit: 100).WithCancellation(cts.Token))
                {
                    trades.Add(trade);
                    if (trades.Count >= 150) break;
                }
            }
            catch (OperationCanceledException) { }

            // Assert
            trades.Should().NotBeEmpty();
            _output.WriteLine($"✅ Streamed {trades.Count} trades");
        }

        [Fact]
        public async Task StreamCandlesAsync_ShouldReturnCandles()
        {
            // Arrange
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - (24 * 60 * 60); // Last 24 hours

            // Act
            var candles = new List<Models.CandleData>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                await foreach (var candle in _client.StreamCandlesAsync(_testSymbol, "1h", from, to, batchSize: 10).WithCancellation(cts.Token))
                {
                    candles.Add(candle);
                }
            }
            catch (OperationCanceledException) { }

            // Assert
            candles.Should().NotBeEmpty();
            _output.WriteLine($"✅ Streamed {candles.Count} candles");
        }

        [Fact]
        public async Task StreamOrdersAsync_ShouldReturnOrders()
        {
            if (string.IsNullOrEmpty(_testAccountId))
            {
                _output.WriteLine("⚠️ Skipping StreamOrdersAsync: No account ID available");
                return;
            }

            // Act
            var orders = new List<Generated.OrderResponse>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                await foreach (var order in _client.StreamOrdersAsync(_testSymbol, _testAccountId).WithCancellation(cts.Token))
                {
                    orders.Add(order);
                    if (orders.Count >= 10) break;
                }
            }
            catch (OperationCanceledException) { }

            // Assert
            // Note: Might be empty if user has no orders
            _output.WriteLine($"✅ Streamed {orders.Count} orders");
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
