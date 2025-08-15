using MercadoBitcoin.Client.UnitTests.Base;
using MercadoBitcoin.Client.WebSocket.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MercadoBitcoin.Client.UnitTests.WebSocket.Models;

[Trait("Category", "Unit")]
[Trait("Category", "WebSocket")]
[Trait("Category", "Models")]
public class WebSocketModelsTests : UnitTestBase
{
    [Fact]
    public void SubscribeMessage_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var message = new SubscribeMessage();

        // Assert
        message.Should().NotBeNull();
        message.Type.Should().Be("subscribe");
        message.Channel.Should().Be(string.Empty);
        message.Symbol.Should().BeNull();
        message.Timestamp.Should().Be(0);
    }

    [Fact]
    public void SubscribeMessage_WithValues_SetsPropertiesCorrectly()
    {
        // Arrange
        var channel = "BTC-BRL@trade";
        var symbol = "BTC-BRL";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var message = new SubscribeMessage
        {
            Channel = channel,
            Symbol = symbol,
            Timestamp = timestamp
        };

        // Assert
        message.Type.Should().Be("subscribe");
        message.Channel.Should().Be(channel);
        message.Symbol.Should().Be(symbol);
        message.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void TradeData_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var trade = new TradeData();

        // Assert
        trade.Should().NotBeNull();
        trade.Symbol.Should().Be(string.Empty);
        trade.Price.Should().Be(0);
        trade.Amount.Should().Be(0);
        trade.Side.Should().Be(string.Empty);
        trade.TradeTimestamp.Should().Be(0);
        trade.Id.Should().Be(string.Empty);
    }

    [Fact]
    public void TradeData_WithValues_SetsPropertiesCorrectly()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var price = 100000.50m;
        var amount = 0.001m;
        var side = "buy";
        var tradeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var id = "12345";

        // Act
        var trade = new TradeData
        {
            Symbol = symbol,
            Price = price,
            Amount = amount,
            Side = side,
            TradeTimestamp = tradeTimestamp,
            Id = id
        };

        // Assert
        trade.Symbol.Should().Be(symbol);
        trade.Price.Should().Be(price);
        trade.Amount.Should().Be(amount);
        trade.Side.Should().Be(side);
        trade.TradeTimestamp.Should().Be(tradeTimestamp);
        trade.Id.Should().Be(id);
    }

    [Fact]
    public void OrderBookData_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var orderBook = new OrderBookData();

        // Assert
        orderBook.Should().NotBeNull();
        orderBook.Symbol.Should().Be(string.Empty);
        orderBook.Bids.Should().NotBeNull();
        orderBook.Bids.Should().BeEmpty();
        orderBook.Asks.Should().NotBeNull();
        orderBook.Asks.Should().BeEmpty();
        orderBook.Timestamp.Should().Be(0);
    }

    [Fact]
    public void OrderBookData_WithValues_SetsPropertiesCorrectly()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var bids = new List<OrderBookEntry>
        {
            new OrderBookEntry { Price = 99000, Amount = 0.5m },
            new OrderBookEntry { Price = 98000, Amount = 1.0m }
        };
        var asks = new List<OrderBookEntry>
        {
            new OrderBookEntry { Price = 101000, Amount = 0.3m },
            new OrderBookEntry { Price = 102000, Amount = 0.8m }
        };

        // Act
        var orderBook = new OrderBookData
        {
            Symbol = symbol,
            Bids = bids,
            Asks = asks,
            Timestamp = timestamp
        };

        // Assert
        orderBook.Symbol.Should().Be(symbol);
        orderBook.Bids.Should().BeEquivalentTo(bids);
        orderBook.Asks.Should().BeEquivalentTo(asks);
        orderBook.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void OrderBookEntry_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var entry = new OrderBookEntry();

        // Assert
        entry.Should().NotBeNull();
        entry.Price.Should().Be(0);
        entry.Amount.Should().Be(0);
    }

    [Fact]
    public void OrderBookEntry_WithValues_SetsPropertiesCorrectly()
    {
        // Arrange
        var price = 100000.50m;
        var amount = 0.001m;

        // Act
        var entry = new OrderBookEntry
        {
            Price = price,
            Amount = amount
        };

        // Assert
        entry.Price.Should().Be(price);
        entry.Amount.Should().Be(amount);
    }

    [Fact]
    public void TickerData_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var ticker = new TickerData();

        // Assert
        ticker.Should().NotBeNull();
        ticker.Symbol.Should().Be(string.Empty);
        ticker.High.Should().Be(0);
        ticker.Low.Should().Be(0);
        ticker.Last.Should().Be(0);
        ticker.Bid.Should().Be(0);
        ticker.Ask.Should().Be(0);
        ticker.Volume.Should().Be(0);
        ticker.Open.Should().Be(0);
        ticker.Change.Should().Be(0);
    }

    [Fact]
    public void TickerData_WithValues_SetsPropertiesCorrectly()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var high = 105000m;
        var low = 95000m;
        var last = 100000m;
        var bid = 99500m;
        var ask = 100500m;
        var volume = 10.5m;
        var open = 98000m;
        var change = 2.5m;

        // Act
        var ticker = new TickerData
        {
            Symbol = symbol,
            High = high,
            Low = low,
            Last = last,
            Bid = bid,
            Ask = ask,
            Volume = volume,
            Open = open,
            Change = change
        };

        // Assert
        ticker.Symbol.Should().Be(symbol);
        ticker.High.Should().Be(high);
        ticker.Low.Should().Be(low);
        ticker.Last.Should().Be(last);
        ticker.Bid.Should().Be(bid);
        ticker.Ask.Should().Be(ask);
        ticker.Volume.Should().Be(volume);
        ticker.Open.Should().Be(open);
        ticker.Change.Should().Be(change);
    }

    [Fact]
    public void CandleData_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var candle = new CandleData();

        // Assert
        candle.Should().NotBeNull();
        candle.Symbol.Should().Be(string.Empty);
        candle.Interval.Should().Be(string.Empty);
        candle.Open.Should().Be(0);
        candle.High.Should().Be(0);
        candle.Low.Should().Be(0);
        candle.Close.Should().Be(0);
        candle.Volume.Should().Be(0);
        candle.OpenTime.Should().Be(0);
        candle.CloseTime.Should().Be(0);
    }

    [Fact]
    public void CandleData_WithValues_SetsPropertiesCorrectly()
    {
        // Arrange
        var symbol = "BTC-BRL";
        var interval = "1m";
        var open = 99000m;
        var high = 101000m;
        var low = 98000m;
        var close = 100000m;
        var volume = 5.5m;
        var openTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var closeTime = openTime + 60000; // 1 minute later

        // Act
        var candle = new CandleData
        {
            Symbol = symbol,
            Interval = interval,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
            OpenTime = openTime,
            CloseTime = closeTime
        };

        // Assert
        candle.Symbol.Should().Be(symbol);
        candle.Interval.Should().Be(interval);
        candle.Open.Should().Be(open);
        candle.High.Should().Be(high);
        candle.Low.Should().Be(low);
        candle.Close.Should().Be(close);
        candle.Volume.Should().Be(volume);
        candle.OpenTime.Should().Be(openTime);
        candle.CloseTime.Should().Be(closeTime);
    }

    [Fact]
    public void TradeData_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var trade = new TradeData
        {
            Symbol = "BTC-BRL",
            Price = 100000.50m,
            Amount = 0.001m,
            Side = "buy",
            TradeTimestamp = 1640995200000,
            Id = "12345"
        };

        // Act
        var json = JsonConvert.SerializeObject(trade);
        var deserializedTrade = JsonConvert.DeserializeObject<TradeData>(json);

        // Assert
        deserializedTrade!.Should().NotBeNull();
        deserializedTrade.Symbol.Should().Be(trade.Symbol);
        deserializedTrade.Price.Should().Be(trade.Price);
        deserializedTrade.Amount.Should().Be(trade.Amount);
        deserializedTrade.Side.Should().Be(trade.Side);
        deserializedTrade.TradeTimestamp.Should().Be(trade.TradeTimestamp);
        deserializedTrade.Id.Should().Be(trade.Id);
    }

    [Fact]
    public void OrderBookData_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var orderBook = new OrderBookData
        {
            Symbol = "BTC-BRL",
            Bids = new List<OrderBookEntry>
            {
                new OrderBookEntry { Price = 99000, Amount = 0.5m }
            },
            Asks = new List<OrderBookEntry>
            {
                new OrderBookEntry { Price = 101000, Amount = 0.3m }
            },
            Timestamp = 1640995200000
        };

        // Act
        var json = JsonConvert.SerializeObject(orderBook);
        var deserializedOrderBook = JsonConvert.DeserializeObject<OrderBookData>(json);

        // Assert
        deserializedOrderBook.Should().NotBeNull();
        deserializedOrderBook!.Symbol.Should().Be(orderBook.Symbol);
        deserializedOrderBook.Bids.Should().HaveCount(1);
        deserializedOrderBook.Asks.Should().HaveCount(1);
        deserializedOrderBook.Bids.First().Price.Should().Be(99000);
        deserializedOrderBook.Asks.First().Price.Should().Be(101000);
        deserializedOrderBook.Timestamp.Should().Be(orderBook.Timestamp);
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("5m")]
    [InlineData("15m")]
    [InlineData("30m")]
    [InlineData("1h")]
    [InlineData("4h")]
    [InlineData("6h")]
    [InlineData("12h")]
    [InlineData("1d")]
    public void CandleIntervals_AllValidIntervals_ShouldBeSupported(string interval)
    {
        // Arrange & Act
        var candle = new CandleData
        {
            Interval = interval
        };

        // Assert
        candle.Interval.Should().Be(interval);
    }

    [Theory]
    [InlineData("buy")]
    [InlineData("sell")]
    public void TradeData_ValidSides_ShouldBeSupported(string side)
    {
        // Arrange & Act
        var trade = new TradeData
        {
            Side = side
        };

        // Assert
        trade.Side.Should().Be(side);
    }

    [Fact]
    public void OrderBookData_EmptyBidsAndAsks_ShouldBeHandledGracefully()
    {
        // Arrange & Act
        var orderBook = new OrderBookData
        {
            Symbol = "BTC-BRL",
            Bids = new List<OrderBookEntry>(),
            Asks = new List<OrderBookEntry>(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // Assert
        orderBook.Bids.Should().NotBeNull();
        orderBook.Asks.Should().NotBeNull();
        orderBook.Bids.Should().BeEmpty();
        orderBook.Asks.Should().BeEmpty();
    }
}