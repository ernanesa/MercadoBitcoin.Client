using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Generated;
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
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly HttpClient _internalClient;

        public AuthenticationHandler(Internal.Security.TokenStore tokenStore, IOptions<MercadoBitcoinClientOptions> options)
            : this(tokenStore, options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        internal AuthenticationHandler(Internal.Security.TokenStore tokenStore, MercadoBitcoinClientOptions options)
        {
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _internalClient = new HttpClient(); // Used for auth requests to avoid handler recursion
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_options.ApiLogin) && !string.IsNullOrEmpty(_options.ApiPassword))
            {
                // Release the response content to avoid memory leaks
                response.Dispose();

                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Check if token was refreshed by another thread while we were waiting
                    // We can't easily check the token value itself without tracking it, 
                    // but we can try the request again if we want, or just proceed to refresh.
                    // Optimization: We could track the last refresh time or token hash.
                    // For now, we'll just refresh. The API rate limit for auth is generous enough for occasional race conditions.

                    // Perform Authentication
                    var authRequest = new AuthorizeRequest
                    {
                        Login = _options.ApiLogin,
                        Password = _options.ApiPassword
                    };

                    var authUri = new Uri(new Uri(_options.BaseUrl), "authorize");
                    
                    // Use a separate client to avoid infinite loops and handler interference
                    using var authResponse = await _internalClient.PostAsJsonAsync(authUri, authRequest, MercadoBitcoinJsonSerializerContext.Default.AuthorizeRequest, cancellationToken);

                    if (authResponse.IsSuccessStatusCode)
                    {
                        var authResult = await authResponse.Content.ReadFromJsonAsync(MercadoBitcoinJsonSerializerContext.Default.AuthorizeResponse, cancellationToken);
                        if (authResult != null && !string.IsNullOrEmpty(authResult.Access_token))
                        {
                            _tokenStore.AccessToken = authResult.Access_token;

                            // Retry the original request
                            // We need to clone the request because it might have been already sent
                            var newRequest = await CloneHttpRequestMessageAsync(request);
                            return await base.SendAsync(newRequest, cancellationToken);
                        }
                    }
                }
                catch (Exception)
                {
                    // Log or ignore, return original 401 if auth fails
                }
                finally
                {
                    _semaphore.Release();
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
