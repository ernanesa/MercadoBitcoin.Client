using System.Threading;

namespace MercadoBitcoin.Client.Internal.Security
{
    /// <summary>
    /// Internal store for the access token, allowing sharing between AuthHttpClient and AuthenticationHandler.
    /// </summary>
    public class TokenStore
    {
        private string? _accessToken;

        public string? AccessToken
        {
            get => Volatile.Read(ref _accessToken);
            set => Volatile.Write(ref _accessToken, value);
        }
    }
}
