using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Wallet

        public Task<ICollection<Deposit>> ListDepositsAsync(string accountId, string symbol, string? limit = null, string? page = null, string? from = null, string? to = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.DepositsAsync(accountId, symbol, limit, page, from, to, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<DepositAddresses> GetDepositAddressesAsync(string accountId, string symbol, Network2? network = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.AddressesAsync(accountId, symbol, network, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<FiatDeposit>> ListFiatDepositsAsync(string accountId, string symbol, string? limit = null, string? page = null, string? from = null, string? to = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.Deposits2Async(accountId, symbol, limit, page, from, to, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<Withdraw> WithdrawCoinAsync(string accountId, string symbol, WithdrawCoinRequest payload, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.WithdrawPOSTAsync(accountId, symbol, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<Withdraw>> ListWithdrawalsAsync(string accountId, string symbol, int? page = null, int? pageSize = null, int? from = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.WithdrawAllAsync(accountId, symbol, page, pageSize, from, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<Withdraw> GetWithdrawalAsync(string accountId, string symbol, string withdrawId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.WithdrawGETAsync(accountId, symbol, withdrawId, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<Response> GetWithdrawLimitsAsync(string accountId, string? symbols = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.LimitsAsync(accountId, symbols, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<BRLWithdrawConfig> GetBrlWithdrawConfigAsync(string accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.BRLAsync(accountId, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<CryptoWalletAddress>> GetWithdrawCryptoWalletAddressesAsync(string accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.AddressesAllAsync(accountId, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<BankAccount>> GetWithdrawBankAccountsAsync(string accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.BankAccountsAsync(accountId, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        #endregion
    }
}
