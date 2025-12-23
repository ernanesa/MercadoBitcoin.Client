using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Internal.Helpers;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Wallet

        public Task<ICollection<Deposit>> ListDepositsRawAsync(string accountId, string symbol, string? limit = null, string? page = null, string? from = null, string? to = null, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Lists deposits for a specific symbol (string overload for backward compatibility).
        /// </summary>
        public Task<ICollection<Deposit>> ListDepositsAsync(string accountId, string symbol, string? limit = null, string? page = null, string? from = null, string? to = null, CancellationToken cancellationToken = default)
        {
            return ListDepositsAsync(accountId, new[] { symbol }, limit, page, from, to, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Lists deposits for multiple symbols (Universal Filter).
        /// </summary>
        public async Task<ICollection<Deposit>> ListDepositsAsync(string accountId, IEnumerable<string>? symbols = null, string? limit = null, string? page = null, string? from = null, string? to = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            var results = await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => ListDepositsRawAsync(accountId, symbol, limit, page, from, to, ct),
                cancellationToken).ConfigureAwait(false);

            return results.SelectMany(r => r).ToList();
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

        public Task<ICollection<Withdraw>> ListWithdrawalsRawAsync(string accountId, string symbol, int? page = null, int? pageSize = null, int? from = null, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Lists withdrawals for a specific symbol (string overload for backward compatibility).
        /// </summary>
        public Task<ICollection<Withdraw>> ListWithdrawalsAsync(string accountId, string symbol, int? page = null, int? pageSize = null, int? from = null, CancellationToken cancellationToken = default)
        {
            return ListWithdrawalsAsync(accountId, new[] { symbol }, page, pageSize, from, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Lists withdrawals for multiple symbols (Universal Filter).
        /// </summary>
        public async Task<ICollection<Withdraw>> ListWithdrawalsAsync(string accountId, IEnumerable<string>? symbols = null, int? page = null, int? pageSize = null, int? from = null, int maxDegreeOfParallelism = 5, CancellationToken cancellationToken = default)
        {
            var results = await BatchHelper.ExecuteParallelFanOutAsync(
                symbols,
                maxDegreeOfParallelism,
                GetAllSymbolsAsync,
                (symbol, ct) => ListWithdrawalsRawAsync(accountId, symbol, page, pageSize, from, ct),
                cancellationToken).ConfigureAwait(false);

            return results.SelectMany(r => r).ToList();
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

        public async Task<Response> GetWithdrawLimitsRawAsync(string accountId, string? symbols = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _generatedClient.LimitsAsync(accountId, symbols, cancellationToken).ConfigureAwait(false);
                return response ?? new Response();
            }
            catch (ApiException apiEx) when (apiEx.Message.Contains("Response was null"))
            {
                return new Response();
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Gets withdraw limits for multiple symbols (Universal Filter).
        /// </summary>
        public async Task<ICollection<Response>> GetWithdrawLimitsAsync(string accountId, IEnumerable<string>? symbols = null, CancellationToken cancellationToken = default)
        {
            return (await BatchHelper.ExecuteNativeBatchAsync<Response>(
                symbols,
                100,
                GetAllSymbolsAsync,
                async (batch, ct) => new[] { await GetWithdrawLimitsRawAsync(accountId, batch, ct).ConfigureAwait(false) },
                cancellationToken).ConfigureAwait(false)).ToList();
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
