using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Internal.Security;
using Microsoft.Extensions.Options;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// DelegatingHandler that intercepts 401 Unauthorized responses and attempts to refresh the access token.
    /// </summary>
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly Internal.Security.TokenStore _tokenStore;
        private readonly MercadoBitcoinClientOptions _options;
        private readonly IMercadoBitcoinCredentialProvider _credentialProvider;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly HttpClient _internalClient;

        public AuthenticationHandler(Internal.Security.TokenStore tokenStore, IOptions<MercadoBitcoinClientOptions> options, IMercadoBitcoinCredentialProvider credentialProvider)
            : this(tokenStore, options?.Value ?? throw new ArgumentNullException(nameof(options)), credentialProvider)
        {
        }

        internal AuthenticationHandler(Internal.Security.TokenStore tokenStore, MercadoBitcoinClientOptions options, IMercadoBitcoinCredentialProvider? credentialProvider = null)
        {
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _credentialProvider = credentialProvider ?? new DefaultMercadoBitcoinCredentialProvider(Options.Create(options));
            _internalClient = new HttpClient(); // Used for auth requests to avoid handler recursion
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                var credentials = await _credentialProvider.GetCredentialsAsync(cancellationToken);
                if (credentials != null && !string.IsNullOrEmpty(credentials.Login) && !string.IsNullOrEmpty(credentials.Password))
                {
                    bool authenticated = false;
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        // Perform Authentication
                        var authRequest = new AuthorizeRequest
                        {
                            Login = credentials.Login,
                            Password = credentials.Password
                        };

                        var baseUrl = _options.BaseUrl;
                        if (!baseUrl.EndsWith("/")) baseUrl += "/";
                        var authUri = new Uri(new Uri(baseUrl), "authorize");

                        // Use a separate client to avoid infinite loops and handler recursion
                        using var authResponse = await _internalClient.PostAsJsonAsync(authUri, authRequest, MercadoBitcoinJsonSerializerContext.Default.AuthorizeRequest, cancellationToken);

                        if (authResponse.IsSuccessStatusCode)
                        {
                            var authResult = await authResponse.Content.ReadFromJsonAsync(MercadoBitcoinJsonSerializerContext.Default.AuthorizeResponse, cancellationToken);
                            if (authResult != null && !string.IsNullOrEmpty(authResult.Access_token))
                            {
                                _tokenStore.AccessToken = authResult.Access_token;
                                authenticated = true;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore authentication exceptions to allow the original 401/403 to propagate if retry fails
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                    if (authenticated)
                    {
                        // Release the failed response content before retrying
                        response.Dispose();

                        // Retry the original request
                        var newRequest = await CloneHttpRequestMessageAsync(request);

                        // Manually add the token because we are downstream of AuthHttpClient
                        newRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);

                        return await base.SendAsync(newRequest, cancellationToken);
                    }
                }
            }

            return response;
        }

        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
        {
            HttpRequestMessage clone = new HttpRequestMessage(req.Method, req.RequestUri);

            // Copy the request's content (via a MemoryStream) into the cloned object
            var ms = new System.IO.MemoryStream();
            if (req.Content != null)
            {
                await req.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                // Copy the content headers
                foreach (var h in req.Content.Headers)
                    clone.Content.Headers.Add(h.Key, h.Value);
            }

            clone.Version = req.Version;

            foreach (var header in req.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            foreach (var option in req.Options)
                clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

            return clone;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _internalClient.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
