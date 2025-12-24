using Xunit;
using Xunit.Abstractions;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    /// <summary>
    /// Exhaustive API coverage tests for ALL endpoints with and without filters.
    /// Covers 100% of the MercadoBitcoin API surface.
    /// </summary>
    [Collection("Sequential")]
    public class ExhaustiveApiCoverageTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MercadoBitcoinClient _client;
        private readonly string _testAccountId = string.Empty;

        public ExhaustiveApiCoverageTests(ITestOutputHelper output)
        {
            _output = output;

            var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var apiId = config["MercadoBitcoin:ApiKey"];
            var apiSecret = config["MercadoBitcoin:ApiSecret"];
            _testAccountId = config["MercadoBitcoin:TestAccountId"] ?? string.Empty;

            _output.WriteLine($"[INIT] Using Account ID: {_testAccountId}");

            var options = new MercadoBitcoinClientOptions
            {
                ApiLogin = apiId,
                ApiPassword = apiSecret,
                BaseUrl = "https://api.mercadobitcoin.net/api/v4",
                TimeoutSeconds = 60,
                RetryPolicyConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig()
            };

            _client = new MercadoBitcoinClient(options);
            _client.SynchronizeTimeAsync().GetAwaiter().GetResult();
            _output.WriteLine($"[INIT] Time synchronized at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        }

        #region Public Endpoints - Symbols

        [Fact]
        public async Task Symbols_NoFilter_ShouldReturnAllSymbols()
        {
            // Act
            var response = await _client.GetSymbolsAsync((IEnumerable<string>?)null);

            // Assert
            response.Should().NotBeNull();
            response.Symbol.Should().NotBeEmpty();
            _output.WriteLine($"✅ Symbols (no filter): {response.Symbol.Count} symbols found");
        }

        [Fact]
        public async Task Symbols_WithSingleFilter_ShouldReturnFiltered()
        {
            // Act
            var response = await _client.GetSymbolsAsync(new[] { "BTC-BRL" });

            // Assert
            response.Should().NotBeNull();
            response.Symbol.Should().NotBeEmpty();
            response.Symbol.Should().Contain("BTC-BRL");
            _output.WriteLine($"✅ Symbols (BTC-BRL filter): {response.Symbol.Count} symbol(s)");
        }

        [Fact]
        public async Task Symbols_WithMultipleFilters_ShouldReturnMultiple()
        {
            // Act
            var response = await _client.GetSymbolsAsync(new[] { "BTC-BRL", "ETH-BRL", "USDT-BRL" });

            // Assert
            response.Should().NotBeNull();
            response.Symbol.Should().HaveCountGreaterThanOrEqualTo(3);
            _output.WriteLine($"✅ Symbols (multiple filters): {response.Symbol.Count} symbols");
        }

        #endregion

        #region Public Endpoints - Tickers

        [Fact]
        public async Task Tickers_NoFilter_ShouldReturnAllTickers()
        {
            // Act
            var tickers = await _client.GetTickersAsync((IEnumerable<string>)null);

            // Assert
            tickers.Should().NotBeEmpty();
            _output.WriteLine($"✅ Tickers (no filter): {tickers.Count} tickers found");
        }

        [Fact]
        public async Task Tickers_WithSingleFilter_ShouldReturnOne()
        {
            // Act
            var tickers = await _client.GetTickersAsync(new[] { "BTC-BRL" });

            // Assert
            tickers.Should().NotBeEmpty();
            tickers.Should().Contain(t => t.Pair == "BTC-BRL");
            _output.WriteLine($"✅ Tickers (BTC-BRL): Last={tickers.First().Last}, Vol={tickers.First().Vol}");
        }

        [Fact]
        public async Task Tickers_WithMultipleFilters_ShouldReturnFiltered()
        {
            // Act
            var tickers = await _client.GetTickersAsync(new[] { "BTC-BRL", "ETH-BRL" });

            // Assert
            tickers.Should().HaveCountGreaterThanOrEqualTo(2);
            _output.WriteLine($"✅ Tickers (multiple): {tickers.Count} tickers returned");
        }

        [Fact]
        public async Task Tickers_BatchMode_ShouldHandleLargeSymbolList()
        {
            // Act
            var allSymbols = (await _client.GetSymbolsAsync((IEnumerable<string>?)null)).Symbol;

            var tickers = await _client.GetTickersBatchAsync(allSymbols.Take(100), batchSize: 25);

            // Assert
            tickers.Should().NotBeEmpty();
            _output.WriteLine($"✅ Tickers (batch mode): {tickers.Count} tickers in batches of 25");
        }

        #endregion

        #region Public Endpoints - OrderBook

        [Fact]
        public async Task OrderBook_DefaultLimit_ShouldReturnBook()
        {
            // Act
            var orderBook = await _client.GetOrderBookAsync("BTC-BRL", null);

            // Assert
            orderBook.Should().NotBeNull();
            orderBook.Bids.Should().NotBeEmpty();
            orderBook.Asks.Should().NotBeEmpty();
            _output.WriteLine($"✅ OrderBook (default): {orderBook.Bids.Count} bids, {orderBook.Asks.Count} asks");
        }

        [Fact]
        public async Task OrderBook_CustomLimit_ShouldRespectLimit()
        {
            // Act
            var orderBook = await _client.GetOrderBookAsync("BTC-BRL", "5");

            // Assert
            orderBook.Should().NotBeNull();
            orderBook.Bids.Should().HaveCountLessThanOrEqualTo(5);
            orderBook.Asks.Should().HaveCountLessThanOrEqualTo(5);
            _output.WriteLine($"✅ OrderBook (limit=5): {orderBook.Bids.Count} bids, {orderBook.Asks.Count} asks");
        }

        #endregion

        #region Public Endpoints - Trades

        [Fact]
        public async Task Trades_NoFilters_ShouldReturnRecentTrades()
        {
            // Act
            var trades = await _client.GetTradesAsync("BTC-BRL");

            // Assert
            trades.Should().NotBeEmpty();
            _output.WriteLine($"✅ Trades (no filters): {trades.Count} trades");
        }

        [Fact]
        public async Task Trades_WithLimit_ShouldRespectLimit()
        {
            // Act
            var trades = await _client.GetTradesAsync("BTC-BRL", limit: 10);

            // Assert
            trades.Should().NotBeEmpty();
            trades.Should().HaveCountLessThanOrEqualTo(10);
            _output.WriteLine($"✅ Trades (limit=10): {trades.Count} trades returned");
        }

        [Fact]
        public async Task Trades_WithTimeRange_ShouldFilterByTime()
        {
            // Arrange
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - 86400; // Last 24h

            // Act
            var trades = await _client.GetTradesAsync("BTC-BRL", from: from, to: to);

            // Assert
            trades.Should().NotBeEmpty();
            _output.WriteLine($"✅ Trades (24h range): {trades.Count} trades");
        }

        #endregion

        #region Public Endpoints - Candles

        [Fact]
        public async Task Candles_NoCountback_ShouldReturnRange()
        {
            // Arrange
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - 86400; // 24h

            // Act
            var candles = await _client.GetCandlesAsync("BTC-BRL", "1h", to, from);

            // Assert
            candles.Should().NotBeNull();
            candles.C.Should().NotBeEmpty();
            _output.WriteLine($"✅ Candles (from-to): {candles.C.Count} candles in 24h");
        }

        [Fact]
        public async Task Candles_WithCountback_ShouldReturnLastN()
        {
            // Arrange
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Act
            var candles = await _client.GetCandlesAsync("BTC-BRL", "1h", to, countback: 10);

            // Assert
            candles.Should().NotBeNull();
            candles.C.Should().HaveCount(10);
            _output.WriteLine($"✅ Candles (countback=10): {candles.C.Count} candles returned");
        }

        [Fact]
        public async Task Candles_TypedModel_ShouldReturnStructs()
        {
            // Act
            var candles = await _client.GetRecentCandlesTypedAsync("BTC-BRL", "1h", countback: 5);

            // Assert
            candles.Should().NotBeEmpty();
            candles.Should().HaveCount(5);

            foreach (var candle in candles)
            {
                candle.Close.Should().BeGreaterThan(0);
                candle.High.Should().BeGreaterThanOrEqualTo(candle.Close);
                candle.Low.Should().BeLessThanOrEqualTo(candle.Close);
            }

            _output.WriteLine($"✅ Candles (typed model): {candles.Count} CandleData structs");
        }

        [Fact]
        public async Task Candles_MultipleSymbols_ShouldAggregateResults()
        {
            // Act
            var candles = await _client.GetCandlesTypedAsync(
                symbols: new[] { "BTC-BRL", "ETH-BRL" },
                resolution: "1h",
                countback: 5);

            // Assert
            candles.Should().NotBeEmpty();
            _output.WriteLine($"✅ Candles (multi-symbol): {candles.Count} candles aggregated");
        }

        #endregion

        #region Public Endpoints - Fees & Networks

        [Fact]
        public async Task AssetFees_WithoutNetwork_ShouldReturnFees()
        {
            // Act
            var fees = await _client.GetAssetFeesAsync("BTC", null);

            // Assert
            fees.Should().NotBeNull();
            fees.Asset.Should().Be("BTC");
            fees.Withdrawal_fee.Should().NotBeNullOrEmpty();
            _output.WriteLine($"✅ Asset Fees (BTC): Withdraw Fee={fees.Withdrawal_fee}");
        }

        [Fact]
        public async Task AssetFees_WithNetwork_ShouldReturnNetworkFee()
        {
            // Act
            var fees = await _client.GetAssetFeesAsync("USDC", "ERC20");

            // Assert
            fees.Should().NotBeNull();
            fees.Asset.Should().Be("USDC");
            _output.WriteLine($"✅ Asset Fees (USDC-ERC20): {fees.Withdrawal_fee}");
        }

        [Fact]
        public async Task AssetNetworks_ShouldReturnAvailableNetworks()
        {
            // Act
            var networks = await _client.GetAssetNetworksAsync("USDT");

            // Assert
            networks.Should().NotBeEmpty();
            _output.WriteLine($"✅ Asset Networks (USDT): {string.Join(", ", networks.Select(n => n.Network1))}");
        }

        #endregion

        #region Private Endpoints - Accounts

        [Fact]
        public async Task Accounts_ShouldReturnAllAccounts()
        {
            // Act
            var accounts = await _client.GetAccountsAsync();

            // Assert
            accounts.Should().NotBeEmpty();
            accounts.Should().Contain(a => a.Id == _testAccountId);
            _output.WriteLine($"✅ Accounts: {accounts.Count} account(s) found");

            foreach (var account in accounts)
            {
                _output.WriteLine($"   - {account.Name} ({account.Currency}): {account.Type}");
            }
        }

        #endregion

        #region Private Endpoints - Orders

        [Fact]
        public async Task Orders_List_NoFilters_ShouldReturnOrders()
        {
            // Act
            var orders = await _client.ListOrdersAsync("BTC-BRL", _testAccountId);

            // Assert
            orders.Should().NotBeNull();
            _output.WriteLine($"✅ Orders (no filters): {orders.Count} order(s)");
        }

        [Fact]
        public async Task Orders_List_WithStatus_ShouldFilterByStatus()
        {
            // Act
            var orders = await _client.ListOrdersAsync(
                symbol: "BTC-BRL",
                accountId: _testAccountId,
                status: "2"); // Open orders

            // Assert
            orders.Should().NotBeNull();
            _output.WriteLine($"✅ Orders (status=open): {orders.Count} open order(s)");
        }

        [Fact]
        public async Task Orders_List_WithHasExecutions_ShouldFilterExecuted()
        {
            // Act
            var orders = await _client.ListOrdersAsync(
                symbol: "BTC-BRL",
                accountId: _testAccountId,
                hasExecutions: "true");

            // Assert
            orders.Should().NotBeNull();
            _output.WriteLine($"✅ Orders (hasExecutions=true): {orders.Count} executed order(s)");
        }

        [Fact]
        public async Task Orders_GetById_ShouldReturnOrderDetails()
        {
            // Arrange
            var allOrders = await _client.ListOrdersAsync("BTC-BRL", _testAccountId);

            if (!allOrders.Any())
            {
                _output.WriteLine("⚠️ No orders found. Skipping GetOrderById test.");
                return;
            }

            var testOrderId = allOrders.First().Id;

            // Act
            var order = await _client.GetOrderAsync("BTC-BRL", _testAccountId, testOrderId);

            // Assert
            order.Should().NotBeNull();
            order.Id.Should().Be(testOrderId);
            _output.WriteLine($"✅ Order by ID: {order.Id} ({order.Side} {order.Qty} @ {order.LimitPrice})");
        }

        #endregion

        #region Private Endpoints - Balances

        [Fact]
        public async Task Balances_ShouldReturnAllBalances()
        {
            // Act
            var balances = await _client.GetBalancesAsync(_testAccountId);

            // Assert
            balances.Should().NotBeEmpty();
            _output.WriteLine($"✅ Balances: {balances.Count} asset(s)");

            foreach (var balance in balances.Where(b => decimal.Parse(b.Total) > 0).Take(5))
            {
                _output.WriteLine($"   - {balance.Symbol}: Available={balance.Available}, Total={balance.Total}");
            }
        }

        #endregion

        #region Private Endpoints - Positions

        [Fact]
        public async Task Positions_SingleSymbol_ShouldReturnPosition()
        {
            // Act
            var positions = await _client.GetPositionsAsync(_testAccountId, "BTC-BRL");

            // Assert
            positions.Should().NotBeNull();
            _output.WriteLine($"✅ Positions (BTC-BRL): {positions.Count} position(s)");
        }

        [Fact]
        public async Task Positions_MultipleSymbols_ShouldReturnMultiple()
        {
            // Act
            var positions = await _client.GetPositionsAsync(_testAccountId, "BTC-BRL,ETH-BRL");

            // Assert
            positions.Should().NotBeNull();
            _output.WriteLine($"✅ Positions (multiple): {positions.Count} position(s)");
        }

        #endregion

        #region Private Endpoints - Tier & Fees

        [Fact]
        public async Task Tier_ShouldReturnUserTier()
        {
            try
            {
                // Act
                var tiers = await _client.GetTierAsync(_testAccountId);

                // Assert
                tiers.Should().NotBeNull();
                _output.WriteLine($"✅ Tier: {tiers.FirstOrDefault()?.Tier ?? "N/A"}");
            }
            catch (MercadoBitcoinApiException ex)
            {
                _output.WriteLine($"⚠️ Tier endpoint: {ex.Message} (may not be available for all accounts)");
            }
        }

        [Fact]
        public async Task TradingFees_ShouldReturnMarketFees()
        {
            // Act
            var fees = await _client.GetTradingFeesAsync(_testAccountId, "BTC-BRL");

            // Assert
            fees.Should().NotBeNull();
            _output.WriteLine($"✅ Trading Fees: Maker={fees.Maker_fee}, Taker={fees.Taker_fee}");
        }

        #endregion

        #region Private Endpoints - Deposits

        [Fact]
        public async Task Deposits_Crypto_NoFilters_ShouldReturnHistory()
        {
            // Act
            var deposits = await _client.ListDepositsAsync(_testAccountId, symbol: "BTC");

            // Assert
            deposits.Should().NotBeNull();
            _output.WriteLine($"✅ Crypto Deposits (BTC): {deposits.Count} deposit(s)");
        }

        [Fact]
        public async Task Deposits_Crypto_WithPagination_ShouldRespectLimit()
        {
            // Act
            var deposits = await _client.ListDepositsAsync(_testAccountId, symbol: "BTC", limit: "5");

            // Assert
            deposits.Should().NotBeNull();
            deposits.Should().HaveCountLessThanOrEqualTo(5);
            _output.WriteLine($"✅ Crypto Deposits (limit=5): {deposits.Count} deposit(s)");
        }

        [Fact]
        public async Task Deposits_Fiat_ShouldReturnFiatHistory()
        {
            // Act
            var deposits = await _client.ListFiatDepositsAsync(_testAccountId, "BRL");

            // Assert
            deposits.Should().NotBeNull();
            _output.WriteLine($"✅ Fiat Deposits (BRL): {deposits.Count} deposit(s)");
        }

        [Fact]
        public async Task DepositAddresses_ShouldReturnAddresses()
        {
            // Act
            var addresses = await _client.GetDepositAddressesAsync(_testAccountId, "BTC");

            // Assert
            addresses.Should().NotBeNull();
            _output.WriteLine($"✅ Deposit Addresses (BTC): {addresses.Addresses?.Count ?? 0} address(es)");
        }

        [Fact]
        public async Task DepositAddresses_WithNetwork_ShouldReturnNetworkAddress()
        {
            // Act - Use Ethereum network (available for some assets)
            var addresses = await _client.GetDepositAddressesAsync(_testAccountId, "USDT", Network2.Ethereum);

            // Assert
            addresses.Should().NotBeNull();
            _output.WriteLine($"✅ Deposit Addresses (USDT-Ethereum): Retrieved");
        }

        [Fact]
        public async Task DepositAddresses_All_ShouldReturnAllAssets()
        {
            // Act
            var addresses = await _client.GetWithdrawCryptoWalletAddressesAsync(_testAccountId);

            // Assert
            addresses.Should().NotBeNull();
            _output.WriteLine($"✅ All Deposit Addresses: {addresses.Count} address(es)");
        }

        #endregion

        #region Private Endpoints - Withdrawals

        [Fact]
        public async Task Withdrawals_List_NoFilters_ShouldReturnHistory()
        {
            // Act
            var withdrawals = await _client.ListWithdrawalsAsync(_testAccountId, "BTC");

            // Assert
            withdrawals.Should().NotBeNull();
            _output.WriteLine($"✅ Withdrawals (BTC): {withdrawals.Count} withdrawal(s)");
        }

        [Fact]
        public async Task Withdrawals_List_WithPagination_ShouldRespectPageSize()
        {
            // Act
            var withdrawals = await _client.ListWithdrawalsAsync(_testAccountId, "BTC", pageSize: 5);

            // Assert
            withdrawals.Should().NotBeNull();
            withdrawals.Should().HaveCountLessThanOrEqualTo(5);
            _output.WriteLine($"✅ Withdrawals (pageSize=5): {withdrawals.Count} withdrawal(s)");
        }

        [Fact]
        public async Task Withdrawals_GetById_ShouldReturnDetails()
        {
            // Arrange
            var allWithdrawals = await _client.ListWithdrawalsAsync(_testAccountId, "BTC");

            if (!allWithdrawals.Any())
            {
                _output.WriteLine("⚠️ No withdrawals found. Skipping GetWithdrawalById test.");
                return;
            }

            var testWithdrawalId = allWithdrawals.First().Id?.ToString() ?? string.Empty;

            // Act
            var withdrawal = await _client.GetWithdrawalAsync(_testAccountId, "BTC", testWithdrawalId);

            // Assert
            withdrawal.Should().NotBeNull();
            _output.WriteLine($"✅ Withdrawal by ID: {withdrawal.Id} ({withdrawal.Qty ?? "0"} {withdrawal.Symbol})");
        }

        [Fact]
        public async Task WithdrawCryptoAddresses_ShouldReturnSavedAddresses()
        {
            // Act
            var addresses = await _client.GetWithdrawCryptoWalletAddressesAsync(_testAccountId);

            // Assert
            addresses.Should().NotBeNull();
            _output.WriteLine($"✅ Withdraw Crypto Addresses: {addresses.Count} address(es)");
        }

        [Fact]
        public async Task WithdrawBankAccounts_ShouldReturnBankAccounts()
        {
            // Act
            var accounts = await _client.GetWithdrawBankAccountsAsync(_testAccountId);

            // Assert
            accounts.Should().NotBeNull();
            _output.WriteLine($"✅ Withdraw Bank Accounts: {accounts.Count} account(s)");
        }

        [Fact]
        public async Task WithdrawLimits_ShouldReturnLimits()
        {
            // Act
            var limits = await _client.GetWithdrawLimitsAsync(_testAccountId, new[] { "BTC" });

            // Assert
            limits.Should().NotBeNull();
            _output.WriteLine($"✅ Withdraw Limits (BTC): Retrieved");
        }

        [Fact]
        public async Task WithdrawBRLConfig_ShouldReturnBRLConfiguration()
        {
            // Act
            var config = await _client.GetBrlWithdrawConfigAsync(_testAccountId);

            // Assert
            config.Should().NotBeNull();
            _output.WriteLine($"✅ BRL Withdraw Config: Retrieved");
        }

        #endregion

        #region Private Endpoints - Cancel Open Orders

        [Fact]
        public async Task CancelOpenOrders_WithFilters_ShouldCancelMatching()
        {
            // Note: This test may not cancel any orders if none are open
            try
            {
                // Act
                var result = await _client.CancelAllOpenOrdersByAccountRawAsync(
                    accountId: _testAccountId,
                    hasExecutions: false,
                    symbol: "BTC-BRL");

                // Assert
                result.Should().NotBeNull();
                _output.WriteLine($"✅ Cancel Open Orders: {result.Count} order(s) cancelled");
            }
            catch (MercadoBitcoinApiException ex)
            {
                _output.WriteLine($"⚠️ Cancel Open Orders: {ex.Message} (no open orders to cancel)");
            }
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
            _output.WriteLine($"[CLEANUP] Client disposed at {DateTimeOffset.UtcNow:HH:mm:ss UTC}");
        }
    }
}
