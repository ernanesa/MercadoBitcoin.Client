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
            
            // Validate orderbook structure
            var firstAsk = result.Asks.First();
            var firstBid = result.Bids.First();
            
            // Parse price and quantity from string arrays
            var askArray = firstAsk.ToArray();
            var bidArray = firstBid.ToArray();
            var askPrice = decimal.Parse(askArray[0]);
            var askQuantity = decimal.Parse(askArray[1]);
            var bidPrice = decimal.Parse(bidArray[0]);
            var bidQuantity = decimal.Parse(bidArray[1]);
            
            Assert.True(askPrice > 0);
            Assert.True(askQuantity > 0);
            Assert.True(bidPrice > 0);
            Assert.True(bidQuantity > 0);
            
            // Validate spread
            Assert.True(askPrice > bidPrice, "Ask price should be higher than bid price");
            
            LogTestResult("GetOrderbook", true, $"Asks: {result.Asks.Count()}, Bids: {result.Bids.Count()}, Spread: {askPrice - bidPrice:F2}");
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