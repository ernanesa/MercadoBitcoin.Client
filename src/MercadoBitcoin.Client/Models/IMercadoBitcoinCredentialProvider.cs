using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Models
{
    /// <summary>
    /// Provides credentials for Mercado Bitcoin API authentication.
    /// This interface allows for dynamic credential resolution, enabling multi-user support.
    /// </summary>
    public interface IMercadoBitcoinCredentialProvider
    {
        /// <summary>
        /// Gets the credentials for the current context.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Credentials containing login and password</returns>
        Task<MercadoBitcoinCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents Mercado Bitcoin API credentials.
    /// </summary>
    public record MercadoBitcoinCredentials(string Login, string Password);

    /// <summary>
    /// A simple implementation of <see cref="IMercadoBitcoinCredentialProvider"/> that returns a fixed set of credentials.
    /// </summary>
    public class StaticCredentialProvider : IMercadoBitcoinCredentialProvider
    {
        private readonly MercadoBitcoinCredentials _credentials;

        public StaticCredentialProvider(MercadoBitcoinCredentials credentials)
        {
            _credentials = credentials;
        }

        public MercadoBitcoinCredentials GetCredentials() => _credentials;

        public Task<MercadoBitcoinCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_credentials);
    }
}
