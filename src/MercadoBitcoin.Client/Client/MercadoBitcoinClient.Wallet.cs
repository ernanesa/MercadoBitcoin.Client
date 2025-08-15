using MercadoBitcoin.Client.Generated;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Wallet

        public Task<ICollection<Deposit>> ListDepositsAsync(string accountId, string symbol, string? limit = null, string? page = null, string? from = null, string? to = null)
        {
            return _generatedClient.DepositsAsync(accountId, symbol, limit, page, from, to);
        }

        public Task<DepositAddresses> GetDepositAddressesAsync(string accountId, string symbol, Network2? network = null)
        {
            return _generatedClient.AddressesAsync(accountId, symbol, network);
        }

        public Task<ICollection<FiatDeposit>> ListFiatDepositsAsync(string accountId, string symbol, string? limit = null, string? page = null, string? from = null, string? to = null)
        {
            return _generatedClient.Deposits2Async(accountId, symbol, limit, page, from, to);
        }

        public Task<Withdraw> WithdrawCoinAsync(string accountId, string symbol, WithdrawCoinRequest payload)
        {
            return _generatedClient.WithdrawPOSTAsync(accountId, symbol, payload);
        }

        public Task<ICollection<Withdraw>> ListWithdrawalsAsync(string accountId, string symbol, int? page = null, int? page_size = null, int? from = null)
        {
            return _generatedClient.WithdrawAllAsync(accountId, symbol, page, page_size, from);
        }

        public Task<Withdraw> GetWithdrawalAsync(string accountId, string symbol, string withdrawId)
        {
            return _generatedClient.WithdrawGETAsync(accountId, symbol, withdrawId);
        }

        public Task<Response> GetWithdrawLimitsAsync(string accountId, string? symbols = null)
        {
            return _generatedClient.LimitsAsync(accountId, symbols);
        }

        public Task<BRLWithdrawConfig> GetBrlWithdrawConfigAsync(string accountId)
        {
            return _generatedClient.BRLAsync(accountId);
        }

        public Task<ICollection<CryptoWalletAddress>> GetWithdrawCryptoWalletAddressesAsync(string accountId)
        {
            return _generatedClient.AddressesAllAsync(accountId);
        }

        public Task<ICollection<BankAccount>> GetWithdrawBankAccountsAsync(string accountId)
        {
            return _generatedClient.BankAccountsAsync(accountId);
        }

        #endregion
    }
}
