using MercadoBitcoin.Client.Trading;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests.Unit;

/// <summary>
/// Comprehensive unit tests for PerformanceMonitor.
/// </summary>
public class PerformanceMonitorTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly PerformanceMonitor _monitor;

    public PerformanceMonitorTests(ITestOutputHelper output)
    {
        _output = output;
        _monitor = new PerformanceMonitor();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Default_ShouldInitialize()
    {
        // Arrange & Act
        using var monitor = new PerformanceMonitor();

        // Assert
        Assert.NotNull(monitor);
    }

    [Fact]
    public void Constructor_WithOptions_ShouldApplyOptions()
    {
        // Arrange
        var options = new PerformanceMonitorOptions
        {
            LatencyThresholdMicroseconds = 5000,
            EnablePeriodicReporting = false
        };

        // Act
        using var monitor = new PerformanceMonitor(options);

        // Assert
        Assert.NotNull(monitor);
    }

    #endregion

    #region Measure Sync Tests

    [Fact]
    public void Measure_Action_ShouldRecordLatency()
    {
        // Arrange
        var operationName = "TestAction";

        // Act
        _monitor.Measure(operationName, () =>
        {
            Thread.Sleep(10); // 10ms delay
        });

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        Assert.True(stats.AverageLatencyMicroseconds >= 10000); // At least 10ms
        _output.WriteLine($"Action latency: {stats.AverageLatencyMicroseconds}µs");
    }

    [Fact]
    public void Measure_Func_ShouldRecordLatencyAndReturnResult()
    {
        // Arrange
        var operationName = "TestFunc";
        var expectedResult = 42;

        // Act
        var result = _monitor.Measure(operationName, () =>
        {
            Thread.Sleep(5);
            return expectedResult;
        });

        // Assert
        Assert.Equal(expectedResult, result);
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        _output.WriteLine($"Func latency: {stats.AverageLatencyMicroseconds}µs");
    }

    #endregion

    #region Measure Async Tests

    [Fact]
    public async Task MeasureAsync_Task_ShouldRecordLatency()
    {
        // Arrange
        var operationName = "TestAsyncAction";

        // Act
        await _monitor.MeasureAsync(operationName, async () =>
        {
            await Task.Delay(15);
        });

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        Assert.True(stats.AverageLatencyMicroseconds >= 15000);
        _output.WriteLine($"Async action latency: {stats.AverageLatencyMicroseconds}µs");
    }

    [Fact]
    public async Task MeasureAsync_TaskWithResult_ShouldRecordLatencyAndReturnResult()
    {
        // Arrange
        var operationName = "TestAsyncFunc";
        var expectedResult = "Hello World";

        // Act
        var result = await _monitor.MeasureAsync(operationName, async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        });

        // Assert
        Assert.Equal(expectedResult, result);
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        _output.WriteLine($"Async func latency: {stats.AverageLatencyMicroseconds}µs");
    }

    #endregion

    #region MeasurementScope Tests

    [Fact]
    public void StartMeasurement_ShouldRecordOnDispose()
    {
        // Arrange
        var operationName = "ScopedOperation";

        // Act
        using (_monitor.StartMeasurement(operationName))
        {
            Thread.Sleep(5);
        }

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        Assert.True(stats.AverageLatencyMicroseconds >= 5000);
        _output.WriteLine($"Scoped measurement latency: {stats.AverageLatencyMicroseconds}µs");
    }

    [Fact]
    public void StartMeasurement_MultipleMeasurements_ShouldAggregate()
    {
        // Arrange
        var operationName = "RepeatedOperation";

        // Act
        for (int i = 0; i < 5; i++)
        {
            using (_monitor.StartMeasurement(operationName))
            {
                Thread.Sleep(2);
            }
        }

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(5, stats.Count);
        _output.WriteLine($"Repeated operation: count={stats.Count}, avg={stats.AverageLatencyMicroseconds}µs");
    }

    #endregion

    #region RecordLatency Tests

    [Fact]
    public void RecordLatency_DirectTicks_ShouldRecord()
    {
        // Arrange
        var operationName = "DirectTicks";
        var ticks = System.Diagnostics.Stopwatch.Frequency / 1000; // ~1ms

        // Act
        _monitor.RecordLatency(operationName, ticks);

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        _output.WriteLine($"Direct ticks latency: {stats.AverageLatencyMicroseconds}µs");
    }

    [Fact]
    public void RecordLatencyMicroseconds_ShouldRecord()
    {
        // Arrange
        var operationName = "DirectMicroseconds";
        var microseconds = 5000L; // 5ms

        // Act
        _monitor.RecordLatencyMicroseconds(operationName, microseconds);

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        // Allow some tolerance due to conversion
        Assert.True(stats.AverageLatencyMicroseconds >= 4500 && stats.AverageLatencyMicroseconds <= 5500);
    }

    [Fact]
    public void RecordLatencyMilliseconds_ShouldRecord()
    {
        // Arrange
        var operationName = "DirectMilliseconds";
        var milliseconds = 10.5; // 10.5ms

        // Act
        _monitor.RecordLatencyMilliseconds(operationName, milliseconds);

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        // Should be around 10500µs
        Assert.True(stats.AverageLatencyMicroseconds >= 10000 && stats.AverageLatencyMicroseconds <= 11000);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void GetStats_NonExistentOperation_ShouldReturnNull()
    {
        // Act
        var stats = _monitor.GetStats("NonExistent");

        // Assert
        Assert.Null(stats);
    }

    [Fact]
    public void GetStats_ShouldCalculateMinMax()
    {
        // Arrange
        var operationName = "MinMaxTest";
        _monitor.RecordLatencyMicroseconds(operationName, 1000);
        _monitor.RecordLatencyMicroseconds(operationName, 5000);
        _monitor.RecordLatencyMicroseconds(operationName, 3000);

        // Act
        var stats = _monitor.GetStats(operationName);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.Count);
        Assert.True(stats.MinLatencyMicroseconds <= stats.AverageLatencyMicroseconds);
        Assert.True(stats.MaxLatencyMicroseconds >= stats.AverageLatencyMicroseconds);
        _output.WriteLine($"Min={stats.MinLatencyMicroseconds}µs, Avg={stats.AverageLatencyMicroseconds}µs, Max={stats.MaxLatencyMicroseconds}µs");
    }

    [Fact]
    public void GetStats_ShouldCalculatePercentiles()
    {
        // Arrange
        var operationName = "PercentileTest";
        // Record 100 measurements with increasing latencies
        for (int i = 1; i <= 100; i++)
        {
            _monitor.RecordLatencyMicroseconds(operationName, i * 100);
        }

        // Act
        var stats = _monitor.GetStats(operationName);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(100, stats.Count);
        Assert.True(stats.P50LatencyMicroseconds > 0);
        Assert.True(stats.P95LatencyMicroseconds > stats.P50LatencyMicroseconds);
        Assert.True(stats.P99LatencyMicroseconds >= stats.P95LatencyMicroseconds);
        _output.WriteLine($"P50={stats.P50LatencyMicroseconds}µs, P95={stats.P95LatencyMicroseconds}µs, P99={stats.P99LatencyMicroseconds}µs");
    }

    [Fact]
    public void GetAllStats_ShouldReturnAllOperations()
    {
        // Arrange
        _monitor.RecordLatencyMicroseconds("Operation1", 1000);
        _monitor.RecordLatencyMicroseconds("Operation2", 2000);
        _monitor.RecordLatencyMicroseconds("Operation3", 3000);

        // Act
        var allStats = _monitor.GetAllStats();

        // Assert
        Assert.Equal(3, allStats.Count);
        Assert.Contains("Operation1", allStats.Keys);
        Assert.Contains("Operation2", allStats.Keys);
        Assert.Contains("Operation3", allStats.Keys);
    }

    #endregion

    #region Report Tests

    [Fact]
    public void GetReport_ShouldGenerateCompleteReport()
    {
        // Arrange
        _monitor.RecordLatencyMicroseconds("FastOp", 100);
        _monitor.RecordLatencyMicroseconds("MediumOp", 1000);
        _monitor.RecordLatencyMicroseconds("SlowOp", 10000);

        // Act
        var report = _monitor.GetReport();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(3, report.TotalOperations);
        Assert.Equal(3, report.OperationStats.Count);
        Assert.NotEmpty(report.SlowestOperations);
        Assert.NotEmpty(report.FastestOperations);
        Assert.Contains("SlowOp", report.SlowestOperations);
        Assert.Contains("FastOp", report.FastestOperations);
        _output.WriteLine($"Report generated at {report.GeneratedAt}: {report.TotalOperations} operations");
    }

    [Fact]
    public void GetReport_EmptyMonitor_ShouldReturnEmptyReport()
    {
        // Act
        var report = _monitor.GetReport();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(0, report.TotalOperations);
        Assert.Empty(report.OperationStats);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        _monitor.RecordLatencyMicroseconds("Op1", 1000);
        _monitor.RecordLatencyMicroseconds("Op2", 2000);
        Assert.Equal(2, _monitor.GetAllStats().Count);

        // Act
        _monitor.Reset();

        // Assert
        Assert.Empty(_monitor.GetAllStats());
    }

    [Fact]
    public void Reset_SpecificOperation_ShouldClearOnlyThatOperation()
    {
        // Arrange
        _monitor.RecordLatencyMicroseconds("Op1", 1000);
        _monitor.RecordLatencyMicroseconds("Op2", 2000);

        // Act
        _monitor.Reset("Op1");

        // Assert
        Assert.Equal(1, _monitor.GetAllStats().Count);
        Assert.Null(_monitor.GetStats("Op1"));
        Assert.NotNull(_monitor.GetStats("Op2"));
    }

    #endregion

    #region Threshold Event Tests

    [Fact]
    public void RecordLatency_ExceedsThreshold_ShouldRaiseEvent()
    {
        // Arrange
        var options = new PerformanceMonitorOptions
        {
            LatencyThresholdMicroseconds = 1000 // 1ms threshold
        };
        using var monitor = new PerformanceMonitor(options);

        var eventRaised = false;
        LatencyThresholdExceededEventArgs? receivedArgs = null;
        monitor.LatencyThresholdExceeded += (_, args) =>
        {
            eventRaised = true;
            receivedArgs = args;
        };

        // Act - record latency exceeding threshold
        monitor.RecordLatencyMicroseconds("SlowOp", 5000); // 5ms > 1ms threshold

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(receivedArgs);
        Assert.Equal("SlowOp", receivedArgs.Operation);
        Assert.Equal(5000, receivedArgs.LatencyMicroseconds);
        Assert.Equal(1000, receivedArgs.ThresholdMicroseconds);
        _output.WriteLine($"Threshold exceeded: {receivedArgs.LatencyMicroseconds}µs > {receivedArgs.ThresholdMicroseconds}µs");
    }

    [Fact]
    public void RecordLatency_BelowThreshold_ShouldNotRaiseEvent()
    {
        // Arrange
        var options = new PerformanceMonitorOptions
        {
            LatencyThresholdMicroseconds = 10000 // 10ms threshold
        };
        using var monitor = new PerformanceMonitor(options);

        var eventRaised = false;
        monitor.LatencyThresholdExceeded += (_, _) => eventRaised = true;

        // Act - record latency below threshold
        monitor.RecordLatencyMicroseconds("FastOp", 1000); // 1ms < 10ms threshold

        // Assert
        Assert.False(eventRaised);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentRecording_ShouldBeThreadSafe()
    {
        // Arrange
        var operationName = "ConcurrentOp";
        var tasks = new List<Task>();
        var iterations = 100;

        // Act - concurrent writes
        for (int i = 0; i < iterations; i++)
        {
            var delay = i;
            tasks.Add(Task.Run(() =>
            {
                _monitor.RecordLatencyMicroseconds(operationName, delay * 10);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(iterations, stats.Count);
        _output.WriteLine($"Concurrent recording: {stats.Count} measurements");
    }

    [Fact]
    public async Task ConcurrentMeasure_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var iterations = 50;

        // Act - concurrent measurements with scope
        for (int i = 0; i < iterations; i++)
        {
            var operationIndex = i % 5; // 5 different operations
            tasks.Add(Task.Run(() =>
            {
                using (_monitor.StartMeasurement($"Op{operationIndex}"))
                {
                    Thread.SpinWait(1000);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var allStats = _monitor.GetAllStats();
        Assert.Equal(5, allStats.Count);
        foreach (var (name, stats) in allStats)
        {
            Assert.Equal(10, stats.Count);
            _output.WriteLine($"{name}: {stats.Count} measurements");
        }
    }

    #endregion

    #region OperationStats Properties Tests

    [Fact]
    public void OperationStats_AverageLatencyMs_ShouldConvertCorrectly()
    {
        // Arrange
        _monitor.RecordLatencyMicroseconds("TestOp", 5000); // 5ms

        // Act
        var stats = _monitor.GetStats("TestOp");

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.AverageLatencyMs >= 4.5 && stats.AverageLatencyMs <= 5.5);
    }

    [Fact]
    public void OperationStats_P99LatencyMs_ShouldConvertCorrectly()
    {
        // Arrange
        for (int i = 1; i <= 100; i++)
        {
            _monitor.RecordLatencyMicroseconds("TestOp", i * 1000);
        }

        // Act
        var stats = _monitor.GetStats("TestOp");

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.P99LatencyMs >= 90);
        _output.WriteLine($"P99 in ms: {stats.P99LatencyMs:F2}ms");
    }

    #endregion

    #region Extension Methods Tests

    [Fact]
    public void CreateForTrading_ShouldReturnConfiguredMonitor()
    {
        // Act
        using var monitor = PerformanceMonitorExtensions.CreateForTrading();

        // Assert
        Assert.NotNull(monitor);
    }

    [Fact]
    public void CreateForDebug_ShouldReturnConfiguredMonitor()
    {
        // Act
        using var monitor = PerformanceMonitorExtensions.CreateForDebug();

        // Assert
        Assert.NotNull(monitor);
    }

    [Fact]
    public void ToDisplayString_ShouldFormatCorrectly()
    {
        // Arrange
        _monitor.RecordLatencyMicroseconds("TestOp", 5000);
        var stats = _monitor.GetStats("TestOp");

        // Act
        var displayString = stats!.ToDisplayString();

        // Assert
        Assert.Contains("TestOp", displayString);
        Assert.Contains("avg=", displayString);
        Assert.Contains("min=", displayString);
        Assert.Contains("max=", displayString);
        Assert.Contains("p99=", displayString);
        Assert.Contains("count=", displayString);
        _output.WriteLine($"Display: {displayString}");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Measure_ExceptionInAction_ShouldStillRecord()
    {
        // Arrange
        var operationName = "ExceptionOp";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            _monitor.Measure(operationName, () =>
            {
                Thread.Sleep(5);
                throw new InvalidOperationException("Test exception");
            });
        });

        // Verify measurement was still recorded
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
    }

    [Fact]
    public async Task MeasureAsync_ExceptionInTask_ShouldStillRecord()
    {
        // Arrange
        var operationName = "AsyncExceptionOp";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _monitor.MeasureAsync(operationName, async () =>
            {
                await Task.Delay(5);
                throw new InvalidOperationException("Test exception");
            });
        });

        // Verify measurement was still recorded
        var stats = _monitor.GetStats(operationName);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
    }

    [Fact]
    public void RecordLatency_ZeroTicks_ShouldRecord()
    {
        // Arrange & Act
        _monitor.RecordLatency("ZeroOp", 0);

        // Assert
        var stats = _monitor.GetStats("ZeroOp");
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        Assert.Equal(0, stats.AverageLatencyMicroseconds);
    }

    [Fact]
    public void RecordLatency_VeryLargeTicks_ShouldRecord()
    {
        // Arrange & Act
        _monitor.RecordLatencyMicroseconds("LargeOp", long.MaxValue / 2);

        // Assert
        var stats = _monitor.GetStats("LargeOp");
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
    }

    #endregion

    public void Dispose()
    {
        _monitor.Dispose();
    }
}
