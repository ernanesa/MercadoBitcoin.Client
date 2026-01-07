using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Trading;

/// <summary>
/// High-precision performance monitoring utility for trading operations.
/// Provides microsecond-level latency measurements and statistical analysis.
/// </summary>
public sealed class PerformanceMonitor : IDisposable
{
    private readonly ILogger<PerformanceMonitor>? _logger;
    private readonly PerformanceMonitorOptions _options;
    private readonly ConcurrentDictionary<string, OperationMetrics> _metrics = new();
    private readonly Timer? _reportingTimer;
    private bool _disposed;

    /// <summary>
    /// Event raised when a latency threshold is exceeded.
    /// </summary>
    public event EventHandler<LatencyThresholdExceededEventArgs>? LatencyThresholdExceeded;

    /// <summary>
    /// Event raised when periodic report is generated.
    /// </summary>
    public event EventHandler<PerformanceReportEventArgs>? ReportGenerated;

    /// <summary>
    /// Creates a new instance of PerformanceMonitor.
    /// </summary>
    /// <param name="options">Monitor options.</param>
    /// <param name="logger">Optional logger.</param>
    public PerformanceMonitor(
        PerformanceMonitorOptions? options = null,
        ILogger<PerformanceMonitor>? logger = null)
    {
        _options = options ?? new PerformanceMonitorOptions();
        _logger = logger;

        if (_options.EnablePeriodicReporting)
        {
            _reportingTimer = new Timer(
                OnReportingTimerCallback,
                null,
                _options.ReportingInterval,
                _options.ReportingInterval);
        }
    }

    #region Measurement Methods

    /// <summary>
    /// Measures the execution time of an async operation.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation name.</param>
    /// <param name="action">The async action to measure.</param>
    /// <returns>The result of the action.</returns>
    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await action();
        }
        finally
        {
            sw.Stop();
            RecordLatency(operation, sw.ElapsedTicks);
        }
    }

    /// <summary>
    /// Measures the execution time of an async operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="action">The async action to measure.</param>
    public async Task MeasureAsync(string operation, Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await action();
        }
        finally
        {
            sw.Stop();
            RecordLatency(operation, sw.ElapsedTicks);
        }
    }

    /// <summary>
    /// Measures the execution time of a synchronous operation.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation name.</param>
    /// <param name="action">The action to measure.</param>
    /// <returns>The result of the action.</returns>
    public T Measure<T>(string operation, Func<T> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return action();
        }
        finally
        {
            sw.Stop();
            RecordLatency(operation, sw.ElapsedTicks);
        }
    }

    /// <summary>
    /// Measures the execution time of a synchronous operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="action">The action to measure.</param>
    public void Measure(string operation, Action action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            action();
        }
        finally
        {
            sw.Stop();
            RecordLatency(operation, sw.ElapsedTicks);
        }
    }

    /// <summary>
    /// Starts a measurement scope that records on disposal.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>A disposable measurement scope.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MeasurementScope StartMeasurement(string operation)
    {
        return new MeasurementScope(this, operation);
    }

    /// <summary>
    /// Records a latency measurement directly.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="elapsedTicks">Stopwatch ticks elapsed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordLatency(string operation, long elapsedTicks)
    {
        var metrics = _metrics.GetOrAdd(operation, _ => new OperationMetrics());
        metrics.Record(elapsedTicks);

        // Check threshold
        var microseconds = TicksToMicroseconds(elapsedTicks);
        if (microseconds > _options.LatencyThresholdMicroseconds)
        {
            OnLatencyThresholdExceeded(new LatencyThresholdExceededEventArgs
            {
                Operation = operation,
                LatencyMicroseconds = microseconds,
                ThresholdMicroseconds = _options.LatencyThresholdMicroseconds,
                Timestamp = DateTime.UtcNow
            });
        }

        if (_options.LogEveryMeasurement && _logger != null)
        {
            _logger.LogDebug(
                "Operation {Operation} completed in {Latency}µs",
                operation, microseconds);
        }
    }

    /// <summary>
    /// Records a latency measurement in microseconds.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="microseconds">Latency in microseconds.</param>
    public void RecordLatencyMicroseconds(string operation, long microseconds)
    {
        var ticks = MicrosecondsToTicks(microseconds);
        RecordLatency(operation, ticks);
    }

    /// <summary>
    /// Records a latency measurement in milliseconds.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="milliseconds">Latency in milliseconds.</param>
    public void RecordLatencyMilliseconds(string operation, double milliseconds)
    {
        var ticks = (long)(milliseconds * Stopwatch.Frequency / 1000.0);
        RecordLatency(operation, ticks);
    }

    #endregion

    #region Statistics Methods

    /// <summary>
    /// Gets the metrics for a specific operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>Operation statistics or null if not found.</returns>
    public OperationStats? GetStats(string operation)
    {
        if (!_metrics.TryGetValue(operation, out var metrics))
        {
            return null;
        }

        return metrics.GetStats(operation);
    }

    /// <summary>
    /// Gets statistics for all operations.
    /// </summary>
    /// <returns>Dictionary of operation statistics.</returns>
    public IReadOnlyDictionary<string, OperationStats> GetAllStats()
    {
        var result = new Dictionary<string, OperationStats>();
        foreach (var (operation, metrics) in _metrics)
        {
            result[operation] = metrics.GetStats(operation);
        }
        return result;
    }

    /// <summary>
    /// Gets a performance report.
    /// </summary>
    /// <returns>Performance report.</returns>
    public PerformanceReport GetReport()
    {
        var allStats = GetAllStats();
        var sortedByLatency = allStats.OrderByDescending(kv => kv.Value.AverageLatencyMicroseconds).ToList();

        return new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalOperations = allStats.Values.Sum(s => s.Count),
            OperationStats = allStats,
            SlowestOperations = sortedByLatency.Take(5).Select(kv => kv.Key).ToList(),
            FastestOperations = sortedByLatency.TakeLast(5).Reverse().Select(kv => kv.Key).ToList()
        };
    }

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void Reset()
    {
        _metrics.Clear();
        _logger?.LogInformation("Performance metrics reset");
    }

    /// <summary>
    /// Resets metrics for a specific operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    public void Reset(string operation)
    {
        _metrics.TryRemove(operation, out _);
    }

    #endregion

    #region Helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long TicksToMicroseconds(long ticks)
    {
        return (long)(ticks * 1_000_000.0 / Stopwatch.Frequency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long MicrosecondsToTicks(long microseconds)
    {
        return (long)(microseconds * Stopwatch.Frequency / 1_000_000.0);
    }

    private void OnReportingTimerCallback(object? state)
    {
        if (_disposed || _metrics.IsEmpty) return;

        var report = GetReport();
        OnReportGenerated(new PerformanceReportEventArgs { Report = report });

        if (_logger != null && _options.LogPeriodicReports)
        {
            _logger.LogInformation(
                "Performance Report: {TotalOps} operations across {UniqueOps} types",
                report.TotalOperations, report.OperationStats.Count);

            foreach (var (operation, stats) in report.OperationStats)
            {
                _logger.LogInformation(
                    "  {Operation}: avg={Avg}µs, min={Min}µs, max={Max}µs, p99={P99}µs, count={Count}",
                    operation,
                    stats.AverageLatencyMicroseconds,
                    stats.MinLatencyMicroseconds,
                    stats.MaxLatencyMicroseconds,
                    stats.P99LatencyMicroseconds,
                    stats.Count);
            }
        }
    }

    private void OnLatencyThresholdExceeded(LatencyThresholdExceededEventArgs args)
    {
        _logger?.LogWarning(
            "Latency threshold exceeded for {Operation}: {Latency}µs > {Threshold}µs",
            args.Operation, args.LatencyMicroseconds, args.ThresholdMicroseconds);

        LatencyThresholdExceeded?.Invoke(this, args);
    }

    private void OnReportGenerated(PerformanceReportEventArgs args)
    {
        ReportGenerated?.Invoke(this, args);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _reportingTimer?.Dispose();
        _metrics.Clear();
    }

    #endregion
}

#region Internal Classes

/// <summary>
/// Thread-safe metrics accumulator for a single operation.
/// </summary>
internal sealed class OperationMetrics
{
    private long _count;
    private long _totalTicks;
    private long _minTicks = long.MaxValue;
    private long _maxTicks;
    private readonly ConcurrentQueue<long> _recentSamples = new();
    private const int MaxSamples = 1000;

    public void Record(long ticks)
    {
        Interlocked.Increment(ref _count);
        Interlocked.Add(ref _totalTicks, ticks);

        // Update min/max using compare-exchange
        long currentMin;
        do
        {
            currentMin = Volatile.Read(ref _minTicks);
            if (ticks >= currentMin) break;
        } while (Interlocked.CompareExchange(ref _minTicks, ticks, currentMin) != currentMin);

        long currentMax;
        do
        {
            currentMax = Volatile.Read(ref _maxTicks);
            if (ticks <= currentMax) break;
        } while (Interlocked.CompareExchange(ref _maxTicks, ticks, currentMax) != currentMax);

        // Keep recent samples for percentile calculation
        _recentSamples.Enqueue(ticks);
        while (_recentSamples.Count > MaxSamples)
        {
            _recentSamples.TryDequeue(out _);
        }
    }

    public OperationStats GetStats(string operation)
    {
        var count = Volatile.Read(ref _count);
        var totalTicks = Volatile.Read(ref _totalTicks);
        var minTicks = Volatile.Read(ref _minTicks);
        var maxTicks = Volatile.Read(ref _maxTicks);

        if (count == 0)
        {
            return new OperationStats
            {
                Operation = operation,
                Count = 0,
                AverageLatencyMicroseconds = 0,
                MinLatencyMicroseconds = 0,
                MaxLatencyMicroseconds = 0,
                P50LatencyMicroseconds = 0,
                P95LatencyMicroseconds = 0,
                P99LatencyMicroseconds = 0
            };
        }

        var avgMicros = TicksToMicroseconds(totalTicks / count);
        var minMicros = TicksToMicroseconds(minTicks);
        var maxMicros = TicksToMicroseconds(maxTicks);

        // Calculate percentiles from recent samples
        var samples = _recentSamples.ToArray();
        Array.Sort(samples);

        var p50 = samples.Length > 0 ? TicksToMicroseconds(samples[(int)(samples.Length * 0.50)]) : 0;
        var p95 = samples.Length > 0 ? TicksToMicroseconds(samples[(int)(samples.Length * 0.95)]) : 0;
        var p99 = samples.Length > 0 ? TicksToMicroseconds(samples[(int)(samples.Length * 0.99)]) : 0;

        return new OperationStats
        {
            Operation = operation,
            Count = count,
            AverageLatencyMicroseconds = avgMicros,
            MinLatencyMicroseconds = minMicros,
            MaxLatencyMicroseconds = maxMicros,
            P50LatencyMicroseconds = p50,
            P95LatencyMicroseconds = p95,
            P99LatencyMicroseconds = p99
        };
    }

    private static long TicksToMicroseconds(long ticks)
    {
        return (long)(ticks * 1_000_000.0 / Stopwatch.Frequency);
    }
}

#endregion

#region Options and Models

/// <summary>
/// Configuration options for PerformanceMonitor.
/// </summary>
public sealed class PerformanceMonitorOptions
{
    /// <summary>
    /// Latency threshold in microseconds for triggering alerts. Default: 10000 (10ms).
    /// </summary>
    public long LatencyThresholdMicroseconds { get; set; } = 10_000;

    /// <summary>
    /// Whether to enable periodic reporting. Default: false.
    /// </summary>
    public bool EnablePeriodicReporting { get; set; } = false;

    /// <summary>
    /// Interval between periodic reports. Default: 1 minute.
    /// </summary>
    public TimeSpan ReportingInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Whether to log each measurement. Default: false (too verbose).
    /// </summary>
    public bool LogEveryMeasurement { get; set; } = false;

    /// <summary>
    /// Whether to log periodic reports. Default: true.
    /// </summary>
    public bool LogPeriodicReports { get; set; } = true;
}

/// <summary>
/// Statistics for a single operation type.
/// </summary>
public sealed class OperationStats
{
    /// <summary>
    /// The operation name.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Number of measurements.
    /// </summary>
    public long Count { get; init; }

    /// <summary>
    /// Average latency in microseconds.
    /// </summary>
    public long AverageLatencyMicroseconds { get; init; }

    /// <summary>
    /// Minimum latency in microseconds.
    /// </summary>
    public long MinLatencyMicroseconds { get; init; }

    /// <summary>
    /// Maximum latency in microseconds.
    /// </summary>
    public long MaxLatencyMicroseconds { get; init; }

    /// <summary>
    /// 50th percentile (median) latency in microseconds.
    /// </summary>
    public long P50LatencyMicroseconds { get; init; }

    /// <summary>
    /// 95th percentile latency in microseconds.
    /// </summary>
    public long P95LatencyMicroseconds { get; init; }

    /// <summary>
    /// 99th percentile latency in microseconds.
    /// </summary>
    public long P99LatencyMicroseconds { get; init; }

    /// <summary>
    /// Average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs => AverageLatencyMicroseconds / 1000.0;

    /// <summary>
    /// P99 latency in milliseconds.
    /// </summary>
    public double P99LatencyMs => P99LatencyMicroseconds / 1000.0;
}

/// <summary>
/// Performance report containing all operation statistics.
/// </summary>
public sealed class PerformanceReport
{
    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; }

    /// <summary>
    /// Total number of operations measured.
    /// </summary>
    public long TotalOperations { get; init; }

    /// <summary>
    /// Statistics per operation.
    /// </summary>
    public required IReadOnlyDictionary<string, OperationStats> OperationStats { get; init; }

    /// <summary>
    /// Top 5 slowest operations by average latency.
    /// </summary>
    public required IReadOnlyList<string> SlowestOperations { get; init; }

    /// <summary>
    /// Top 5 fastest operations by average latency.
    /// </summary>
    public required IReadOnlyList<string> FastestOperations { get; init; }
}

#endregion

#region Event Arguments

/// <summary>
/// Event arguments for latency threshold exceeded events.
/// </summary>
public sealed class LatencyThresholdExceededEventArgs : EventArgs
{
    /// <summary>
    /// The operation that exceeded the threshold.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// The actual latency in microseconds.
    /// </summary>
    public long LatencyMicroseconds { get; init; }

    /// <summary>
    /// The configured threshold in microseconds.
    /// </summary>
    public long ThresholdMicroseconds { get; init; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Event arguments for periodic report generation.
/// </summary>
public sealed class PerformanceReportEventArgs : EventArgs
{
    /// <summary>
    /// The generated report.
    /// </summary>
    public required PerformanceReport Report { get; init; }
}

#endregion

#region Measurement Scope

/// <summary>
/// Disposable scope for measuring operation latency.
/// </summary>
public readonly struct MeasurementScope : IDisposable
{
    private readonly PerformanceMonitor _monitor;
    private readonly string _operation;
    private readonly long _startTicks;

    internal MeasurementScope(PerformanceMonitor monitor, string operation)
    {
        _monitor = monitor;
        _operation = operation;
        _startTicks = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Completes the measurement and records the latency.
    /// </summary>
    public void Dispose()
    {
        var elapsed = Stopwatch.GetTimestamp() - _startTicks;
        _monitor.RecordLatency(_operation, elapsed);
    }
}

#endregion

#region Extensions

/// <summary>
/// Extension methods for PerformanceMonitor.
/// </summary>
public static class PerformanceMonitorExtensions
{
    /// <summary>
    /// Creates a pre-configured monitor for trading operations.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Configured performance monitor.</returns>
    public static PerformanceMonitor CreateForTrading(ILogger<PerformanceMonitor>? logger = null)
    {
        return new PerformanceMonitor(new PerformanceMonitorOptions
        {
            LatencyThresholdMicroseconds = 5_000, // 5ms
            EnablePeriodicReporting = true,
            ReportingInterval = TimeSpan.FromSeconds(30),
            LogEveryMeasurement = false,
            LogPeriodicReports = true
        }, logger);
    }

    /// <summary>
    /// Creates a monitor for debugging with verbose logging.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Configured performance monitor.</returns>
    public static PerformanceMonitor CreateForDebug(ILogger<PerformanceMonitor>? logger = null)
    {
        return new PerformanceMonitor(new PerformanceMonitorOptions
        {
            LatencyThresholdMicroseconds = 1_000, // 1ms
            EnablePeriodicReporting = true,
            ReportingInterval = TimeSpan.FromSeconds(10),
            LogEveryMeasurement = true,
            LogPeriodicReports = true
        }, logger);
    }

    /// <summary>
    /// Formats operation statistics as a human-readable string.
    /// </summary>
    /// <param name="stats">The statistics to format.</param>
    /// <returns>Formatted string.</returns>
    public static string ToDisplayString(this OperationStats stats)
    {
        return $"{stats.Operation}: avg={stats.AverageLatencyMicroseconds}µs, " +
               $"min={stats.MinLatencyMicroseconds}µs, max={stats.MaxLatencyMicroseconds}µs, " +
               $"p99={stats.P99LatencyMicroseconds}µs, count={stats.Count}";
    }
}

#endregion
