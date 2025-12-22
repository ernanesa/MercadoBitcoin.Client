using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Models;
using MercadoBitcoin.Client.Internal.Helpers;
using MercadoBitcoin.Client.Errors;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        /// <summary>
        /// Asynchronously iterates over all crypto deposits of a user, paginating automatically.
        /// <para>**Requires authentication**</para>
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="symbol">Asset symbol (e.g., BTC)</param>
        /// <param name="limit">Page size (default: 50, max: 50)</param>
        /// <param name="from">Start timestamp (optional)</param>
        /// <param name="to">End timestamp (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>IAsyncEnumerable of Deposit</returns>
        public IAsyncEnumerable<Deposit> GetDepositsPagedAsync(
            string accountId,
            string symbol,
            int limit = 50,
            int? from = null,
            int? to = null,
            CancellationToken cancellationToken = default)
        {
            return AsyncPaginationHelper.PaginateAsync<Deposit>(
                fetchPage: async (pageSize, page, ct) =>
                {
                    using var lease = await _rateLimiter.AcquireAsync(1, ct);
                    if (!lease.IsAcquired)
                    {
                        throw new MercadoBitcoinRateLimitException("Rate limit exceeded (client-side).", new ErrorResponse { Code = "CLIENT_RATE_LIMIT", Message = "Rate limit exceeded (client-side)." });
                    }
                    return await _generatedClient.DepositsAsync(
                        accountId,
                        symbol,
                        limit: pageSize.ToString(),
                        page: page.ToString(),
                        from: from?.ToString(),
                        to: to?.ToString(),
                        cancellationToken: ct
                    ).ConfigureAwait(false);
                },
                pageSize: limit,
                startPage: 1,
                cancellationToken: cancellationToken
            );
        }
        #region Public Data

        public Task<AssetFee> GetAssetFeesAsync(string asset, string? network = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.FeesAsync(asset, network, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<OrderBookResponse> GetOrderBookAsync(string symbol, string? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrderbookAsync(symbol, limit, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<TradeResponse>> GetTradesAsync(string symbol, int? tid = null, int? since = null, int? from = null, int? to = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.TradesAsync(symbol, tid, since, from, to, limit, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Gets candles (OHLCV) from the public API, with input normalization and protection against inverted windows.
        /// <para>**Does not require authentication**</para>
        /// Parameters:
        /// - symbol: pair in BASE-QUOTE format (e.g., BTC-BRL, btcbrl)
        /// - resolution: timeframe (e.g., 1m, 15m, 1h, 1d)
        /// - to: unix timestamp (seconds) of the right limit (inclusive)
        /// - from: unix timestamp (seconds) of the left limit (optional)
        /// - countback: number of candles from "to" (priority over "from")
        /// </summary>
        public Task<ListCandlesResponse> GetCandlesAsync(string symbol, string resolution, int to, int? from = null, int? countback = null, CancellationToken cancellationToken = default)
        {
            if (to <= 0)
                throw new ArgumentException("Parameter 'to' must be a valid unix timestamp (seconds).", nameof(to));

            // Normalizes inputs
            var normalizedSymbol = CandleExtensions.NormalizeSymbol(symbol);
            var normalizedResolution = CandleExtensions.NormalizeResolution(resolution);

            // If 'from' is provided and greater than 'to', swap to avoid inverted window
            if (from.HasValue && from.Value > to)
            {
                var tmp = to;
                to = from.Value;
                from = tmp;
            }

            try
            {
                return _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Convenience: fetches the last N candles until now (uses countback and ignores "from").
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ListCandlesResponse> GetRecentCandlesAsync(string symbol, string resolution, int countback, int? to = null, CancellationToken cancellationToken = default)
        {
            if (countback <= 0) throw new ArgumentOutOfRangeException(nameof(countback));
            var right = to ?? (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var normalizedSymbol = CandleExtensions.NormalizeSymbol(symbol);
            var normalizedResolution = CandleExtensions.NormalizeResolution(resolution);
            try
            {
                return _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from: null, to: right, countback: countback, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Gets candles with validation and input normalization, returning a typed list of CandleData.
        /// <para>**Does not require authentication**</para>
        /// </summary>
        /// <param name="symbol">Pair symbol (e.g., BTC-BRL or btcbrl)</param>
        /// <param name="resolution">Resolution/timeframe (e.g., 1m, 15m, 1h, 1d)</param>
        /// <param name="to">Unix timestamp (seconds) of the right limit (inclusive)</param>
        /// <param name="from">Unix timestamp (seconds) of the left limit (optional)</param>
        /// <param name="countback">Number of candles from "to" (priority over "from")</param>
        /// <returns>List of candles mapped to CandleData</returns>
        public async Task<IReadOnlyList<CandleData>> GetCandlesTypedAsync(string symbol, string resolution, int to, int? from = null, int? countback = null, CancellationToken cancellationToken = default)
        {
            if (!CandleExtensions.IsValidResolution(resolution))
                throw new ArgumentException($"Invalid resolution: {resolution}", nameof(resolution));

            var normalizedSymbol = CandleExtensions.NormalizeSymbol(symbol);
            var normalizedResolution = CandleExtensions.NormalizeResolution(resolution);

            // Protects against inverted window
            if (from.HasValue && from.Value > to)
            {
                var tmp = to;
                to = from.Value;
                from = tmp;
            }

            try
            {
                // Calls the generated client with normalized parameters
                var response = await _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback, cancellationToken).ConfigureAwait(false);
                // Converts response to typed list
                var candles = response.ToCandleDataList(normalizedSymbol, normalizedResolution);
                return candles;
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Practical overload: fetches last N candles until now (uses countback).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<IReadOnlyList<CandleData>> GetRecentCandlesTypedAsync(string symbol, string resolution, int countback, CancellationToken cancellationToken = default)
        {
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                return GetCandlesTypedAsync(symbol, resolution, to, from: null, countback: countback, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Gets the list of tradable symbols available in the public API.
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ListSymbolInfoResponse> GetSymbolsAsync(string? symbols = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.SymbolsAsync(symbols, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Gets the list of tradable symbols available in the public API (Convenience overload).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ListSymbolInfoResponse> GetSymbolsAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
        {
            var joined = string.Join(",", symbols);
            return GetSymbolsAsync(joined, cancellationToken);
        }

        /// <summary>
        /// Gets the current tickers for one or more symbols.
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ICollection<TickerResponse>> GetTickersAsync(string symbols, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.TickersAsync(symbols, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Gets the current tickers for all symbols.
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ICollection<TickerResponse>> GetTickersAsync(CancellationToken cancellationToken = default)
        {
            return GetTickersAsync(symbols: (IEnumerable<string>?)null, cancellationToken);
        }

        /// <summary>
        /// Gets the current tickers for one or more symbols (Convenience overload).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public async Task<ICollection<TickerResponse>> GetTickersAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
        {
            if (symbols is null)
            {
                var allSymbols = await GetAllSymbolsAsync(cancellationToken).ConfigureAwait(false);
                return await GetTickersBatchedAsync(allSymbols, cancellationToken).ConfigureAwait(false);
            }

            var normalized = symbols
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalized.Length == 0)
            {
                var allSymbols = await GetAllSymbolsAsync(cancellationToken).ConfigureAwait(false);
                return await GetTickersBatchedAsync(allSymbols, cancellationToken).ConfigureAwait(false);
            }

            return await GetTickersBatchedAsync(normalized, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken cancellationToken)
        {
            var response = await GetSymbolsAsync((string?)null, cancellationToken).ConfigureAwait(false);
            if (response?.Symbol is null || response.Symbol.Count == 0)
            {
                return Array.Empty<string>();
            }

            return response.Symbol
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<ICollection<TickerResponse>> GetTickersBatchedAsync(
            IReadOnlyCollection<string> symbols,
            CancellationToken cancellationToken)
        {
            if (symbols.Count == 0)
            {
                return Array.Empty<TickerResponse>();
            }

            List<TickerResponse> allTickers = [];
            const int tickerBatchSize = 100;

            foreach (var batch in symbols.Chunk(tickerBatchSize))
            {
                var joined = string.Join(",", batch);
                var result = await GetTickersAsync(joined, cancellationToken).ConfigureAwait(false);
                if (result is not null)
                {
                    allTickers.AddRange(result);
                }
            }

            return allTickers;
        }

        /// <summary>
        /// [BEAST MODE] Gets tickers in parallel batches for maximum performance.
        /// Automatically chunks large symbol lists to avoid URL length limits.
        /// </summary>
        public async Task<IReadOnlyList<TickerResponse>> GetTickersBatchAsync(IEnumerable<string> symbols, int batchSize = 50, CancellationToken cancellationToken = default)
        {
            var symbolList = symbols.ToList();
            if (symbolList.Count == 0) return Array.Empty<TickerResponse>();

            if (symbolList.Count <= batchSize)
            {
                var result = await GetTickersAsync(symbolList, cancellationToken).ConfigureAwait(false);
                return result.ToList();
            }

            var tasks = new List<Task<ICollection<TickerResponse>>>();
            for (int i = 0; i < symbolList.Count; i += batchSize)
            {
                var batch = symbolList.Skip(i).Take(batchSize);
                tasks.Add(GetTickersAsync(batch, cancellationToken));
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.SelectMany(r => r).ToList();
        }

        /// <summary>
        /// Gets the available networks for an asset (e.g., USDC, BTC).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ICollection<Network>> GetAssetNetworksAsync(string asset, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.NetworksAsync(asset, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        #endregion
    }
}
