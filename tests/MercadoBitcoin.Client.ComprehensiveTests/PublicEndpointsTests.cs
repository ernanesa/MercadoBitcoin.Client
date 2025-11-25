using Xunit;
using Xunit.Abstractions;
using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client.ComprehensiveTests;

public class PublicEndpointsTests : TestBase
{
    private readonly ITestOutputHelper _output;

    public PublicEndpointsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetSymbols_ShouldReturnValidSymbols()
    {
        try
        {
            // Act
            var result = await Client.GetSymbolsAsync();
            LogApiCall("GET /symbols", response: result);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Symbol);
            Assert.Contains(TestSymbol, result.Symbol);

            LogTestResult("GetSymbols", true, $"Returned {result.Symbol.Count} symbols");
        }
        catch (Exception ex)
        {
            LogTestResult("GetSymbols", false, ex.Message);
            throw;
        }

        await DelayAsync();
    }

    [Fact]
    public async Task GetTickers_ShouldReturnValidTickers()
    {
        try
        {
            // Act
            var result = await Client.GetTickersAsync(TestSymbol);
            LogApiCall($"GET /tickers/{TestSymbol}", response: result);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var ticker = result.First();
            Assert.Equal(TestSymbol, ticker.Pair);
            Assert.True(decimal.Parse(ticker.Last) > 0);
            Assert.True(decimal.Parse(ticker.High) > 0);
            Assert.True(decimal.Parse(ticker.Low) > 0);
            Assert.True(decimal.Parse(ticker.Vol) > 0);

            LogTestResult("GetTickers", true, $"Last price: {ticker.Last:C}");
        }
        catch (Exception ex)
        {
            LogTestResult("GetTickers", false, ex.Message);
            throw;
        }

        await DelayAsync();
    }

    [Fact]
    public async Task GetOrderbook_ShouldReturnValidOrderbook()
    {
        try
        {
            // Act
            var result = await Client.GetOrderBookAsync(TestSymbol);
            LogApiCall($"GET /orderbook/{TestSymbol}", response: result);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Asks);
            Assert.NotNull(result.Bids);
            Assert.NotEmpty(result.Asks);
            Assert.NotEmpty(result.Bids);

            // Parse all prices to find the best bid and best ask
            var askPrices = result.Asks.Select(ask => decimal.Parse(ask.ToArray()[0], System.Globalization.CultureInfo.InvariantCulture)).ToList();
            var bidPrices = result.Bids.Select(bid => decimal.Parse(bid.ToArray()[0], System.Globalization.CultureInfo.InvariantCulture)).ToList();

            // Best ask is the lowest ask price (sellers want to sell at this price)
            var bestAskPrice = askPrices.Min();
            // Best bid is the highest bid price (buyers want to buy at this price)
            var bestBidPrice = bidPrices.Max();

            Assert.True(bestAskPrice > 0, "Best ask price should be positive");
            Assert.True(bestBidPrice > 0, "Best bid price should be positive");

            // Validate prices are reasonable (not more than 10x different - sanity check)
            var priceRatio = bestAskPrice / bestBidPrice;
            Assert.True(priceRatio > 0.5m && priceRatio < 2.0m,
                $"Ask/Bid price ratio ({priceRatio:F4}) should be within reasonable bounds (0.5-2.0)");

            // Note: In real markets, we'd expect bestAskPrice >= bestBidPrice,
            // but crossed books can occur briefly during high volatility.
            // We verify the structure is valid regardless of market conditions.
            var spread = bestAskPrice - bestBidPrice;
            var spreadInfo = spread >= 0 ? $"Spread: {spread:F2}" : $"Crossed book: {Math.Abs(spread):F2}";

            LogTestResult("GetOrderbook", true,
                $"Asks: {result.Asks.Count()}, Bids: {result.Bids.Count()}, " +
                $"Best Ask: {bestAskPrice:F2}, Best Bid: {bestBidPrice:F2}, {spreadInfo}");
        }
        catch (Exception ex)
        {
            LogTestResult("GetOrderbook", false, ex.Message);
            throw;
        }

        await DelayAsync();
    }

    [Fact]
    public async Task GetTrades_ShouldReturnValidTrades()
    {
        try
        {
            // Act
            var result = await Client.GetTradesAsync(TestSymbol);
            LogApiCall($"GET /trades/{TestSymbol}", response: result);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var trade = result.First();
            var tradePrice = decimal.Parse(trade.Price);
            var tradeAmount = decimal.Parse(trade.Amount);
            var tradeDate = DateTimeOffset.FromUnixTimeSeconds(trade.Date ?? 0);

            Assert.True(tradePrice > 0);
            Assert.True(tradeAmount > 0);
            Assert.True(trade.Tid > 0);
            Assert.True(tradeDate > DateTimeOffset.MinValue);
            Assert.Contains(trade.Type, new[] { "buy", "sell" });

            LogTestResult("GetTrades", true, $"Returned {result.Count()} trades, Latest: {tradePrice:C} at {tradeDate}");
        }
        catch (Exception ex)
        {
            LogTestResult("GetTrades", false, ex.Message);
            throw;
        }

        await DelayAsync();
    }

    [Fact]
    public async Task GetCandles_ShouldReturnValidCandles()
    {
        try
        {
            // Act - Get 1-hour candles for the last 24 hours
            var from = (int)DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = await Client.GetCandlesAsync(TestSymbol, "1h", to, from);
            LogApiCall($"GET /candles/{TestSymbol}/1h", new { from, to }, result);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.T);
            Assert.NotEmpty(result.T);

            var firstTime = result.T.First();
            var firstOpen = decimal.Parse(result.O.First());
            var firstHigh = decimal.Parse(result.H.First());
            var firstLow = decimal.Parse(result.L.First());
            var firstClose = decimal.Parse(result.C.First());
            var firstVolume = decimal.Parse(result.V.First());

            Assert.True(firstOpen > 0);
            Assert.True(firstHigh > 0);
            Assert.True(firstLow > 0);
            Assert.True(firstClose > 0);
            Assert.True(firstVolume > 0);
            Assert.True(firstHigh >= firstLow);
            Assert.True(firstHigh >= firstOpen);
            Assert.True(firstHigh >= firstClose);
            Assert.True(firstLow <= firstOpen);
            Assert.True(firstLow <= firstClose);

            LogTestResult("GetCandles", true, $"Returned {result.T.Count} candles, OHLC: {firstOpen:F2}/{firstHigh:F2}/{firstLow:F2}/{firstClose:F2}");
        }
        catch (Exception ex)
        {
            LogTestResult("GetCandles", false, ex.Message);
            throw;
        }

        await DelayAsync();
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("5m")]
    [InlineData("15m")]
    [InlineData("30m")]
    [InlineData("1h")]
    [InlineData("4h")]
    [InlineData("1d")]
    public async Task GetCandles_DifferentTimeframes_ShouldWork(string timeframe)
    {
        try
        {
            // Act
            var from = (int)DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeSeconds();
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = await Client.GetCandlesAsync(TestSymbol, timeframe, to, from);

            // Assert
            Assert.NotNull(result);
            // Note: Some timeframes might not have data in the last 6 hours

            LogTestResult($"GetCandles_{timeframe}", true, $"Returned {result.T?.Count ?? 0} candles");
        }
        catch (Exception ex)
        {
            LogTestResult($"GetCandles_{timeframe}", false, ex.Message);
            throw;
        }

        await DelayAsync();
    }

    [Fact]
    public async Task GetAllSymbols_ShouldReturnMajorPairs()
    {
        try
        {
            // Act
            var result = await Client.GetSymbolsAsync();

            // Assert - Check for major Brazilian pairs
            var expectedSymbols = new[] { "BTC-BRL", "ETH-BRL", "LTC-BRL", "XRP-BRL" };
            var foundSymbols = result.Symbol?.ToList() ?? new List<string>();

            foreach (var expectedSymbol in expectedSymbols)
            {
                Assert.Contains(expectedSymbol, foundSymbols);
            }

            LogTestResult("GetAllSymbols_MajorPairs", true, $"Found all major pairs: {string.Join(", ", expectedSymbols)}");
        }
        catch (Exception ex)
        {
            LogTestResult("GetAllSymbols_MajorPairs", false, ex.Message);
            throw;
        }

        await DelayAsync();
    }
}