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

        public Task<ICollection<PositionResponse>> GetPositionsRawAsync(string accountId, string? symbols = null, CancellationToken cancellationToken = default)
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
        /// Gets open positions for specific symbols (string overload for backward compatibility).
        /// </summary>
        public Task<ICollection<PositionResponse>> GetPositionsAsync(string accountId, string symbol, CancellationToken cancellationToken = default)
        {
            return GetPositionsAsync(accountId, new[] { symbol }, cancellationToken);
        }

        /// <summary>
        /// Gets open positions for specific symbols (Universal Filter).
        /// </summary>
        public async Task<ICollection<PositionResponse>> GetPositionsAsync(string accountId, IEnumerable<string>? symbols = null, CancellationToken cancellationToken = default)
        {
            return (await BatchHelper.ExecuteNativeBatchAsync<PositionResponse>(
                symbols,
                50,
                async ct => (await GetSymbolsRawAsync(null, ct).ConfigureAwait(false)).Symbol ?? Enumerable.Empty<string>(),
                async (batch, ct) => await GetPositionsRawAsync(accountId, batch, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false)).ToList();
        }

        #endregion
    }
}
