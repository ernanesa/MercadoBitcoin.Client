using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Internal.Time
{
    /// <summary>
    /// Responsible for estimating and correcting time drift between the client and the Mercado Bitcoin server.
    /// </summary>
    public class ServerTimeEstimator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger? _logger;
        private TimeSpan _timeOffset = TimeSpan.Zero;

        private readonly object _lock = new();

        public ServerTimeEstimator(HttpClient httpClient, ILogger? logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
        }

        /// <summary>
        /// Gets the current corrected time (estimated server time).
        /// </summary>
        public DateTimeOffset GetCorrectedTime()
        {
            return DateTimeOffset.UtcNow.Add(_timeOffset);
        }

        /// <summary>
        /// Gets the current corrected Unix timestamp in seconds.
        /// </summary>
        public long GetCorrectedUnixTimeSeconds()
        {
            return GetCorrectedTime().ToUnixTimeSeconds();
        }

        /// <summary>
        /// Synchronizes the local clock with the Mercado Bitcoin server.
        /// Makes a lightweight request to compute latency and the 'Date' header delta.
        /// </summary>
        public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Use a lightweight public endpoint to 'ping' the server
                // The HTTP 'Date' header is standard and reflects the server time
                var sw = Stopwatch.StartNew();
                var request = new HttpRequestMessage(HttpMethod.Head, "symbols");

                // Avoid caching to ensure a real timestamp
                request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                sw.Stop();

                if (response.Headers.Date.HasValue)
                {
                    var serverDate = response.Headers.Date.Value;
                    var localNow = DateTimeOffset.UtcNow;

                    // The HTTP 'Date' header has second precision. Add half of RTT (round-trip time) to adjust for latency.
                    var latencyAdjustment = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds / 2);
                    var estimatedServerTime = serverDate.Add(latencyAdjustment);

                    lock (_lock)
                    {
                        _timeOffset = estimatedServerTime - localNow;
                    }

                    _logger?.LogInformation("Clock synchronized. Offset: {Offset}ms. Latency: {Latency}ms",
                        _timeOffset.TotalMilliseconds, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to synchronize time with server. Using local time.");
                // On failure, keep the previous offset or zero (safe fallback)
            }
        }
    }
}
