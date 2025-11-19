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

        public Task<ICollection<Deposit>> ListDepositsAsync(string accountId, string symbol, CancellationToken cancellationToken, string? limit = null, string? page = null, string? from = null, string? to = null)
        {
            return _generatedClient.DepositsAsync(accountId, symbol, limit, page, from, to, cancellationToken);
        }

        public Task<DepositAddresses> GetDepositAddressesAsync(string accountId, string symbol, Network2? network = null)
        {
            return _generatedClient.AddressesAsync(accountId, symbol, network);
        }

        public Task<DepositAddresses> GetDepositAddressesAsync(string accountId, string symbol, Network2? network, CancellationToken cancellationToken)
        {
            return _generatedClient.AddressesAsync(accountId, symbol, network, cancellationToken);
        }

        public Task<ICollection<FiatDeposit>> ListFiatDepositsAsync(string accountId, string symbol, string? limit = null, string? page = null, string? from = null, string? to = null)
        {
            return _generatedClient.Deposits2Async(accountId, symbol, limit, page, from, to);
        }

        public Task<ICollection<FiatDeposit>> ListFiatDepositsAsync(string accountId, string symbol, CancellationToken cancellationToken, string? limit = null, string? page = null, string? from = null, string? to = null)
        {
            return _generatedClient.Deposits2Async(accountId, symbol, limit, page, from, to, cancellationToken);
        }

        public Task<Withdraw> WithdrawCoinAsync(string accountId, string symbol, WithdrawCoinRequest payload)
        {
            return _generatedClient.WithdrawPOSTAsync(accountId, symbol, payload);
        }

        public Task<Withdraw> WithdrawCoinAsync(string accountId, string symbol, WithdrawCoinRequest payload, CancellationToken cancellationToken)
        {
            return _generatedClient.WithdrawPOSTAsync(accountId, symbol, payload, cancellationToken);
        }

        public Task<ICollection<Withdraw>> ListWithdrawalsAsync(string accountId, string symbol, int? page = null, int? pageSize = null, int? from = null)
        {
            return _generatedClient.WithdrawAllAsync(accountId, symbol, page, pageSize, from);
        }

        public Task<ICollection<Withdraw>> ListWithdrawalsAsync(string accountId, string symbol, CancellationToken cancellationToken, int? page = null, int? pageSize = null, int? from = null)
        {
            return _generatedClient.WithdrawAllAsync(accountId, symbol, page, pageSize, from, cancellationToken);
        }

        public Task<Withdraw> GetWithdrawalAsync(string accountId, string symbol, string withdrawId)
        {
            return _generatedClient.WithdrawGETAsync(accountId, symbol, withdrawId);
        }

        public Task<Withdraw> GetWithdrawalAsync(string accountId, string symbol, string withdrawId, CancellationToken cancellationToken)
        {
            return _generatedClient.WithdrawGETAsync(accountId, symbol, withdrawId, cancellationToken);
        }

        public Task<Response> GetWithdrawLimitsAsync(string accountId, string? symbols = null)
        {
            return _generatedClient.LimitsAsync(accountId, symbols);
        }

        public Task<Response> GetWithdrawLimitsAsync(string accountId, CancellationToken cancellationToken, string? symbols = null)
        {
            return _generatedClient.LimitsAsync(accountId, symbols, cancellationToken);
        }

        public Task<BRLWithdrawConfig> GetBrlWithdrawConfigAsync(string accountId)
        {
            return _generatedClient.BRLAsync(accountId);
        }

        public Task<BRLWithdrawConfig> GetBrlWithdrawConfigAsync(string accountId, CancellationToken cancellationToken)
        {
            return _generatedClient.BRLAsync(accountId, cancellationToken);
        }

        public Task<ICollection<CryptoWalletAddress>> GetWithdrawCryptoWalletAddressesAsync(string accountId)
        {
            return _generatedClient.AddressesAllAsync(accountId);
        }

        public Task<ICollection<CryptoWalletAddress>> GetWithdrawCryptoWalletAddressesAsync(string accountId, CancellationToken cancellationToken)
        {
            return _generatedClient.AddressesAllAsync(accountId, cancellationToken);
        }

        public Task<ICollection<BankAccount>> GetWithdrawBankAccountsAsync(string accountId)
        {
            return _generatedClient.BankAccountsAsync(accountId);
        }

        public Task<ICollection<BankAccount>> GetWithdrawBankAccountsAsync(string accountId, CancellationToken cancellationToken)
        {
            return _generatedClient.BankAccountsAsync(accountId, cancellationToken);
        }

        #endregion
    }
}
