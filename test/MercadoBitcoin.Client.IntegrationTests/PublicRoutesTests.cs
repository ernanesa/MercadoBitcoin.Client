using System;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.IntegrationTests;

public class PublicRoutesTests
{
    private MercadoBitcoinClient CreateClient() => new MercadoBitcoinClient();

    [Fact]
    public async Task GetAssetFees_Works()
    {
        var client = CreateClient();
        var result = await client.GetAssetFeesAsync("btc");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrderBook_Works()
    {
        var client = CreateClient();
        var result = await client.GetOrderBookAsync("BTC-BRL");
        Assert.NotNull(result);
        // Generated model has Asks, Bids, Timestamp
        Assert.NotNull(result.Asks);
        Assert.NotNull(result.Bids);
    }

    [Fact]
    public async Task GetTrades_Works()
    {
        var client = CreateClient();
        var result = await client.GetTradesAsync("BTC-BRL", limit: 5);
        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }

    [Fact]
    public async Task GetCandles_Works()
    {
        var client = CreateClient();
        var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = await client.GetCandlesAsync("BTC-BRL", resolution: "1m", to: to, countback: 2);
        Assert.NotNull(result);
        // Generated model exposes O, H, L, C, T, V
        Assert.NotNull(result.C);
        Assert.NotNull(result.T);
    }

    [Fact]
    public async Task GetSymbols_Works()
    {
        var client = CreateClient();
        var result = await client.GetSymbolsAsync();
        Assert.NotNull(result);
        // Generated model exposes collections like Symbol, Currency, BaseCurrency, etc.
        Assert.NotNull(result.Symbol);
    }

    [Fact]
    public async Task GetTickers_Works()
    {
        var client = CreateClient();
        var result = await client.GetTickersAsync("BTC-BRL");
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task GetAssetNetworks_Works()
    {
        var client = CreateClient();
        var result = await client.GetAssetNetworksAsync("usdc");
        Assert.NotNull(result);
    }
}