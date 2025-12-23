using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Security
{
    /// <summary>
    /// Provides credentials for Mercado Bitcoin API authentication.
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
}
