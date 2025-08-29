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

        public Task<ICollection<OrderResponse>> ListOrdersAsync(string symbol, string accountId, string? has_executions = null, string? side = null, string? status = null, string? id_from = null, string? id_to = null, string? created_at_from = null, string? created_at_to = null, string? executed_at_from = null, string? executed_at_to = null)
        {
            return _generatedClient.OrdersAllAsync(symbol, accountId, has_executions, side, status, id_from, id_to, created_at_from, created_at_to, executed_at_from, executed_at_to);
        }

        public Task<ICollection<OrderResponse>> ListOrdersAsync(string symbol, string accountId, CancellationToken cancellationToken, string? has_executions = null, string? side = null, string? status = null, string? id_from = null, string? id_to = null, string? created_at_from = null, string? created_at_to = null, string? executed_at_from = null, string? executed_at_to = null)
        {
            return _generatedClient.OrdersAllAsync(symbol, accountId, has_executions, side, status, id_from, id_to, created_at_from, created_at_to, executed_at_from, executed_at_to, cancellationToken);
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

        public Task<ListAllOrdersResponse> ListAllOrdersAsync(string accountId, string? has_executions = null, string? symbol = null, string? status = null, string? size = null)
        {
            return _generatedClient.OrdersGET2Async(accountId, has_executions, symbol, status, size);
        }

        public Task<ListAllOrdersResponse> ListAllOrdersAsync(string accountId, CancellationToken cancellationToken, string? has_executions = null, string? symbol = null, string? status = null, string? size = null)
        {
            return _generatedClient.OrdersGET2Async(accountId, has_executions, symbol, status, size, cancellationToken);
        }

        public Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountAsync(string accountId, bool? has_executions = null, string? symbol = null)
        {
            // Use OpenClient since cancel_all_open_orders lives there in the generated code
            return _openClient.OrdersAsync(accountId, has_executions, symbol);
        }

        public Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountAsync(string accountId, CancellationToken cancellationToken, bool? has_executions = null, string? symbol = null)
        {
            return _openClient.OrdersAsync(accountId, has_executions, symbol, cancellationToken);
        }

        #endregion
    }
}