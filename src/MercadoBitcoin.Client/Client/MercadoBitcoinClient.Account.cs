using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Internal.Helpers;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Account

        public Task<ICollection<AccountResponse>> GetAccountsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.AccountsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<CryptoBalanceResponse>> GetBalancesAsync(string accountId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("Invalid accountId", nameof(accountId));
            try
            {
                return _generatedClient.BalancesAsync(accountId.Trim(), cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<GetTierResponse>> GetTierAsync(string accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.TierAsync(accountId, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<GetMarketFeesResponse> GetTradingFeesAsync(string accountId, string symbol, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.Fees2Async(accountId, symbol, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<PositionResponse>> GetPositionsAsync(string accountId, string? symbols = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.PositionsAsync(accountId, symbols, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Gets open positions for specific symbols (Convenience overload with batching).
        /// </summary>
        public async Task<ICollection<PositionResponse>> GetPositionsAsync(string accountId, IEnumerable<string> symbols, CancellationToken cancellationToken = default)
        {
            var normalized = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalized.Count == 0) return await GetPositionsAsync(accountId, (string?)null, cancellationToken).ConfigureAwait(false);

            return (await BatchHelper.ExecuteNativeBatchAsync<PositionResponse>(
                normalized,
                50,
                async (batch, ct) => (IEnumerable<PositionResponse>)await GetPositionsAsync(accountId, batch, ct),
                cancellationToken).ConfigureAwait(false)).ToList();
        }

        #endregion
    }
}
