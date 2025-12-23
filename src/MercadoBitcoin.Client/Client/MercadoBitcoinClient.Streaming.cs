using System.Runtime.CompilerServices;
using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Internal.Helpers;

namespace MercadoBitcoin.Client;

/// <summary>
/// Streaming extensions for MercadoBitcoinClient using IAsyncEnumerable for efficient pagination.
/// </summary>
public partial class MercadoBitcoinClient
{
    #region Streaming Methods (IAsyncEnumerable)

    /// <summary>
    /// Streams trades for a symbol, automatically handling pagination.
    /// Each trade is yielded individually without buffering the entire response.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="from">Start timestamp (optional).</param>
    /// <param name="to">End timestamp (optional).</param>
    /// <param name="limit">Number of trades per page (default: 1000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of trades.</returns>
    public async IAsyncEnumerable<TradeResponse> StreamTradesAsync(
        string symbol,
        int? from = null,
        int? to = null,
        int limit = 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        int? currentTid = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
            if (!lease.IsAcquired)
            {
                throw new MercadoBitcoinRateLimitException(
                    "Rate limit exceeded (client-side).",
                    new ErrorResponse { Code = "CLIENT_RATE_LIMIT", Message = "Rate limit exceeded (client-side)." });
            }

            ICollection<TradeResponse> trades;
            try
            {
                trades = await _generatedClient.TradesAsync(
                    symbol,
                    tid: currentTid,
                    since: null,
                    from: from,
                    to: to,
                    limit: limit,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }

            if (trades == null || trades.Count == 0)
            {
                yield break;
            }

            foreach (var trade in trades)
            {
                yield return trade;
                // Track the last trade ID for pagination
                if (trade.Tid.HasValue)
                {
                    currentTid = (int?)trade.Tid.Value;
                }
            }

            // If we got fewer trades than requested, we've reached the end
            if (trades.Count < limit)
            {
                yield break;
            }

            // Move to next page by using the last trade ID
            if (currentTid.HasValue)
            {
                currentTid++;
            }
            else
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Streams orders for a symbol, automatically handling pagination.
    /// Each order is yielded individually without buffering the entire response.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="accountId">Account ID.</param>
    /// <param name="status">Order status filter (optional).</param>
    /// <param name="side">Order side filter: "buy" or "sell" (optional).</param>
    /// <param name="hasExecutions">Filter for orders with executions (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of orders.</returns>
    public async IAsyncEnumerable<OrderResponse> StreamOrdersAsync(
        string symbol,
        string accountId,
        string? status = null,
        string? side = null,
        string? hasExecutions = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        string? idFrom = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
            if (!lease.IsAcquired)
            {
                throw new MercadoBitcoinRateLimitException(
                    "Rate limit exceeded (client-side).",
                    new ErrorResponse { Code = "CLIENT_RATE_LIMIT", Message = "Rate limit exceeded (client-side)." });
            }

            ICollection<OrderResponse> orders;
            try
            {
                orders = await _generatedClient.OrdersAllAsync(
                    symbol,
                    accountId,
                    hasExecutions,
                    side,
                    status,
                    idFrom,
                    null,
                    null,
                    null,
                    null,
                    null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }

            if (orders == null || orders.Count == 0)
            {
                yield break;
            }

            string? lastOrderId = null;
            foreach (var order in orders)
            {
                yield return order;
                lastOrderId = order.Id;
            }

            // If we have a last order ID, use it for the next page
            if (!string.IsNullOrEmpty(lastOrderId))
            {
                idFrom = lastOrderId;
            }
            else
            {
                yield break;
            }

            // If we got fewer than typical page size, we've likely reached the end
            // The API doesn't have explicit pagination, so we rely on ID-based cursoring
            if (orders.Count < 100)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Streams candles for a symbol within a time range, automatically handling large requests.
    /// Useful for backtesting and historical data analysis.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTC-BRL").</param>
    /// <param name="resolution">Candle resolution (e.g., "1m", "15m", "1h", "1d").</param>
    /// <param name="from">Start timestamp (Unix seconds).</param>
    /// <param name="to">End timestamp (Unix seconds).</param>
    /// <param name="batchSize">Number of candles per request (default: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of candles.</returns>
    public async IAsyncEnumerable<Models.CandleData> StreamCandlesAsync(
        string symbol,
        string resolution,
        int from,
        int to,
        int batchSize = 500,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolution);

        if (from > to)
        {
            (from, to) = (to, from);
        }

        var currentFrom = from;

        while (currentFrom < to && !cancellationToken.IsCancellationRequested)
        {
            using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
            if (!lease.IsAcquired)
            {
                throw new MercadoBitcoinRateLimitException(
                    "Rate limit exceeded (client-side).",
                    new ErrorResponse { Code = "CLIENT_RATE_LIMIT", Message = "Rate limit exceeded (client-side)." });
            }

            IReadOnlyList<Models.CandleData> candles;
            try
            {
                candles = await GetCandlesTypedAsync(
                    symbol,
                    resolution,
                    to: Math.Min(to, currentFrom + GetResolutionSeconds(resolution) * batchSize),
                    from: currentFrom,
                    countback: null, // Don't use countback for range streaming
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }

            if (candles == null || candles.Count == 0)
            {
                yield break;
            }

            foreach (var candle in candles)
            {
                if (candle.Timestamp >= from && candle.Timestamp <= to)
                {
                    yield return candle;
                }
            }

            // Move window forward based on the last candle's timestamp
            if (candles.Count > 0)
            {
                var lastCandle = candles[^1];
                currentFrom = (int)(lastCandle.Timestamp + GetResolutionSeconds(resolution));
            }
            else
            {
                yield break;
            }

            // If we got fewer candles than requested, we've reached the end
            if (candles.Count < batchSize)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Streams all withdrawals for an account, handling pagination automatically.
    /// </summary>
    /// <param name="accountId">Account ID.</param>
    /// <param name="symbol">Asset symbol filter (optional).</param>
    /// <param name="pageSize">Number of items per page (default: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of withdrawals.</returns>
    public IAsyncEnumerable<Withdraw> StreamWithdrawalsAsync(
        string accountId,
        string? symbol = null,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        return AsyncPaginationHelper.PaginateAsync<Withdraw>(
            fetchPage: async (limit, page, ct) =>
            {
                using var lease = await _rateLimiter.AcquireAsync(1, ct).ConfigureAwait(false);
                if (!lease.IsAcquired)
                {
                    throw new MercadoBitcoinRateLimitException(
                        "Rate limit exceeded (client-side).",
                        new ErrorResponse { Code = "CLIENT_RATE_LIMIT", Message = "Rate limit exceeded (client-side)." });
                }

                try
                {
                    return await _generatedClient.WithdrawAllAsync(
                        accountId,
                        symbol ?? "BTC", // Default to BTC if null, or throw? API requires symbol.
                        page,
                        limit,
                        null,
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            },
            pageSize: pageSize,
            startPage: 1,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Streams all fiat deposits for an account, handling pagination automatically.
    /// </summary>
    /// <param name="accountId">Account ID.</param>
    /// <param name="pageSize">Number of items per page (default: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of fiat deposits.</returns>
    public IAsyncEnumerable<FiatDeposit> StreamFiatDepositsAsync(
        string accountId,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        return AsyncPaginationHelper.PaginateAsync<FiatDeposit>(
            fetchPage: async (limit, page, ct) =>
            {
                using var lease = await _rateLimiter.AcquireAsync(1, ct).ConfigureAwait(false);
                if (!lease.IsAcquired)
                {
                    throw new MercadoBitcoinRateLimitException(
                        "Rate limit exceeded (client-side).",
                        new ErrorResponse { Code = "CLIENT_RATE_LIMIT", Message = "Rate limit exceeded (client-side)." });
                }

                try
                {
                    return await _generatedClient.Deposits2Async(
                        accountId,
                        "BRL", // Only BRL is supported for fiat deposits
                        limit.ToString(),
                        page.ToString(),
                        null,
                        null,
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw MapApiException(ex);
                }
            },
            pageSize: pageSize,
            startPage: 1,
            cancellationToken: cancellationToken);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the number of seconds in a candle resolution.
    /// </summary>
    private static int GetResolutionSeconds(string resolution)
    {
        return resolution.ToLowerInvariant() switch
        {
            "1m" => 60,
            "5m" => 300,
            "15m" => 900,
            "30m" => 1800,
            "1h" => 3600,
            "2h" => 7200,
            "4h" => 14400,
            "6h" => 21600,
            "8h" => 28800,
            "12h" => 43200,
            "1d" => 86400,
            "3d" => 259200,
            "1w" => 604800,
            "1M" => 2592000,
            _ => 3600 // Default to 1 hour
        };
    }

    #endregion
}
