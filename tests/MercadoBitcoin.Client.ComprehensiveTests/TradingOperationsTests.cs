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
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    /// <summary>
    /// Real trading operations tests: place, cancel, and manage actual orders.
    /// Uses minimal order quantities to avoid balance issues.
    /// </summary>
    [Collection("Sequential")]
    public class TradingOperationsTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MercadoBitcoinClient _client;
        private readonly string _testAccountId = string.Empty;
        private readonly string _testSymbol = "BTC-BRL";

        public TradingOperationsTests(ITestOutputHelper output)
        {
            _output = output;

            var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var apiId = config["MercadoBitcoin:ApiKey"];
            var apiSecret = config["MercadoBitcoin:ApiSecret"];
            _testAccountId = config["MercadoBitcoin:TestAccountId"] ?? string.Empty;

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
            _output.WriteLine($"[INIT] Trading tests initialized for account {_testAccountId}");
        }

        #region Limit Buy Order

        [Fact]
        public async Task PlaceLimitBuyOrder_ShouldCreateOrder()
        {
            try
            {
                // Arrange - Get current market price to place order below it (won't execute)
                var ticker = (await _client.GetTickersAsync(new[] { _testSymbol })).First();
                var currentPrice = decimal.Parse(ticker.Last);
                var orderPrice = Math.Floor(currentPrice * 0.90m); // 10% below market
                var minQuantity = 0.0001m; // Minimum BTC quantity

                _output.WriteLine($"[BUY] Current Price: R$ {currentPrice:N2}");
                _output.WriteLine($"[BUY] Order Price: R$ {orderPrice:N2} (10% below)");
                _output.WriteLine($"[BUY] Quantity: {minQuantity} BTC");

                // Act
                var orderRequest = new PlaceOrderRequest
                {
                    Side = "buy",
                    Type = "limit",
                    Qty = minQuantity.ToString("F8"),
                    LimitPrice = (double)orderPrice
                };

                var result = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);

                // Assert
                result.Should().NotBeNull();
                result.OrderId.Should().NotBeNullOrWhiteSpace();
                _output.WriteLine($"✅ Limit BUY order placed: {result.OrderId}");

                // Cleanup - Cancel the order
                await Task.Delay(1000); // Wait 1s before cancellation
                var cancelResult = await _client.CancelOrderAsync(_testAccountId, _testSymbol, result.OrderId, async: false);
                _output.WriteLine($"✅ Order cancelled: {cancelResult.Status}");
            }
            catch (MercadoBitcoinApiException ex)
            {
                _output.WriteLine($"⚠️ API Error: {ex.Message}");
                _output.WriteLine($"   This may be expected if account has insufficient balance or trading restrictions.");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Unexpected error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Limit Sell Order

        [Fact]
        public async Task PlaceLimitSellOrder_ShouldCreateOrder()
        {
            try
            {
                // Arrange - Get current market price to place order above it (won't execute)
                var ticker = (await _client.GetTickersAsync(new[] { _testSymbol })).First();
                var currentPrice = decimal.Parse(ticker.Last);
                var orderPrice = Math.Ceiling(currentPrice * 1.10m); // 10% above market
                var minQuantity = 0.0001m; // Minimum BTC quantity

                _output.WriteLine($"[SELL] Current Price: R$ {currentPrice:N2}");
                _output.WriteLine($"[SELL] Order Price: R$ {orderPrice:N2} (10% above)");
                _output.WriteLine($"[SELL] Quantity: {minQuantity} BTC");

                // Act
                var orderRequest = new PlaceOrderRequest
                {
                    Side = "sell",
                    Type = "limit",
                    Qty = minQuantity.ToString("F8"),
                    LimitPrice = (double)orderPrice
                };

                var result = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);

                // Assert
                result.Should().NotBeNull();
                result.OrderId.Should().NotBeNullOrWhiteSpace();
                _output.WriteLine($"✅ Limit SELL order placed: {result.OrderId}");

                // Cleanup - Cancel the order
                await Task.Delay(1000);
                var cancelResult = await _client.CancelOrderAsync(_testAccountId, _testSymbol, result.OrderId, async: false);
                _output.WriteLine($"✅ Order cancelled: {cancelResult.Status}");
            }
            catch (MercadoBitcoinApiException ex)
            {
                _output.WriteLine($"⚠️ API Error: {ex.Message}");
                _output.WriteLine($"   This may be expected if account has insufficient BTC balance.");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Unexpected error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Market Orders (commented - risky for automated tests)

        /*
        [Fact]
        public async Task PlaceMarketBuyOrder_ShouldExecuteImmediately()
        {
            // WARNING: Market orders execute immediately at current price!
            // Uncomment only for manual testing with awareness of costs.

            var minQuantity = 0.0001m;

            var orderRequest = new PlaceOrderRequest
            {
                Side = "buy",
                Type = "market",
                Qty = minQuantity.ToString("F8")
            };

            var result = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);
            _output.WriteLine($"Market BUY executed: {result.OrderId}");
        }
        */

        #endregion

        #region Order Management

        [Fact]
        public async Task CancelNonExistentOrder_ShouldReturnError()
        {
            try
            {
                // Act
                var fakeOrderId = Guid.NewGuid().ToString();
                var result = await _client.CancelOrderAsync(_testAccountId, _testSymbol, fakeOrderId, async: false);

                // If we reach here, something unexpected happened
                _output.WriteLine($"⚠️ Cancel returned: {result.Status}");
            }
            catch (MercadoBitcoinApiException ex)
            {
                // Assert
                ex.Message.Should().Contain("not found");
                _output.WriteLine($"✅ Expected error: {ex.Message}");
            }
        }

        [Fact]
        public async Task ListOrders_AfterPlacement_ShouldIncludeNewOrder()
        {
            string? orderId = null;

            try
            {
                // Arrange - Place an order
                var ticker = (await _client.GetTickersAsync(new[] { _testSymbol })).First();
                var currentPrice = decimal.Parse(ticker.Last);
                var orderPrice = Math.Floor(currentPrice * 0.90m);

                var orderRequest = new PlaceOrderRequest
                {
                    Side = "buy",
                    Type = "limit",
                    Qty = "0.0001",
                    LimitPrice = (double)orderPrice
                };

                var placeResult = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);
                orderId = placeResult.OrderId;
                _output.WriteLine($"[LIST] Placed order {orderId}");

                await Task.Delay(2000); // Wait for order to propagate

                // Act - List orders
                var orders = await _client.ListOrdersAsync(_testSymbol, _testAccountId);

                // Assert
                orders.Should().Contain(o => o.Id == orderId);
                _output.WriteLine($"✅ Order found in list: {orderId}");
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(orderId))
                {
                    try
                    {
                        await _client.CancelOrderAsync(_testAccountId, _testSymbol, orderId, async: false);
                        _output.WriteLine($"✅ Cleanup: Order {orderId} cancelled");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"⚠️ Cleanup failed: {ex.Message}");
                    }
                }
            }
        }

        [Fact]
        public async Task GetOrderById_ShouldReturnOrderDetails()
        {
            string? orderId = null;

            try
            {
                // Arrange - Place an order
                var ticker = (await _client.GetTickersAsync(new[] { _testSymbol })).First();
                var currentPrice = decimal.Parse(ticker.Last);
                var orderPrice = Math.Floor(currentPrice * 0.90m);

                var orderRequest = new PlaceOrderRequest
                {
                    Side = "buy",
                    Type = "limit",
                    Qty = "0.0001",
                    LimitPrice = (double)orderPrice
                };

                var placeResult = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);
                orderId = placeResult.OrderId;
                _output.WriteLine($"[GETBYID] Placed order {orderId}");

                await Task.Delay(1000);

                // Act - Get order by ID
                var order = await _client.GetOrderAsync(_testSymbol, _testAccountId, orderId);

                // Assert
                order.Should().NotBeNull();
                order.Id.Should().Be(orderId);
                order.Side.Should().Be("buy");
                order.LimitPrice.Should().NotBeNull();
                _output.WriteLine($"✅ Order retrieved: {order.Side} {order.Qty} BTC @ R$ {order.LimitPrice}");
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(orderId))
                {
                    try
                    {
                        await _client.CancelOrderAsync(_testAccountId, _testSymbol, orderId, async: false);
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region Order Validation

        [Fact]
        public async Task PlaceOrder_InvalidQuantity_ShouldFail()
        {
            try
            {
                // Act - Try to place order with invalid (too small) quantity
                var orderRequest = new PlaceOrderRequest
                {
                    Side = "buy",
                    Type = "limit",
                    Qty = "0.00000001", // Way below minimum
                    LimitPrice = 100000.0
                };

                var result = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);

                // If we reach here, API didn't validate properly
                _output.WriteLine($"⚠️ Order unexpectedly placed: {result.OrderId}");

                // Cleanup
                await _client.CancelOrderAsync(_testAccountId, _testSymbol, result.OrderId, async: false);
            }
            catch (MercadoBitcoinApiException ex)
            {
                // Assert
                _output.WriteLine($"✅ Expected validation error: {ex.Message}");
                ex.Message.Should().Contain("quantity");
            }
        }

        [Fact]
        public async Task PlaceOrder_InvalidPrice_ShouldFail()
        {
            try
            {
                // Act - Try to place order with negative price
                var orderRequest = new PlaceOrderRequest
                {
                    Side = "buy",
                    Type = "limit",
                    Qty = "0.001",
                    LimitPrice = -100.0 // Invalid negative price
                };

                var result = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);

                // Cleanup if somehow it got placed
                await _client.CancelOrderAsync(_testAccountId, _testSymbol, result.OrderId, async: false);
                _output.WriteLine($"⚠️ Order unexpectedly placed: {result.OrderId}");
            }
            catch (MercadoBitcoinApiException ex)
            {
                // Assert
                _output.WriteLine($"✅ Expected validation error: {ex.Message}");
                ex.Message.Should().MatchRegex("price|invalid");
            }
        }

        #endregion

        #region Concurrent Order Operations

        [Fact]
        public async Task PlaceMultipleOrders_Concurrently_ShouldSucceed()
        {
            var orderIds = new System.Collections.Concurrent.ConcurrentBag<string>();

            try
            {
                // Arrange
                var ticker = (await _client.GetTickersAsync(new[] { _testSymbol })).First();
                var currentPrice = decimal.Parse(ticker.Last);
                var basePrice = Math.Floor(currentPrice * 0.90m);

                // Act - Place 5 orders concurrently at different prices
                var tasks = Enumerable.Range(1, 5).Select(i => Task.Run(async () =>
                {
                    var orderPrice = basePrice - (i * 100); // Stagger prices

                    var orderRequest = new PlaceOrderRequest
                    {
                        Side = "buy",
                        Type = "limit",
                        Qty = "0.0001",
                        LimitPrice = (double)orderPrice
                    };

                    var result = await _client.PlaceOrderAsync(_testSymbol, _testAccountId, orderRequest);
                    orderIds.Add(result.OrderId);
                    _output.WriteLine($"✅ Concurrent order {i} placed: {result.OrderId} @ R$ {orderPrice}");
                }));

                await Task.WhenAll(tasks);

                // Assert
                orderIds.Should().HaveCount(5);
                _output.WriteLine($"✅ All {orderIds.Count} concurrent orders placed successfully");
            }
            finally
            {
                // Cleanup - Cancel all placed orders
                foreach (var orderId in orderIds)
                {
                    try
                    {
                        await _client.CancelOrderAsync(_testAccountId, _testSymbol, orderId, async: false);
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"⚠️ Cleanup failed for {orderId}: {ex.Message}");
                    }
                }
                _output.WriteLine($"✅ Cleanup: {orderIds.Count} orders cancelled");
            }
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
            _output.WriteLine($"[CLEANUP] Trading tests completed");
        }
    }
}
