using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Internal.Helpers;
using MercadoBitcoin.Client.Models;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Trading

        public Task<ICollection<OrderResponse>> ListOrdersRawAsync(string symbol, string accountId, string? hasExecutions = null, string? side = null, string? status = null, string? idFrom = null, string? idTo = null, string? createdAtFrom = null, string? createdAtTo = null, string? executedAtFrom = null, string? executedAtTo = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrdersAllAsync(symbol, accountId, hasExecutions, side, status, idFrom, idTo, createdAtFrom, createdAtTo, executedAtFrom, executedAtTo, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Lists orders for a specific symbol (string overload for backward compatibility).
        /// </summary>
        public Task<ICollection<OrderResponse>> ListOrdersAsync(string symbol, string accountId, string? hasExecutions = null, string? side = null, string? status = null, string? idFrom = null, string? idTo = null, string? createdAtFrom = null, string? createdAtTo = null, string? executedAtFrom = null, string? executedAtTo = null, CancellationToken cancellationToken = default)
        {
            return ListOrdersRawAsync(symbol, accountId, hasExecutions, side, status, idFrom, idTo, createdAtFrom, createdAtTo, executedAtFrom, executedAtTo, cancellationToken);
        }

        /// <summary>
        /// Lists orders for multiple symbols (Universal Filter).
        /// </summary>
        public async Task<ICollection<OrderResponse>> ListOrdersAsync(string accountId, IEnumerable<string>? symbols = null, string? hasExecutions = null, string? side = null, string? status = null, string? idFrom = null, string? idTo = null, string? createdAtFrom = null, string? createdAtTo = null, string? executedAtFrom = null, string? executedAtTo = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            var results = await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => ListOrdersRawAsync(symbol, accountId, hasExecutions, side, status, idFrom, idTo, createdAtFrom, createdAtTo, executedAtFrom, executedAtTo, ct),
                cancellationToken).ConfigureAwait(false);

            return results.SelectMany(r => r).ToList();
        }

        /// <summary>
        /// Lists orders using a universal filter.
        /// </summary>
        public Task<ICollection<OrderResponse>> ListOrdersAsync(string accountId, UniversalFilter filter, string? hasExecutions = null, string? side = null, string? status = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            return ListOrdersAsync(
                accountId,
                filter.Symbols,
                hasExecutions,
                side,
                status,
                idFrom: null,
                idTo: null,
                createdAtFrom: filter.From?.ToString(),
                createdAtTo: filter.To?.ToString(),
                executedAtFrom: null,
                executedAtTo: null,
                maxDegreeOfParallelism,
                cancellationToken);
        }

        public Task<PlaceOrderResponse> PlaceOrderAsync(string symbol, string accountId, PlaceOrderRequest payload, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrdersPOSTAsync(symbol, accountId, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<CancelOrderResponse> CancelOrderAsync(string accountId, string symbol, string orderId, bool? async = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrdersDELETEAsync(accountId, symbol, orderId, async, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<OrderResponse> GetOrderAsync(string symbol, string accountId, string orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrdersGETAsync(symbol, accountId, orderId, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ListAllOrdersResponse> ListAllOrdersRawAsync(string accountId, string? hasExecutions = null, string? symbols = null, string? status = null, string? size = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrdersGET2Async(accountId, hasExecutions, symbols, status, size, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Lists all orders for multiple symbols (Universal Filter).
        /// </summary>
        public async Task<ListAllOrdersResponse> ListAllOrdersAsync(string accountId, IEnumerable<string>? symbols = null, string? hasExecutions = null, string? status = null, string? size = null, CancellationToken cancellationToken = default)
        {
            var results = await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                5,
                GetAllSymbolsAsync,
                (symbol, ct) => ListAllOrdersRawAsync(accountId, hasExecutions, symbol, status, size, ct),
                cancellationToken).ConfigureAwait(false);

            // Combine all items from all responses
            var allItems = new List<Orders>();
            foreach (var res in results)
            {
                if (res.Items != null)
                {
                    allItems.AddRange(res.Items);
                }
            }

            return new ListAllOrdersResponse
            {
                Items = allItems
            };
        }

        /// <summary>
        /// Lists all orders using a universal filter.
        /// </summary>
        public Task<ListAllOrdersResponse> ListAllOrdersAsync(string accountId, UniversalFilter filter, string? hasExecutions = null, string? status = null, CancellationToken cancellationToken = default)
        {
            return ListAllOrdersAsync(accountId, filter.Symbols, hasExecutions, status, filter.Limit?.ToString(), cancellationToken);
        }

        public Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountRawAsync(string accountId, bool? hasExecutions = null, string? symbol = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use OpenClient since cancel_all_open_orders lives there in the generated code
                return _openClient.OrdersAsync(accountId, hasExecutions, symbol, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Cancels all open orders for multiple symbols (Universal Filter).
        /// </summary>
        public async Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountAsync(string accountId, IEnumerable<string>? symbols = null, bool? hasExecutions = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            var results = await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => CancelAllOpenOrdersByAccountRawAsync(accountId, hasExecutions, symbol, ct),
                cancellationToken).ConfigureAwait(false);

            return results.SelectMany(r => r).ToList();
        }

        /// <summary>
        /// Cancels all open orders using a universal filter.
        /// </summary>
        public Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountAsync(string accountId, UniversalFilter filter, bool? hasExecutions = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            return CancelAllOpenOrdersByAccountAsync(accountId, filter.Symbols, hasExecutions, maxDegreeOfParallelism, cancellationToken);
        }

        #endregion
    }
}