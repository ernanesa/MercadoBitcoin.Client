using System;
using System.Threading;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Configuration;
using Microsoft.Extensions.Options;

namespace MercadoBitcoin.Client.Internal.Security
{
    /// <summary>
    /// Default implementation of <see cref="IMercadoBitcoinCredentialProvider"/> that uses <see cref="MercadoBitcoinClientOptions"/>.
    /// </summary>
    public class DefaultMercadoBitcoinCredentialProvider : IMercadoBitcoinCredentialProvider
    {
        private readonly MercadoBitcoinClientOptions _options;

        public DefaultMercadoBitcoinCredentialProvider(IOptions<MercadoBitcoinClientOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<MercadoBitcoinCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_options.ApiLogin) || string.IsNullOrEmpty(_options.ApiPassword))
            {
                return Task.FromResult<MercadoBitcoinCredentials>(null!);
            }

            return Task.FromResult(new MercadoBitcoinCredentials(_options.ApiLogin, _options.ApiPassword));
        }
    }
}
