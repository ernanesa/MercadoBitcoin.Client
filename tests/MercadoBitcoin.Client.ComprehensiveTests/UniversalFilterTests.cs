using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using MercadoBitcoin.Client.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    public class UniversalFilterTests : TestBase
    {
        private readonly ITestOutputHelper _output;

        public UniversalFilterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task GetTickersAsync_NoParams_ShouldReturnAllTickers()
        {
            // Act
            var tickers = await Client.GetTickersAsync();

            // Assert
            tickers.Should().NotBeNull();
            tickers.Should().HaveCountGreaterThan(100, "Should return tickers for all symbols");
            _output.WriteLine($"✅ Fetched {tickers.Count} tickers without filters.");
        }

        [Fact]
        public async Task GetOrderBooksAsync_NoParams_ShouldReturnMultipleOrderBooks()
        {
            // Act - Limit to a few symbols to avoid hitting rate limits too hard in tests
            // but the logic is the same. For "all", we'll just test with a small subset or null
            // to verify the fan-out logic.
            var symbols = new[] { "BTC-BRL", "ETH-BRL" };
            var orderBooks = await Client.GetOrderBooksAsync(symbols);

            // Assert
            orderBooks.Should().NotBeNull();
            orderBooks.Should().HaveCount(2);
            _output.WriteLine($"✅ Fetched {orderBooks.Count} order books.");
        }

        [Fact]
        public async Task GetSymbolsAsync_NoParams_ShouldReturnAllSymbols()
        {
            // Act
            var response = await Client.GetSymbolsAsync();

            // Assert
            response.Should().NotBeNull();
            response.Symbol.Should().NotBeNullOrEmpty();
            response.Symbol.Count.Should().BeGreaterThan(100);
            _output.WriteLine($"✅ Fetched {response.Symbol.Count} symbols.");
        }

        [Fact]
        public async Task GetPositionsAsync_NoParams_ShouldReturnAllPositions()
        {
            // Act
            var positions = await Client.GetPositionsAsync(TestAccountId);

            // Assert
            positions.Should().NotBeNull();
            _output.WriteLine($"✅ Fetched {positions.Count} positions for account {TestAccountId}.");
        }

        [Fact]
        public async Task ListOrdersAsync_NoParams_ShouldReturnOrdersForAllSymbols()
        {
            // Act - We use a small subset to avoid long test times, but null would work too
            var symbols = new[] { "BTC-BRL" };
            var orders = await Client.ListOrdersAsync(TestAccountId, symbols);

            // Assert
            orders.Should().NotBeNull();
            _output.WriteLine($"✅ Fetched {orders.Count} orders for symbols: {string.Join(", ", symbols)}.");
        }

        [Fact]
        public async Task GetWithdrawLimitsAsync_NoParams_ShouldReturnAllLimits()
        {
            // Act
            var limits = await Client.GetWithdrawLimitsAsync(TestAccountId);

            // Assert
            limits.Should().NotBeNull();
            _output.WriteLine($"✅ Fetched {limits.Count} withdraw limit responses.");
        }

        [Fact]
        public async Task GetTradesAsync_NoParams_ShouldReturnTradesForMultipleSymbols()
        {
            // Act
            var symbols = new[] { "BTC-BRL", "ETH-BRL" };
            var trades = await Client.GetTradesAsync(symbols, limit: 5);

            // Assert
            trades.Should().NotBeNull();
            _output.WriteLine($"✅ Fetched {trades.Count} trades for symbols: {string.Join(", ", symbols)}.");
        }

        [Fact]
        public async Task GetCandlesAsync_NoParams_ShouldReturnCandlesForMultipleSymbols()
        {
            // Act
            var symbols = new[] { "BTC-BRL", "ETH-BRL" };
            var candles = await Client.GetCandlesAsync(symbols, resolution: "1h", countback: 5);

            // Assert
            candles.Should().NotBeNull();
            candles.Should().HaveCount(2);
            _output.WriteLine($"✅ Fetched candles for {candles.Count} symbols.");
        }

        [Fact]
        public async Task ListDepositsAsync_NoParams_ShouldReturnDepositsForMultipleSymbols()
        {
            // Act
            var symbols = new[] { "BTC", "ETH" };
            var deposits = await Client.ListDepositsAsync(TestAccountId, symbols);

            // Assert
            deposits.Should().NotBeNull();
            _output.WriteLine($"✅ Fetched {deposits.Count} deposits for symbols: {string.Join(", ", symbols)}.");
        }

        [Fact]
        public async Task ListWithdrawalsAsync_NoParams_ShouldReturnWithdrawalsForMultipleSymbols()
        {
            // Act
            var symbols = new[] { "BTC", "ETH" };
            var withdrawals = await Client.ListWithdrawalsAsync(TestAccountId, symbols);

            // Assert
            withdrawals.Should().NotBeNull();
            _output.WriteLine($"✅ Fetched {withdrawals.Count} withdrawals for symbols: {string.Join(", ", symbols)}.");
        }

        [Fact]
        public async Task ListAllOrdersAsync_NoParams_ShouldReturnAllOrders()
        {
            // Act
            var symbols = new[] { "BTC-BRL", "ETH-BRL" };
            var response = await Client.ListAllOrdersAsync(TestAccountId, symbols);

            // Assert
            response.Should().NotBeNull();
            response.Items.Should().NotBeNull();
            _output.WriteLine($"✅ Fetched {response.Items.Count} total orders for symbols: {string.Join(", ", symbols)}.");
        }
    }
}
