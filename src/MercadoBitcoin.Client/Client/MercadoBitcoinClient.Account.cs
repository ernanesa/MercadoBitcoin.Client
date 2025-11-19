using MercadoBitcoin.Client.Generated;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Account

        public Task<ICollection<AccountResponse>> GetAccountsAsync()
        {
            return _generatedClient.AccountsAsync();
        }

        /// <summary>
        /// Version with CancellationToken for scenarios requiring cooperative cancellation.
        /// </summary>
        public Task<ICollection<AccountResponse>> GetAccountsAsync(CancellationToken cancellationToken)
        {
            return _generatedClient.AccountsAsync(cancellationToken);
        }

        public Task<ICollection<CryptoBalanceResponse>> GetBalancesAsync(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("Invalid accountId", nameof(accountId));
            return _generatedClient.BalancesAsync(accountId.Trim());
        }

        /// <summary>
        /// Version with CancellationToken of <see cref="GetBalancesAsync(string)"/>.
        /// </summary>
        public Task<ICollection<CryptoBalanceResponse>> GetBalancesAsync(string accountId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("Invalid accountId", nameof(accountId));
            return _generatedClient.BalancesAsync(accountId.Trim(), cancellationToken);
        }

        public Task<ICollection<GetTierResponse>> GetTierAsync(string accountId)
        {
            return _generatedClient.TierAsync(accountId);
        }

        public Task<ICollection<GetTierResponse>> GetTierAsync(string accountId, CancellationToken cancellationToken)
        {
            return _generatedClient.TierAsync(accountId, cancellationToken);
        }

        public Task<GetMarketFeesResponse> GetTradingFeesAsync(string accountId, string symbol)
        {
            return _generatedClient.Fees2Async(accountId, symbol);
        }

        public Task<GetMarketFeesResponse> GetTradingFeesAsync(string accountId, string symbol, CancellationToken cancellationToken)
        {
            return _generatedClient.Fees2Async(accountId, symbol, cancellationToken);
        }

        public Task<ICollection<PositionResponse>> GetPositionsAsync(string accountId, string? symbols = null)
        {
            return _generatedClient.PositionsAsync(accountId, symbols);
        }

        public Task<ICollection<PositionResponse>> GetPositionsAsync(string accountId, string? symbols, CancellationToken cancellationToken)
        {
            return _generatedClient.PositionsAsync(accountId, symbols, cancellationToken);
        }

        #endregion
    }
}
