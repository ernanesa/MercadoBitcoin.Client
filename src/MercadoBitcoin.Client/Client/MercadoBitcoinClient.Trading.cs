using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Trading

        public Task<ICollection<OrderResponse>> ListOrdersAsync(string symbol, string accountId, string? hasExecutions = null, string? side = null, string? status = null, string? idFrom = null, string? idTo = null, string? createdAtFrom = null, string? createdAtTo = null, string? executedAtFrom = null, string? executedAtTo = null, CancellationToken cancellationToken = default)
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

        public Task<ListAllOrdersResponse> ListAllOrdersAsync(string accountId, string? hasExecutions = null, string? symbol = null, string? status = null, string? size = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrdersGET2Async(accountId, hasExecutions, symbol, status, size, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<CancelOpenOrdersResponse>> CancelAllOpenOrdersByAccountAsync(string accountId, bool? hasExecutions = null, string? symbol = null, CancellationToken cancellationToken = default)
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

        #endregion
    }
}