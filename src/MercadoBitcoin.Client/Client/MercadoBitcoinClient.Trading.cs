using MercadoBitcoin.Client.Generated;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Trading

        public Task<ICollection<OrderResponse>> ListOrdersAsync(string symbol, string accountId, string? hasExecutions = null, string? side = null, string? status = null, string? idFrom = null, string? idTo = null, string? createdAtFrom = null, string? createdAtTo = null, string? executedAtFrom = null, string? executedAtTo = null)
        {
            return _generatedClient.OrdersAllAsync(symbol, accountId, hasExecutions, side, status, idFrom, idTo, createdAtFrom, createdAtTo, executedAtFrom, executedAtTo);
        }

        public Task<ICollection<OrderResponse>> ListOrdersAsync(string symbol, string accountId, CancellationToken cancellationToken, string? hasExecutions = null, string? side = null, string? status = null, string? idFrom = null, string? idTo = null, string? createdAtFrom = null, string? createdAtTo = null, string? executedAtFrom = null, string? executedAtTo = null)
        {
            return _generatedClient.OrdersAllAsync(symbol, accountId, hasExecutions, side, status, idFrom, idTo, createdAtFrom, createdAtTo, executedAtFrom, executedAtTo, cancellationToken);
        }

        public Task<PlaceOrderResponse> PlaceOrderAsync(string symbol, string accountId, PlaceOrderRequest payload)
        {
            return _generatedClient.OrdersPOSTAsync(symbol, accountId, payload);
        }

        public Task<PlaceOrderResponse> PlaceOrderAsync(string symbol, string accountId, PlaceOrderRequest payload, CancellationToken cancellationToken)
        {
            return _generatedClient.OrdersPOSTAsync(symbol, accountId, payload, cancellationToken);
        }

        public Task<CancelOrderResponse> CancelOrderAsync(string accountId, string symbol, string orderId, bool? async = null)
        {
            return _generatedClient.OrdersDELETEAsync(accountId, symbol, orderId, async);
        }

        public Task<CancelOrderResponse> CancelOrderAsync(string accountId, string symbol, string orderId, CancellationToken cancellationToken, bool? async = null)
        {
            return _generatedClient.OrdersDELETEAsync(accountId, symbol, orderId, async, cancellationToken);
        }

        public Task<OrderResponse> GetOrderAsync(string symbol, string accountId, string orderId)
        {
            return _generatedClient.OrdersGETAsync(symbol, accountId, orderId);
        }

        public Task<OrderResponse> GetOrderAsync(string symbol, string accountId, string orderId, CancellationToken cancellationToken)
        {
            return _generatedClient.OrdersGETAsync(symbol, accountId, orderId, cancellationToken);
        }

        public Task<ListAllOrdersResponse> ListAllOrdersAsync(string accountId, string? hasExecutions = null, string? symbol = null, string? status = null, string? size = null)
        {
            return _generatedClient.OrdersGET2Async(accountId, hasExecutions, symbol, status, size);
        }

        public Task<ListAllOrdersResponse> ListAllOrdersAsync(string accountId, CancellationToken cancellationToken, string? hasExecutions = null, string? symbol = null, string? status = null, string? size = null)
        {
            return _generatedClient.OrdersGET2Async(accountId, hasExecutions, symbol, status, size, cancellationToken);
        }

        public Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountAsync(string accountId, bool? hasExecutions = null, string? symbol = null)
        {
            // Use OpenClient since cancel_all_open_orders lives there in the generated code
            return _openClient.OrdersAsync(accountId, hasExecutions, symbol);
        }

        public Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountAsync(string accountId, CancellationToken cancellationToken, bool? hasExecutions = null, string? symbol = null)
        {
            return _openClient.OrdersAsync(accountId, hasExecutions, symbol, cancellationToken);
        }

        #endregion
    }
}