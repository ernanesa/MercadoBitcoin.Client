using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MercadoBitcoin.Client.Internal.Diagnostics
{
    /// <summary>
    /// Metrics collection and observability manager for performance monitoring.
    /// Tracks request latencies, throughput, error rates, and system health.
    /// </summary>
    public sealed class MetricsCollector
    {
        /// <summary>
        /// Represents operation metrics snapshot.
        /// </summary>
        public sealed class OperationMetrics
        {
            public string Name { get; set; } = string.Empty;
            public long RequestCount { get; set; }
            public long ErrorCount { get; set; }
            public long TotalLatencyMs { get; set; }
            public long MinLatencyMs { get; set; }
            public long MaxLatencyMs { get; set; }
            public double AverageLatencyMs => RequestCount > 0 ? (double)TotalLatencyMs / RequestCount : 0;
            public double ErrorRate => RequestCount > 0 ? (double)ErrorCount / RequestCount : 0;
            public long LastUpdatedUtc { get; set; }
        }

        private readonly Dictionary<string, OperationMetrics> _metrics = new();
        private readonly object _metricsLock = new();
        private readonly Stopwatch _uptime = Stopwatch.StartNew();

        /// <summary>
        /// Records a successful operation.
        /// </summary>
        public void RecordSuccess(string operationName, long latencyMs)
        {
            lock (_metricsLock)
            {
                if (!_metrics.TryGetValue(operationName, out var metrics))
                {
                    metrics = new OperationMetrics { Name = operationName, MinLatencyMs = long.MaxValue };
                    _metrics[operationName] = metrics;
                }

                metrics.RequestCount++;
                metrics.TotalLatencyMs += latencyMs;
                metrics.MinLatencyMs = Math.Min(metrics.MinLatencyMs, latencyMs);
                metrics.MaxLatencyMs = Math.Max(metrics.MaxLatencyMs, latencyMs);
                metrics.LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// Records a failed operation.
        /// </summary>
        public void RecordError(string operationName, long latencyMs)
        {
            lock (_metricsLock)
            {
                if (!_metrics.TryGetValue(operationName, out var metrics))
                {
                    metrics = new OperationMetrics { Name = operationName, MinLatencyMs = long.MaxValue };
                    _metrics[operationName] = metrics;
                }

                metrics.RequestCount++;
                metrics.ErrorCount++;
                metrics.TotalLatencyMs += latencyMs;
                metrics.MinLatencyMs = Math.Min(metrics.MinLatencyMs, latencyMs);
                metrics.MaxLatencyMs = Math.Max(metrics.MaxLatencyMs, latencyMs);
                metrics.LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// Gets metrics snapshot for a specific operation.
        /// </summary>
        public OperationMetrics? GetMetrics(string operationName)
        {
            lock (_metricsLock)
            {
                return _metrics.TryGetValue(operationName, out var metrics)
                    ? new OperationMetrics
                    {
                        Name = metrics.Name,
                        RequestCount = metrics.RequestCount,
                        ErrorCount = metrics.ErrorCount,
                        TotalLatencyMs = metrics.TotalLatencyMs,
                        MinLatencyMs = metrics.MinLatencyMs,
                        MaxLatencyMs = metrics.MaxLatencyMs,
                        LastUpdatedUtc = metrics.LastUpdatedUtc
                    }
                    : null;
            }
        }

        /// <summary>
        /// Gets all operation metrics.
        /// </summary>
        public IReadOnlyList<OperationMetrics> GetAllMetrics()
        {
            lock (_metricsLock)
            {
                var snapshot = new List<OperationMetrics>();
                foreach (var kvp in _metrics)
                {
                    snapshot.Add(new OperationMetrics
                    {
                        Name = kvp.Value.Name,
                        RequestCount = kvp.Value.RequestCount,
                        ErrorCount = kvp.Value.ErrorCount,
                        TotalLatencyMs = kvp.Value.TotalLatencyMs,
                        MinLatencyMs = kvp.Value.MinLatencyMs,
                        MaxLatencyMs = kvp.Value.MaxLatencyMs,
                        LastUpdatedUtc = kvp.Value.LastUpdatedUtc
                    });
                }
                return snapshot;
            }
        }

        /// <summary>
        /// Gets system uptime.
        /// </summary>
        public TimeSpan Uptime => _uptime.Elapsed;

        /// <summary>
        /// Gets total request count across all operations.
        /// </summary>
        public long TotalRequestCount
        {
            get
            {
                lock (_metricsLock)
                {
                    long total = 0;
                    foreach (var kvp in _metrics)
                    {
                        total += kvp.Value.RequestCount;
                    }
                    return total;
                }
            }
        }

        /// <summary>
        /// Gets total error count across all operations.
        /// </summary>
        public long TotalErrorCount
        {
            get
            {
                lock (_metricsLock)
                {
                    long total = 0;
                    foreach (var kvp in _metrics)
                    {
                        total += kvp.Value.ErrorCount;
                    }
                    return total;
                }
            }
        }

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        public void Reset()
        {
            lock (_metricsLock)
            {
                _metrics.Clear();
            }
        }

        /// <summary>
        /// Gets a health summary.
        /// </summary>
        public string GetHealthSummary()
        {
            lock (_metricsLock)
            {
                var totalRequests = TotalRequestCount;
                var totalErrors = TotalErrorCount;
                var errorRate = totalRequests > 0 ? (double)totalErrors / totalRequests * 100 : 0;

                return $"Health: Uptime={Uptime:hh\\:mm\\:ss}, " +
                       $"Requests={totalRequests}, " +
                       $"Errors={totalErrors}, " +
                       $"ErrorRate={errorRate:F2}%, " +
                       $"Operations={_metrics.Count}";
            }
        }
    }
}
