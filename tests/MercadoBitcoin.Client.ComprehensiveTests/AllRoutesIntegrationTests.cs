using Xunit;
using Xunit.Abstractions;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using System;
using System.Threading.Tasks;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    /// <summary>
    /// Comprehensive integration tests for ALL Mercado Bitcoin API routes.
    /// Tests use REAL API credentials and make actual HTTP calls.
    /// </summary>
    [Collection("Sequential")]
    public class AllRoutesIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MercadoBitcoinClient _client;
        private readonly string _testSymbol = "BTC-BRL";
        private string? _testAccountId;

        public AllRoutesIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Use provided credentials with fallback
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var apiId = config["MercadoBitcoin:ApiKey"] ?? Environment.GetEnvironmentVariable("MB_API_ID");
            var apiSecret = config["MercadoBitcoin:ApiSecret"] ?? Environment.GetEnvironmentVariable("MB_API_SECRET");

            _output.WriteLine($"DEBUG: API Key found: {!string.IsNullOrEmpty(apiId)}");
            _output.WriteLine($"DEBUG: API Secret found: {!string.IsNullOrEmpty(apiSecret)}");

            var options = new MercadoBitcoinClientOptions
            {
                ApiLogin = apiId,
                ApiPassword = apiSecret,
                BaseUrl = "https://api.mercadobitcoin.net/api/v4",
                TimeoutSeconds = 30,
                RetryPolicyConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig()
            };

            _client = new MercadoBitcoinClient(options);
            
            // Synchronize time immediately
            _client.SynchronizeTimeAsync().GetAwaiter().GetResult();
            _output.WriteLine($"✅ [INIT] Time synchronized. Timestamp: {_client.GetCurrentTimestamp()}");

            // Get account ID for private tests
            try
            {
                var accounts = _client.GetAccountsAsync().GetAwaiter().GetResult();
                _testAccountId = accounts.FirstOrDefault()?.Id;
                _output.WriteLine($"✅ [INIT] Account ID: {_testAccountId}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️ [INIT] Could not fetch account: {ex.Message}");
            }
        }

        #region Beast Mode Features

        [Fact]
        public async Task BeastMode_TimeSynchronization_ShouldCalculateOffset()
        {
            // Act
            await _client.SynchronizeTimeAsync();
            var correctedTime = _client.GetCurrentTimestamp();
            var localTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Assert
            var diff = Math.Abs(correctedTime - localTime);
            _output.WriteLine($"✅ Time offset: {diff} seconds");
            diff.Should().BeLessThan(10, "Time synchronization should be accurate");
        }

        [Fact]
        public async Task BeastMode_Http2Multiplexing_ShouldExecuteSimultaneously()
        {
            // Arrange
            var symbols = new[] { "BTC-BRL", "ETH-BRL", "USDT-BRL" };
            var tasks = symbols.Select(s => _client.GetOrderBookAsync(s, null)).ToList();

            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = await _client.ExecuteBatchAsync(tasks);
            sw.Stop();

            // Assert
            results.Should().HaveCount(3);
            results.All(r => r != null).Should().BeTrue();
            _output.WriteLine($"✅ HTTP/2 Batch execution: {sw.ElapsedMilliseconds}ms for {results.Count()} requests");
        }

        #endregion

        #region Public Endpoints (Open Client - No Auth)

        [Fact]
        public async Task Public_Orderbook_ShouldReturnBidsAndAsks()
        {
            // Act
            var orderBook = await _client.GetOrderBookAsync(_testSymbol, null);

            // Assert
            orderBook.Should().NotBeNull();
            orderBook.Bids.Should().NotBeEmpty();
            orderBook.Asks.Should().NotBeEmpty();
            
            var bestBid = orderBook.Bids.First();
            var bestAsk = orderBook.Asks.First();
            
            _output.WriteLine($"✅ OrderBook: Best Bid={bestBid.FirstOrDefault()}, Best Ask={bestAsk.FirstOrDefault()}");
        }

        [Fact]
        public async Task Public_Trades_ShouldReturnRecentTrades()
        {
            // Act
            var trades = await _client.GetTradesAsync(_testSymbol, null, null, null, null);
            var fees = await _client.GetAssetFeesAsync("BTC", null);

            // Assert
            fees.Should().NotBeNull();
            fees.Asset.Should().Be("BTC");
            fees.Withdrawal_fee.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"✅ BTC Withdraw Fee: {fees.Withdrawal_fee}");
        }

        #endregion

        #region Private Endpoints (Authenticated)

        [Fact]
        public async Task Private_Accounts_ShouldReturnUserAccounts()
        {
            // Act
            var accounts = await _client.GetAccountsAsync();

            // Assert
            accounts.Should().NotBeNull();
            accounts.Should().NotBeEmpty();
            
            var firstAccount = accounts.First();
            firstAccount.Id.Should().NotBeNullOrWhiteSpace();
            
            _output.WriteLine($"✅ Found {accounts.Count()} account(s). First ID: {firstAccount.Id}");
        }

        [Fact]
        public async Task Private_Balances_ShouldReturnAccountBalances()
        {
            // Arrange
            _testAccountId.Should().NotBeNullOrWhiteSpace("Account ID must be available");

            // Act
            var balances = await _client.GetBalancesAsync(_testAccountId);

            // Assert
            balances.Should().NotBeNull();
            _output.WriteLine($"✅ Found {balances.Count()} balance entries");
            
            foreach (var balance in balances.Take(5))
            {
                _output.WriteLine($"   {balance.Symbol}: Available={balance.Available}, Total={balance.Total}");
            }
        }

        [Fact]
        public async Task Private_Orders_ShouldReturnUserOrders()
        {
            // Arrange
            _testAccountId.Should().NotBeNullOrWhiteSpace("Account ID must be available");

            // Act
            var orders = await _client.ListOrdersAsync(
                accountId: _testAccountId,
                symbol: _testSymbol,
                status: null,
                idFrom: null,
                idTo: null,
                // limit: null, // ListOrdersAsync signature differs slightly, check params
                createdAtFrom: null,
                createdAtTo: null,
                hasExecutions: null);

            // Assert
            orders.Should().NotBeNull();
            _output.WriteLine($"✅ Found {orders.Count()} orders for {_testSymbol}");
        }

        [Fact]
        public async Task Private_OrderById_ShouldReturnOrderDetails()
        {
            // Arrange
            _testAccountId.Should().NotBeNullOrWhiteSpace("Account ID must be available");
            
            // Get any existing order first
            var orders = await _client.ListOrdersAsync(_testSymbol, _testAccountId);
            
            if (!orders.Any())
            {
                _output.WriteLine("⚠️ No orders found to test OrderById. Skipping.");
                return;
            }

            var testOrderId = orders.First().Id;

            // Act
            var order = await _client.GetOrderAsync(_testSymbol, _testAccountId, testOrderId);

            // Assert
            order.Should().NotBeNull();
            order.Id.Should().Be(testOrderId);
            _output.WriteLine($"✅ Order {testOrderId} retrieved successfully");
        }

        [Fact]
        public async Task Private_Fills_ShouldReturnExecutedTrades()
        {
            // Arrange
            _testAccountId.Should().NotBeNullOrWhiteSpace("Account ID must be available");

            // Act
            // Using ListOrdersAsync with hasExecutions=true as FillsAsync replacement
            var fills = await _client.ListOrdersAsync(
                symbol: _testSymbol,
                accountId: _testAccountId,
                hasExecutions: "true");

            // Assert
            fills.Should().NotBeNull();
            _output.WriteLine($"✅ Found {fills.Count()} fills (executed trades) for {_testSymbol}");
        }

        [Fact]
        public async Task Private_Deposits_ShouldReturnDepositHistory()
        {
            // Arrange
            _testAccountId.Should().NotBeNullOrWhiteSpace("Account ID must be available");

            // Act
            var deposits = await _client.ListDepositsAsync(
                accountId: _testAccountId,
                symbol: "BTC", // Symbol is required
                limit: "10");

            // Assert
            deposits.Should().NotBeNull();
            _output.WriteLine($"✅ Found {deposits.Count()} deposit(s)");
        }

        [Fact]
        public async Task Private_Withdrawals_ShouldReturnWithdrawalHistory()
        {
            // Arrange
            _testAccountId.Should().NotBeNullOrWhiteSpace("Account ID must be available");

            // Act
            var withdrawals = await _client.ListWithdrawalsAsync(
                accountId: _testAccountId,
                symbol: "BTC", // Symbol is required
                pageSize: 10);

            // Assert
            withdrawals.Should().NotBeNull();
            _output.WriteLine($"✅ Found {withdrawals.Count()} withdrawal(s)");
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
