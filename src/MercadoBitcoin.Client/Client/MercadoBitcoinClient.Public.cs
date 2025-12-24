using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Internal.Helpers;
using MercadoBitcoin.Client.Models;

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
            var cacheKey = $"orderbook:{symbol}:{limit ?? "default"}";
            return ExecuteCachedAsync(cacheKey, ct =>
            {
                try
                {
                    return _generatedClient.OrderbookAsync(symbol, limit, ct);
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets order books for multiple symbols in parallel (Universal Filter).
        /// </summary>
        public async Task<ICollection<OrderBookResponse>> GetOrderBooksAsync(IEnumerable<string>? symbols = null, string? limit = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            return (await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => GetOrderBookAsync(symbol, limit, ct),
                cancellationToken).ConfigureAwait(false)).ToList();
        }

        /// <summary>
        /// Gets order books using a universal filter.
        /// </summary>
        public Task<ICollection<OrderBookResponse>> GetOrderBooksAsync(UniversalFilter filter, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            return GetOrderBooksAsync(filter.Symbols, filter.Limit?.ToString(), maxDegreeOfParallelism, cancellationToken);
        }

        public Task<ICollection<TradeResponse>> GetTradesAsync(string symbol, int? tid = null, int? since = null, int? from = null, int? to = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"trades:{symbol}:{tid}:{since}:{from}:{to}:{limit}";
            return ExecuteCachedAsync(cacheKey, ct =>
            {
                try
                {
                    return _generatedClient.TradesAsync(symbol, tid, since, from, to, limit, ct);
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets trades for multiple symbols in parallel (Universal Filter).
        /// </summary>
        public async Task<ICollection<TradeResponse>> GetTradesAsync(IEnumerable<string>? symbols = null, int? limit = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            var results = await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => GetTradesAsync(symbol, limit: limit, cancellationToken: ct),
                cancellationToken).ConfigureAwait(false);

            return results.SelectMany(r => r).ToList();
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

            var cacheKey = $"candles:{normalizedSymbol}:{normalizedResolution}:{to}:{from}:{countback}";
            return ExecuteCachedAsync(cacheKey, ct =>
            {
                try
                {
                    return _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback, ct);
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets candles for multiple symbols in parallel (Universal Filter).
        /// </summary>
        public async Task<ICollection<ListCandlesResponse>> GetCandlesAsync(IEnumerable<string>? symbols = null, string resolution = "1h", int? to = null, int? from = null, int? countback = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            var right = to ?? (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => GetCandlesAsync(symbol, resolution, right, from, countback, ct),
                cancellationToken).ConfigureAwait(false)).ToList();
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

            var cacheKey = $"candles_typed:{normalizedSymbol}:{normalizedResolution}:{to}:{from}:{countback}";
            return await ExecuteCachedAsync(cacheKey, async ct =>
            {
                try
                {
                    // Calls the generated client with normalized parameters
                    var response = await _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback, ct).ConfigureAwait(false);
                    // Converts response to typed list
                    var candles = response.ToCandleDataList(normalizedSymbol, normalizedResolution);
                    return candles;
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets typed candles for multiple symbols in parallel (Universal Filter).
        /// </summary>
        public async Task<IReadOnlyList<CandleData>> GetCandlesTypedAsync(IEnumerable<string>? symbols = null, string resolution = "1h", int? to = null, int? from = null, int? countback = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            var right = to ?? (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var results = await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => GetCandlesTypedAsync(symbol, resolution, right, from, countback, ct),
                cancellationToken).ConfigureAwait(false);

            return results.SelectMany(r => r).ToList();
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
        /// Gets the list of tradable symbols available in the public API (Raw string parameter).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ListSymbolInfoResponse> GetSymbolsRawAsync(string? symbols = null, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"symbols:{symbols ?? "all"}";
            return ExecuteCachedAsync(cacheKey, ct =>
            {
                try
                {
                    return _generatedClient.SymbolsAsync(symbols, ct);
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets the list of tradable symbols available in the public API (string overload for backward compatibility).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ListSymbolInfoResponse> GetSymbolsAsync(string? symbols, CancellationToken cancellationToken = default)
        {
            return GetSymbolsAsync(symbols: (IEnumerable<string>?)null, cancellationToken);
        }

        /// <summary>
        /// Gets the list of tradable symbols available in the public API (Universal Filter).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public async Task<ListSymbolInfoResponse> GetSymbolsAsync(IEnumerable<string>? symbols = null, CancellationToken cancellationToken = default)
        {
            if (symbols is null || !symbols.Any())
            {
                return await GetSymbolsRawAsync(null, cancellationToken).ConfigureAwait(false);
            }

            var results = await BatchHelper.ExecuteNativeBatchAsync<ListSymbolInfoResponse>(
                symbols,
                100,
                GetAllSymbolsAsync,
                async (batch, ct) => new[] { await GetSymbolsRawAsync(batch, ct).ConfigureAwait(false) },
                cancellationToken).ConfigureAwait(false);

            var finalResponse = new ListSymbolInfoResponse
            {
                Symbol = new List<string>(),
                Description = new List<string>(),
                Currency = new List<string>(),
                BaseCurrency = new List<string>(),
                ExchangeListed = new List<bool>(),
                ExchangeTraded = new List<bool>(),
                Minmovement = new List<string>(),
                Pricescale = new List<double>(),
                Type = new List<string>(),
                SessionRegular = new List<string>(),
                WithdrawalFee = new List<string>()
            };

            foreach (var res in results)
            {
                if (res.Symbol != null) ((List<string>)finalResponse.Symbol).AddRange(res.Symbol);
                if (res.Description != null) ((List<string>)finalResponse.Description).AddRange(res.Description);
                if (res.Currency != null) ((List<string>)finalResponse.Currency).AddRange(res.Currency);
                if (res.BaseCurrency != null) ((List<string>)finalResponse.BaseCurrency).AddRange(res.BaseCurrency);
                if (res.ExchangeListed != null) ((List<bool>)finalResponse.ExchangeListed).AddRange(res.ExchangeListed);
                if (res.ExchangeTraded != null) ((List<bool>)finalResponse.ExchangeTraded).AddRange(res.ExchangeTraded);
                if (res.Minmovement != null) ((List<string>)finalResponse.Minmovement).AddRange(res.Minmovement);
                if (res.Pricescale != null) ((List<double>)finalResponse.Pricescale).AddRange(res.Pricescale);
                if (res.Type != null) ((List<string>)finalResponse.Type).AddRange(res.Type);
                if (res.SessionRegular != null) ((List<string>)finalResponse.SessionRegular).AddRange(res.SessionRegular);
                if (res.WithdrawalFee != null) ((List<string>)finalResponse.WithdrawalFee).AddRange(res.WithdrawalFee);
            }

            return finalResponse;
        }

        /// <summary>
        /// Gets the current tickers for one or more symbols (Raw string parameter).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ICollection<TickerResponse>> GetTickersRawAsync(string? symbols, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"tickers:{symbols ?? "all"}";
            return ExecuteCachedAsync(cacheKey, ct =>
            {
                try
                {
                    return _generatedClient.TickersAsync(symbols, ct);
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets the current tickers for one or more symbols (string overload for backward compatibility).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public Task<ICollection<TickerResponse>> GetTickersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return GetTickersAsync(new[] { symbol }, cancellationToken);
        }

        /// <summary>
        /// Gets the current tickers for all symbols (Universal Filter).
        /// <para>**Does not require authentication**</para>
        /// </summary>
        public async Task<ICollection<TickerResponse>> GetTickersAsync(IEnumerable<string>? symbols = null, CancellationToken cancellationToken = default)
        {
            return (await BatchHelper.ExecuteNativeBatchAsync<TickerResponse>(
                symbols,
                100,
                GetAllSymbolsAsync,
                async (batch, ct) => await GetTickersRawAsync(batch, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false)).ToList();
        }

        /// <summary>
        /// Gets tickers using a universal filter.
        /// </summary>
        public Task<ICollection<TickerResponse>> GetTickersAsync(UniversalFilter filter, CancellationToken cancellationToken = default)
        {
            return GetTickersAsync(filter.Symbols, cancellationToken);
        }

        private async Task<IEnumerable<string>> GetAllSymbolsAsync(CancellationToken cancellationToken)
        {
            var response = await GetSymbolsRawAsync(null, cancellationToken).ConfigureAwait(false);
            if (response?.Symbol is null || response.Symbol.Count == 0)
            {
                return Array.Empty<string>();
            }

            var symbols = new List<string>();
            var symbolList = response.Symbol.ToList();
            var tradedList = response.ExchangeTraded?.ToList();

            for (int i = 0; i < symbolList.Count; i++)
            {
                var symbol = symbolList[i];
                var isTraded = tradedList != null && i < tradedList.Count ? tradedList[i] : false;

                if (!string.IsNullOrWhiteSpace(symbol) && isTraded)
                {
                    symbols.Add(symbol.Trim());
                }
            }

            return symbols.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// [BEAST MODE] Gets tickers in parallel batches for maximum performance.
        /// Automatically chunks large symbol lists to avoid URL length limits.
        /// </summary>
        public async Task<IReadOnlyList<TickerResponse>> GetTickersBatchAsync(IEnumerable<string> symbols, int batchSize = 50, CancellationToken cancellationToken = default)
        {
            var result = await GetTickersAsync(symbols, cancellationToken).ConfigureAwait(false);
            return result.ToList();
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
