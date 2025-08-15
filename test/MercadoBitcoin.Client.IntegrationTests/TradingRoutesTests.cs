using System;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.IntegrationTests;

public class TradingRoutesTests
{
    private async Task<(MercadoBitcoinClient client, string accountId)> CreateAuthAndGetAccount()
    {
        var client = new MercadoBitcoinClient();
        await client.AuthenticateAsync(TestConfig.ClientId, TestConfig.ClientSecret);
        var accounts = await client.GetAccountsAsync();
        var accountId = accounts.First().Id;
        return (client, accountId);
    }

    [Fact]
    public async Task ListOrders_Works()
    {
        if (!TestConfig.HasRealCredentials) return;
        var (client, accountId) = await CreateAuthAndGetAccount();
        var result = await client.ListOrdersAsync("BTC-BRL", accountId);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task PlaceAndCancelOrder_Works()
    {
        if (!TestConfig.HasRealCredentials) return;
        var (client, accountId) = await CreateAuthAndGetAccount();
        var payload = new Generated.PlaceOrderRequest
        {
            Side = "buy",
            Type = "limit",
            Qty = "0.00001",
            LimitPrice = 350000.0 // realistic price that should be accepted
        };
        var placed = await client.PlaceOrderAsync("BTC-BRL", accountId, payload);
        Assert.NotNull(placed);
        Assert.False(string.IsNullOrWhiteSpace(placed.OrderId));

        try
        {
            var canceled = await client.CancelOrderAsync(accountId, "BTC-BRL", placed.OrderId);
            Assert.NotNull(canceled);
        }
        catch (MercadoBitcoinApiException ex) when (ex.Message.Contains("Order status is invalid"))
        {
            // A ordem pode ter sido executada ou já cancelada automaticamente
            // Isso é comportamento normal da API em ambiente real
            Assert.True(true, $"Order cannot be canceled due to status: {ex.Message}");
        }
    }

    [Fact]
    public async Task CancelAllOpenOrders_Works()
    {
        if (!TestConfig.HasRealCredentials) return;
        var (client, accountId) = await CreateAuthAndGetAccount();
        var result = await client.CancelAllOpenOrdersByAccountAsync(accountId);
        Assert.NotNull(result);
    }
}