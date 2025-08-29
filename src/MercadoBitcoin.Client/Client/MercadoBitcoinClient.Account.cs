using MercadoBitcoin.Client.Generated;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        /// Versão com CancellationToken para cenários que exigem cancelamento cooperativo.
        /// </summary>
        public Task<ICollection<AccountResponse>> GetAccountsAsync(CancellationToken cancellationToken)
        {
            return _generatedClient.AccountsAsync(cancellationToken);
        }

        public Task<ICollection<CryptoBalanceResponse>> GetBalancesAsync(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("accountId inválido", nameof(accountId));
            return _generatedClient.BalancesAsync(accountId.Trim());
        }

        /// <summary>
        /// Versão com CancellationToken de <see cref="GetBalancesAsync(string)"/>.
        /// </summary>
        public Task<ICollection<CryptoBalanceResponse>> GetBalancesAsync(string accountId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("accountId inválido", nameof(accountId));
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
