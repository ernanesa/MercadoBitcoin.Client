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

        public Task<ICollection<CryptoBalanceResponse>> GetBalancesAsync(string accountId)
        {
            return _generatedClient.BalancesAsync(accountId);
        }

        public Task<ICollection<GetTierResponse>> GetTierAsync(string accountId)
        {
            return _generatedClient.TierAsync(accountId);
        }

        public Task<GetMarketFeesResponse> GetTradingFeesAsync(string accountId, string symbol)
        {
            return _generatedClient.Fees2Async(accountId, symbol);
        }

        public Task<ICollection<PositionResponse>> GetPositionsAsync(string accountId, string? symbols = null)
        {
            return _generatedClient.PositionsAsync(accountId, symbols);
        }

        #endregion
    }
}
