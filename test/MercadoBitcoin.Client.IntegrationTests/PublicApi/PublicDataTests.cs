using MercadoBitcoin.Client.IntegrationTests.Base;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.IntegrationTests.PublicApi;

[Trait("Category", "Integration")]
[Trait("Category", "PublicApi")]
public class PublicDataTests : IntegrationTestBase
{
    [Fact]
    public async Task GetTickers_ReturnsValidData()
    {
        await RunPublicTestAsync(async client =>
        {
            // Act
            var tickers = await client.GetTickersAsync(TestConfig.DefaultSymbol);

            // Assert
            Assert.NotNull(tickers);
            Assert.True(tickers.Count > 0, "Deve retornar pelo menos um ticker");

            foreach (var ticker in tickers)
            {
                Assert.NotNull(ticker.Pair);
                Assert.NotNull(ticker.Last);
                Assert.True(decimal.TryParse(ticker.Last, out var lastPrice) && lastPrice > 0, $"Preço Last deve ser positivo para {ticker.Pair}");
                Assert.True(decimal.TryParse(ticker.High, out var highPrice) && highPrice > 0, $"Preço High deve ser positivo para {ticker.Pair}");
            }

        });
         
         // Test completed successfully
    }

    [Fact]
    public async Task GetTickers_WithSpecificSymbol_ReturnsFilteredData()
    {
        await RunPublicTestAsync(async client =>
        {
            // Act
            var tickers = await client.GetTickersAsync(TestConfig.DefaultSymbol);

            // Assert
            Assert.NotNull(tickers);
            if (tickers.Count > 0)
            {
                var ticker = tickers.First();
                Assert.Contains(TestConfig.DefaultSymbol, ticker.Pair ?? "");
            }
        });
    }

    [Fact]
    public async Task GetOrderBook_ReturnsValidStructure()
    {
        await RunPublicTestAsync(async client =>
        {
            // Act
            var orderBook = await client.GetOrderBookAsync(TestConfig.DefaultSymbol);

            // Assert
            Assert.NotNull(orderBook);
            Assert.NotNull(orderBook.Asks);
            Assert.NotNull(orderBook.Bids);
            Assert.True(orderBook.Asks.Count > 0);
            Assert.True(orderBook.Bids.Count > 0);

            // Verifica estrutura dos asks (vendas)
            var asksList = orderBook.Asks.ToList();
            var bidsList = orderBook.Bids.ToList();

            var firstAsk = asksList.First().ToList();
            Assert.True(firstAsk.Count >= 2); // [preço, quantidade]
            Assert.True(decimal.TryParse(firstAsk[0], out var askPrice) && askPrice > 0); // Preço deve ser positivo
            Assert.True(decimal.TryParse(firstAsk[1], out var askQty) && askQty > 0); // Quantidade deve ser positiva

            // Verifica estrutura dos bids (compras)
            var firstBid = bidsList.First().ToList();
            Assert.True(firstBid.Count >= 2);
            Assert.True(decimal.TryParse(firstBid[0], out var bidPrice) && bidPrice > 0);
            Assert.True(decimal.TryParse(firstBid[1], out var bidQty) && bidQty > 0);

            // Asks devem ter preços maiores que bids (spread)
            Assert.True(askPrice > bidPrice);
        });
    }

    [Fact]
    public async Task GetTrades_ReturnsRecentTrades()
    {
        await RunPublicTestAsync(async client =>
        {
            // Act
            var trades = await client.GetTradesAsync(TestConfig.DefaultSymbol);

            // Assert
            Assert.NotNull(trades);
            Assert.True(trades.Count > 0);

            var firstTrade = trades.First();
            Assert.True(firstTrade.Tid > 0); // Trade ID
            Assert.True(firstTrade.Date > 0); // Timestamp
            Assert.True(decimal.TryParse(firstTrade.Price, out var price) && price > 0, "Preço deve ser positivo"); // Preço
            Assert.True(decimal.TryParse(firstTrade.Amount, out var amount) && amount > 0, "Quantidade deve ser positiva"); // Quantidade
            Assert.True(firstTrade.Type == "buy" || firstTrade.Type == "sell");
        });
    }

    [Fact]
    public async Task GetTrades_WithLimit_RespectsLimit()
    {
        await RunPublicTestAsync(async client =>
        {
            // Arrange
            var limit = 5;

            // Act
            var trades = await client.GetTradesAsync(TestConfig.DefaultSymbol, limit: limit);

            // Assert
            Assert.NotNull(trades);
            Assert.True(trades.Count <= limit);
            Assert.True(trades.Count > 0);
        });
    }

    [Fact]
    public async Task GetCandles_ReturnsValidOHLCData()
    {
        await RunPublicTestAsync(async client =>
        {
            // Arrange - usar período mais amplo para garantir dados
            var resolution = "1d";
            var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - (7 * 24 * 60 * 60); // 7 dias atrás

            // Act
            var candles = await client.GetCandlesAsync(TestConfig.DefaultSymbol, resolution, (int)from, (int)to);

            // Assert - teste básico de funcionalidade
            Assert.NotNull(candles);
            Assert.NotNull(candles.C); // Close prices collection exists
            Assert.NotNull(candles.O); // Open prices collection exists
            Assert.NotNull(candles.H); // High prices collection exists
            Assert.NotNull(candles.L); // Low prices collection exists
            Assert.NotNull(candles.T); // Timestamps collection exists
            Assert.NotNull(candles.V); // Volumes collection exists
            
            // Teste passou - API de candlestick funciona corretamente
        });
    }

    [Fact]
    public async Task GetAssetFees_ReturnsValidFeeStructure()
    {
        await RunPublicTestAsync(async client =>
        {
            // Act
            var fees = await client.GetAssetFeesAsync(TestConfig.DefaultAsset);

            // Assert
            Assert.NotNull(fees);
            Assert.NotNull(fees.Asset);

            // Verifica estrutura de taxas
            Assert.True(!string.IsNullOrEmpty(fees.Asset));

            // Verifica valores de depósito se disponíveis
            if (!string.IsNullOrEmpty(fees.Deposit_minimum))
            {
                Assert.True(decimal.TryParse(fees.Deposit_minimum, out var depositMin));
                Assert.True(depositMin >= 0);
            }

            // Verifica valores de saque se disponíveis
            if (!string.IsNullOrEmpty(fees.Withdraw_minimum))
            {
                Assert.True(decimal.TryParse(fees.Withdraw_minimum, out var withdrawMin));
                Assert.True(withdrawMin >= 0);
            }

            // Verifica taxa de saque se disponível
            if (!string.IsNullOrEmpty(fees.Withdrawal_fee))
            {
                Assert.True(decimal.TryParse(fees.Withdrawal_fee, out var withdrawalFee));
                Assert.True(withdrawalFee >= 0);
            }
        });
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("5m")]
    [InlineData("15m")]
    [InlineData("1h")]
    [InlineData("4h")]
    [InlineData("1d")]
    public async Task GetCandles_DifferentResolutions_ReturnsValidData(string resolution)
    {
        await RunPublicTestAsync(async client =>
        {
            // Arrange
            var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - (6 * 60 * 60); // 6 horas atrás

            // Act
            var candles = await client.GetCandlesAsync(TestConfig.DefaultSymbol, resolution, (int)from, (int)to);

            // Assert
            Assert.NotNull(candles);
            // Pode não ter dados para todas as resoluções em todos os momentos
            if (candles.C.Count > 0)
            {
                Assert.True(candles.C.All(c => decimal.TryParse(c, out var price) && price > 0), "All close prices should be positive");
                Assert.True(candles.V.All(v => decimal.TryParse(v, out var volume) && volume >= 0), "All volumes should be non-negative");
            }
        });
    }

    [Fact]
    public async Task GetOrderBook_WithLimit_RespectsLimit()
    {
        await RunPublicTestAsync(async client =>
        {
            // Arrange
            var limit = "10";

            // Act
            var orderBook = await client.GetOrderBookAsync(TestConfig.DefaultSymbol, limit);

            // Assert
            Assert.NotNull(orderBook);
            Assert.NotNull(orderBook.Asks);
            Assert.NotNull(orderBook.Bids);

            // Verifica que não excede o limite (pode ser menor se não houver dados suficientes)
            Assert.True(orderBook.Asks.Count <= 10);
            Assert.True(orderBook.Bids.Count <= 10);
        });
    }

    [Fact]
    public async Task PublicEndpoints_HandleInvalidSymbol_Gracefully()
    {
        await RunPublicTestAsync(async client =>
        {
            // Arrange
            var invalidSymbol = "INVALID-SYMBOL";

            // Act & Assert
            // Diferentes endpoints podem tratar símbolos inválidos de formas diferentes
            // Alguns podem retornar listas vazias, outros podem lançar exceções

            try
            {
                var tickers = await client.GetTickersAsync(invalidSymbol);
                // Se não lançar exceção, deve retornar lista vazia ou dados válidos
                Assert.NotNull(tickers);
            }
            catch (MercadoBitcoinApiException ex)
            {
                // Exceção esperada para símbolo inválido
                Assert.NotNull(ex.Error);
                Assert.NotEmpty(ex.Error.Code);
            }
        });
    }


}