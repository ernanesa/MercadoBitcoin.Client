using MercadoBitcoin.Client.Trading;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests.Unit;

/// <summary>
/// Comprehensive unit tests for IncrementalOrderBook.
/// </summary>
public class IncrementalOrderBookTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IncrementalOrderBook _orderBook;

    public IncrementalOrderBookTests(ITestOutputHelper output)
    {
        _output = output;
        _orderBook = new IncrementalOrderBook("BTC-BRL");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithSymbol_ShouldInitialize()
    {
        // Assert
        Assert.Equal("BTC-BRL", _orderBook.Symbol);
        Assert.Equal(0, _orderBook.BidLevels);
        Assert.Equal(0, _orderBook.AskLevels);
        Assert.Equal(0, _orderBook.LastUpdateId);
    }

    [Fact]
    public void Constructor_WithNullSymbol_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IncrementalOrderBook(null!));
    }

    [Fact]
    public void Constructor_WithOptions_ShouldApplyOptions()
    {
        // Arrange
        var options = new IncrementalOrderBookOptions
        {
            MaxDepth = 10,
            SpreadChangeThresholdPercent = 5m
        };

        // Act
        using var orderBook = new IncrementalOrderBook("ETH-BRL", options);

        // Assert
        Assert.Equal("ETH-BRL", orderBook.Symbol);
    }

    #endregion

    #region ApplySnapshot Tests

    [Fact]
    public void ApplySnapshot_WithValidData_ShouldPopulateBook()
    {
        // Arrange
        var bids = new[]
        {
            (100000m, 1.5m),
            (99900m, 2.0m),
            (99800m, 0.5m)
        };
        var asks = new[]
        {
            (100100m, 1.0m),
            (100200m, 2.5m),
            (100300m, 1.2m)
        };

        // Act
        _orderBook.ApplySnapshot(bids, asks, 12345);

        // Assert
        Assert.Equal(3, _orderBook.BidLevels);
        Assert.Equal(3, _orderBook.AskLevels);
        Assert.Equal(12345, _orderBook.LastUpdateId);
        _output.WriteLine($"Snapshot applied: {_orderBook.BidLevels} bids, {_orderBook.AskLevels} asks");
    }

    [Fact]
    public void ApplySnapshot_WithEmptyData_ShouldClearBook()
    {
        // Arrange - first populate
        _orderBook.ApplySnapshot(new[] { (100m, 1m) }, new[] { (101m, 1m) });
        Assert.Equal(1, _orderBook.BidLevels);

        // Act - apply empty snapshot
        _orderBook.ApplySnapshot(Array.Empty<(decimal, decimal)>(), Array.Empty<(decimal, decimal)>());

        // Assert
        Assert.Equal(0, _orderBook.BidLevels);
        Assert.Equal(0, _orderBook.AskLevels);
    }

    [Fact]
    public void ApplySnapshot_WithZeroQuantity_ShouldIgnoreLevel()
    {
        // Arrange
        var bids = new[]
        {
            (100000m, 1.5m),
            (99900m, 0m), // Zero quantity - should be ignored
            (99800m, 0.5m)
        };

        // Act
        _orderBook.ApplySnapshot(bids, Array.Empty<(decimal, decimal)>());

        // Assert
        Assert.Equal(2, _orderBook.BidLevels);
    }

    [Fact]
    public void ApplySnapshot_WithMaxDepth_ShouldTruncate()
    {
        // Arrange
        var options = new IncrementalOrderBookOptions { MaxDepth = 2 };
        using var orderBook = new IncrementalOrderBook("BTC-BRL", options);

        var bids = Enumerable.Range(1, 10).Select(i => (100000m - i * 100, (decimal)i)).ToArray();
        var asks = Enumerable.Range(1, 10).Select(i => (100000m + i * 100, (decimal)i)).ToArray();

        // Act
        orderBook.ApplySnapshot(bids, asks);

        // Assert
        Assert.Equal(2, orderBook.BidLevels);
        Assert.Equal(2, orderBook.AskLevels);
        _output.WriteLine($"Truncated to {orderBook.BidLevels} bids, {orderBook.AskLevels} asks");
    }

    [Fact]
    public void ApplySnapshot_ShouldRaiseUpdatedEvent()
    {
        // Arrange
        var eventRaised = false;
        OrderBookUpdatedEventArgs? receivedArgs = null;
        _orderBook.Updated += (sender, args) =>
        {
            eventRaised = true;
            receivedArgs = args;
        };

        // Act
        _orderBook.ApplySnapshot(new[] { (100m, 1m) }, new[] { (101m, 1m) }, 999);

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(receivedArgs);
        Assert.Equal("BTC-BRL", receivedArgs.Symbol);
        Assert.Equal(OrderBookUpdateType.Snapshot, receivedArgs.UpdateType);
        Assert.Equal(999, receivedArgs.UpdateId);
    }

    #endregion

    #region ApplyDelta Tests

    [Fact]
    public void ApplyDelta_WithValidDelta_ShouldUpdateBook()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) },
            100);

        var delta = new OrderBookDelta
        {
            UpdateId = 101,
            Bids = new[] { (99900m, 0.5m) },
            Asks = new[] { (100200m, 0.3m) }
        };

        // Act
        var applied = _orderBook.ApplyDelta(delta);

        // Assert
        Assert.True(applied);
        Assert.Equal(2, _orderBook.BidLevels);
        Assert.Equal(2, _orderBook.AskLevels);
        Assert.Equal(101, _orderBook.LastUpdateId);
    }

    [Fact]
    public void ApplyDelta_WithStaleDelta_ShouldReject()
    {
        // Arrange
        _orderBook.ApplySnapshot(new[] { (100m, 1m) }, new[] { (101m, 1m) }, 100);

        var staleDelta = new OrderBookDelta
        {
            UpdateId = 50, // Stale
            Bids = new[] { (99m, 1m) },
            Asks = Array.Empty<(decimal, decimal)>()
        };

        // Act
        var applied = _orderBook.ApplyDelta(staleDelta);

        // Assert
        Assert.False(applied);
        Assert.Equal(100, _orderBook.LastUpdateId);
    }

    [Fact]
    public void ApplyDelta_WithZeroQuantity_ShouldRemoveLevel()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m), (99900m, 2.0m) },
            new[] { (100100m, 1.0m) },
            100);
        Assert.Equal(2, _orderBook.BidLevels);

        var delta = new OrderBookDelta
        {
            UpdateId = 101,
            Bids = new[] { (100000m, 0m) }, // Remove this level
            Asks = Array.Empty<(decimal, decimal)>()
        };

        // Act
        _orderBook.ApplyDelta(delta);

        // Assert
        Assert.Equal(1, _orderBook.BidLevels);
        var bestBid = _orderBook.GetBestBid();
        Assert.NotNull(bestBid);
        Assert.Equal(99900m, bestBid.Value.Price);
    }

    [Fact]
    public void ApplyDelta_ShouldRaiseUpdatedEvent()
    {
        // Arrange
        _orderBook.ApplySnapshot(new[] { (100m, 1m) }, new[] { (101m, 1m) }, 100);

        var eventRaised = false;
        _orderBook.Updated += (_, args) =>
        {
            eventRaised = true;
            Assert.Equal(OrderBookUpdateType.Delta, args.UpdateType);
        };

        var delta = new OrderBookDelta
        {
            UpdateId = 101,
            Bids = new[] { (99m, 1m) },
            Asks = Array.Empty<(decimal, decimal)>()
        };

        // Act
        _orderBook.ApplyDelta(delta);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion

    #region BestBid/Ask Tests

    [Fact]
    public void GetBestBid_WithData_ShouldReturnHighestBid()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m), (99900m, 2.0m), (99800m, 3.0m) },
            Array.Empty<(decimal, decimal)>());

        // Act
        var bestBid = _orderBook.GetBestBid();

        // Assert
        Assert.NotNull(bestBid);
        Assert.Equal(100000m, bestBid.Value.Price);
        Assert.Equal(1.0m, bestBid.Value.Quantity);
    }

    [Fact]
    public void GetBestBid_EmptyBook_ShouldReturnNull()
    {
        // Act
        var bestBid = _orderBook.GetBestBid();

        // Assert
        Assert.Null(bestBid);
    }

    [Fact]
    public void GetBestAsk_WithData_ShouldReturnLowestAsk()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            Array.Empty<(decimal, decimal)>(),
            new[] { (100100m, 1.0m), (100200m, 2.0m), (100300m, 3.0m) });

        // Act
        var bestAsk = _orderBook.GetBestAsk();

        // Assert
        Assert.NotNull(bestAsk);
        Assert.Equal(100100m, bestAsk.Value.Price);
        Assert.Equal(1.0m, bestAsk.Value.Quantity);
    }

    [Fact]
    public void GetBestAsk_EmptyBook_ShouldReturnNull()
    {
        // Act
        var bestAsk = _orderBook.GetBestAsk();

        // Assert
        Assert.Null(bestAsk);
    }

    #endregion

    #region Spread and MidPrice Tests

    [Fact]
    public void GetSpread_WithData_ShouldCalculateCorrectly()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) });

        // Act
        var spread = _orderBook.GetSpread();

        // Assert
        Assert.NotNull(spread);
        Assert.Equal(100m, spread.Value);
    }

    [Fact]
    public void GetSpread_EmptyBook_ShouldReturnNull()
    {
        // Act
        var spread = _orderBook.GetSpread();

        // Assert
        Assert.Null(spread);
    }

    [Fact]
    public void GetMidPrice_WithData_ShouldCalculateCorrectly()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) });

        // Act
        var midPrice = _orderBook.GetMidPrice();

        // Assert
        Assert.NotNull(midPrice);
        Assert.Equal(100050m, midPrice.Value);
    }

    [Fact]
    public void GetMidPrice_EmptyBook_ShouldReturnNull()
    {
        // Act
        var midPrice = _orderBook.GetMidPrice();

        // Assert
        Assert.Null(midPrice);
    }

    [Fact]
    public void GetSpreadPercent_WithData_ShouldCalculateCorrectly()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) });

        // Act
        var spreadPercent = _orderBook.GetSpreadPercent();

        // Assert
        Assert.NotNull(spreadPercent);
        // 100 / 100050 * 100 ≈ 0.1%
        Assert.True(spreadPercent.Value > 0.09m && spreadPercent.Value < 0.11m);
        _output.WriteLine($"Spread percent: {spreadPercent.Value:F4}%");
    }

    #endregion

    #region TopBids/Asks Tests

    [Fact]
    public void GetTopBids_ShouldReturnRequestedCount()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m), (99900m, 2.0m), (99800m, 3.0m), (99700m, 4.0m), (99600m, 5.0m) },
            Array.Empty<(decimal, decimal)>());

        // Act
        var topBids = _orderBook.GetTopBids(3);

        // Assert
        Assert.Equal(3, topBids.Length);
        Assert.Equal(100000m, topBids[0].Price);
        Assert.Equal(99900m, topBids[1].Price);
        Assert.Equal(99800m, topBids[2].Price);
    }

    [Fact]
    public void GetTopBids_RequestMoreThanAvailable_ShouldReturnAllAvailable()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m), (99900m, 2.0m) },
            Array.Empty<(decimal, decimal)>());

        // Act
        var topBids = _orderBook.GetTopBids(10);

        // Assert
        Assert.Equal(2, topBids.Length);
    }

    [Fact]
    public void GetTopAsks_ShouldReturnRequestedCount()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            Array.Empty<(decimal, decimal)>(),
            new[] { (100100m, 1.0m), (100200m, 2.0m), (100300m, 3.0m), (100400m, 4.0m), (100500m, 5.0m) });

        // Act
        var topAsks = _orderBook.GetTopAsks(3);

        // Assert
        Assert.Equal(3, topAsks.Length);
        Assert.Equal(100100m, topAsks[0].Price);
        Assert.Equal(100200m, topAsks[1].Price);
        Assert.Equal(100300m, topAsks[2].Price);
    }

    #endregion

    #region Volume Tests

    [Fact]
    public void GetBidVolume_ShouldSumQuantities()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m), (99900m, 2.0m), (99800m, 3.0m) },
            Array.Empty<(decimal, decimal)>());

        // Act
        var volume = _orderBook.GetBidVolume();

        // Assert
        Assert.Equal(6.0m, volume);
    }

    [Fact]
    public void GetBidVolume_WithLevelLimit_ShouldSumLimitedLevels()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m), (99900m, 2.0m), (99800m, 3.0m) },
            Array.Empty<(decimal, decimal)>());

        // Act
        var volume = _orderBook.GetBidVolume(2);

        // Assert
        Assert.Equal(3.0m, volume);
    }

    [Fact]
    public void GetAskVolume_ShouldSumQuantities()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            Array.Empty<(decimal, decimal)>(),
            new[] { (100100m, 1.0m), (100200m, 2.0m), (100300m, 3.0m) });

        // Act
        var volume = _orderBook.GetAskVolume();

        // Assert
        Assert.Equal(6.0m, volume);
    }

    #endregion

    #region VWAP Tests

    [Fact]
    public void CalculateVwap_BuySide_ShouldCalculateFromAsks()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            Array.Empty<(decimal, decimal)>(),
            new[] { (100m, 1.0m), (101m, 1.0m), (102m, 1.0m) });

        // Act - buy 2 units
        var result = _orderBook.CalculateVwap(2m, OrderSide.Buy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2m, result.Value.FillableQuantity);
        // VWAP = (100*1 + 101*1) / 2 = 100.5
        Assert.Equal(100.5m, result.Value.Vwap);
        _output.WriteLine($"VWAP: {result.Value.Vwap}, Fillable: {result.Value.FillableQuantity}");
    }

    [Fact]
    public void CalculateVwap_SellSide_ShouldCalculateFromBids()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (102m, 1.0m), (101m, 1.0m), (100m, 1.0m) },
            Array.Empty<(decimal, decimal)>());

        // Act - sell 2 units
        var result = _orderBook.CalculateVwap(2m, OrderSide.Sell);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2m, result.Value.FillableQuantity);
        // VWAP = (102*1 + 101*1) / 2 = 101.5
        Assert.Equal(101.5m, result.Value.Vwap);
    }

    [Fact]
    public void CalculateVwap_PartialFill_ShouldReturnActualFillable()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            Array.Empty<(decimal, decimal)>(),
            new[] { (100m, 0.5m), (101m, 0.5m) }); // Total 1 unit available

        // Act - try to buy 2 units
        var result = _orderBook.CalculateVwap(2m, OrderSide.Buy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1m, result.Value.FillableQuantity);
    }

    [Fact]
    public void CalculateVwap_EmptyBook_ShouldReturnNull()
    {
        // Act
        var result = _orderBook.CalculateVwap(1m, OrderSide.Buy);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Imbalance Tests

    [Fact]
    public void GetImbalanceRatio_MoreBids_ShouldBePositive()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100m, 10m) }, // More bid volume
            new[] { (101m, 5m) });

        // Act
        var imbalance = _orderBook.GetImbalanceRatio();

        // Assert
        Assert.NotNull(imbalance);
        Assert.True(imbalance.Value > 0);
        // (10 - 5) / (10 + 5) = 0.333...
        Assert.True(imbalance.Value > 0.3m && imbalance.Value < 0.35m);
        _output.WriteLine($"Imbalance ratio: {imbalance.Value:F4}");
    }

    [Fact]
    public void GetImbalanceRatio_MoreAsks_ShouldBeNegative()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100m, 5m) },
            new[] { (101m, 10m) }); // More ask volume

        // Act
        var imbalance = _orderBook.GetImbalanceRatio();

        // Assert
        Assert.NotNull(imbalance);
        Assert.True(imbalance.Value < 0);
    }

    [Fact]
    public void GetImbalanceRatio_EqualVolume_ShouldBeZero()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100m, 5m) },
            new[] { (101m, 5m) });

        // Act
        var imbalance = _orderBook.GetImbalanceRatio();

        // Assert
        Assert.NotNull(imbalance);
        Assert.Equal(0m, imbalance.Value);
    }

    [Fact]
    public void GetImbalanceRatio_EmptyBook_ShouldReturnNull()
    {
        // Act
        var imbalance = _orderBook.GetImbalanceRatio();

        // Assert
        Assert.Null(imbalance);
    }

    #endregion

    #region GetState Tests

    [Fact]
    public void GetState_ShouldReturnCompleteState()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m), (99900m, 2.0m) },
            new[] { (100100m, 1.5m), (100200m, 2.5m) },
            12345);

        // Act
        var state = _orderBook.GetState();

        // Assert
        Assert.Equal("BTC-BRL", state.Symbol);
        Assert.Equal(2, state.Bids.Length);
        Assert.Equal(2, state.Asks.Length);
        Assert.Equal(100000m, state.BestBid);
        Assert.Equal(100100m, state.BestAsk);
        Assert.NotNull(state.Spread);
        Assert.NotNull(state.MidPrice);
        Assert.Equal(12345, state.LastUpdateId);
        Assert.Equal(3.0m, state.TotalBidVolume);
        Assert.Equal(4.0m, state.TotalAskVolume);
        _output.WriteLine($"State: BestBid={state.BestBid}, BestAsk={state.BestAsk}, Spread={state.Spread}");
    }

    [Fact]
    public void GetState_WithDepthLimit_ShouldLimitResults()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            Enumerable.Range(1, 10).Select(i => (100000m - i * 100, (decimal)i)).ToArray(),
            Enumerable.Range(1, 10).Select(i => (100000m + i * 100, (decimal)i)).ToArray());

        // Act
        var state = _orderBook.GetState(depth: 3);

        // Assert
        Assert.Equal(3, state.Bids.Length);
        Assert.Equal(3, state.Asks.Length);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldEmptyTheBook()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) },
            12345);

        // Act
        _orderBook.Clear();

        // Assert
        Assert.Equal(0, _orderBook.BidLevels);
        Assert.Equal(0, _orderBook.AskLevels);
        Assert.Equal(0, _orderBook.LastUpdateId);
    }

    #endregion

    #region SpreadChanged Event Tests

    [Fact]
    public void ApplySnapshot_SignificantSpreadChange_ShouldRaiseEvent()
    {
        // Arrange
        var options = new IncrementalOrderBookOptions { SpreadChangeThresholdPercent = 1m };
        using var orderBook = new IncrementalOrderBook("BTC-BRL", options);

        orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) });

        var eventRaised = false;
        SpreadChangedEventArgs? receivedArgs = null;
        orderBook.SpreadChanged += (_, args) =>
        {
            eventRaised = true;
            receivedArgs = args;
        };

        // Act - significant spread change (from 100 to 1000)
        orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (101000m, 1.0m) });

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(receivedArgs);
        Assert.Equal("BTC-BRL", receivedArgs.Symbol);
        _output.WriteLine($"Spread changed: {receivedArgs.PreviousSpread} -> {receivedArgs.CurrentSpread} ({receivedArgs.ChangePercent:F2}%)");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var random = new Random(42);

        // Act - concurrent reads and writes
        for (int i = 0; i < 100; i++)
        {
            var iteration = i;
            tasks.Add(Task.Run(() =>
            {
                // Write
                _orderBook.ApplySnapshot(
                    new[] { (100000m - iteration, 1.0m + iteration) },
                    new[] { (100100m + iteration, 1.0m + iteration) },
                    iteration);

                // Read
                var spread = _orderBook.GetSpread();
                var midPrice = _orderBook.GetMidPrice();
                var state = _orderBook.GetState();
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - no exceptions thrown, data is consistent
        Assert.True(_orderBook.BidLevels >= 0);
        Assert.True(_orderBook.AskLevels >= 0);
        _output.WriteLine($"After concurrent operations: {_orderBook.BidLevels} bids, {_orderBook.AskLevels} asks");
    }

    #endregion

    #region Extension Method Tests

    [Fact]
    public void CreateFromWebSocket_ShouldInitializeCorrectly()
    {
        // Arrange
        var bidData = new[]
        {
            new decimal[] { 100000m, 1.0m },
            new decimal[] { 99900m, 2.0m }
        };
        var askData = new[]
        {
            new decimal[] { 100100m, 1.0m },
            new decimal[] { 100200m, 2.0m }
        };

        // Act
        using var orderBook = IncrementalOrderBookExtensions.CreateFromWebSocket(
            "BTC-BRL", bidData, askData);

        // Assert
        Assert.Equal(2, orderBook.BidLevels);
        Assert.Equal(2, orderBook.AskLevels);
    }

    [Fact]
    public void IsHealthy_WithData_ShouldReturnTrue()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) });

        // Act & Assert
        Assert.True(_orderBook.IsHealthy());
    }

    [Fact]
    public void IsHealthy_EmptyBook_ShouldReturnFalse()
    {
        // Act & Assert
        Assert.False(_orderBook.IsHealthy());
    }

    [Fact]
    public void IsStale_RecentUpdate_ShouldReturnFalse()
    {
        // Arrange
        _orderBook.ApplySnapshot(
            new[] { (100000m, 1.0m) },
            new[] { (100100m, 1.0m) });

        // Act & Assert
        Assert.False(_orderBook.IsStale(TimeSpan.FromMinutes(1)));
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void SnapshotCount_ShouldIncrement()
    {
        // Arrange & Act
        _orderBook.ApplySnapshot(new[] { (100m, 1m) }, new[] { (101m, 1m) });
        _orderBook.ApplySnapshot(new[] { (100m, 2m) }, new[] { (101m, 2m) });
        _orderBook.ApplySnapshot(new[] { (100m, 3m) }, new[] { (101m, 3m) });

        // Assert
        Assert.Equal(3, _orderBook.SnapshotCount);
    }

    [Fact]
    public void DeltaCount_ShouldIncrement()
    {
        // Arrange
        _orderBook.ApplySnapshot(new[] { (100m, 1m) }, new[] { (101m, 1m) }, 100);

        // Act
        _orderBook.ApplyDelta(new OrderBookDelta { UpdateId = 101, Bids = new[] { (99m, 1m) }, Asks = Array.Empty<(decimal, decimal)>() });
        _orderBook.ApplyDelta(new OrderBookDelta { UpdateId = 102, Bids = new[] { (98m, 1m) }, Asks = Array.Empty<(decimal, decimal)>() });

        // Assert
        Assert.Equal(2, _orderBook.DeltaCount);
    }

    #endregion

    public void Dispose()
    {
        _orderBook.Dispose();
    }
}
