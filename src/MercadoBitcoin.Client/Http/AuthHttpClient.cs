using System.Buffers;
using System.Net.Http.Headers;
using System.Text.Json;
using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Models;

namespace MercadoBitcoin.Client
{
    public class AuthHttpClient : DelegatingHandler
    {
        private readonly Internal.Security.TokenStore _tokenStore;
        private readonly HttpConfiguration _httpConfig;

        /// <summary>
        /// Constructor for use with DI (via IHttpClientFactory)
        /// </summary>
        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
        public AuthHttpClient(Internal.Security.TokenStore tokenStore, Microsoft.Extensions.Options.IOptionsSnapshot<Configuration.MercadoBitcoinClientOptions> options)
            : this(tokenStore, options?.Value?.RetryPolicyConfig ?? new RetryPolicyConfig(), options?.Value?.HttpConfiguration ?? HttpConfiguration.CreateHttp2Default(), false) // Disable embedded retry for DI
        {
        }

        /// <summary>
        /// Default constructor for standalone usage
        /// </summary>
        public AuthHttpClient() : this(null, null, null, true)
        {
        }

        /// <summary>
        /// Constructor for standalone usage with custom configuration
        /// </summary>
        public AuthHttpClient(RetryPolicyConfig? retryConfig, HttpConfiguration? httpConfig)
            : this(new Internal.Security.TokenStore(), retryConfig, httpConfig, true)
        {
        }

        internal AuthHttpClient(Internal.Security.TokenStore? tokenStore, RetryPolicyConfig? retryConfig, HttpConfiguration? httpConfig, bool useEmbeddedRetry)
        {
            _tokenStore = tokenStore ?? new Internal.Security.TokenStore();
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();

            if (useEmbeddedRetry)
            {
                // Create the retry handler with HTTP configuration
                var retryHandler = new RetryHandler(retryConfig ?? new RetryPolicyConfig(), _httpConfig);
                InnerHandler = retryHandler;
            }
        }

        /// <summary>
        /// Gets the current access token (for diagnostics). DO NOT expose this publicly in production logs.
        /// </summary>
        public string? GetAccessToken() => _tokenStore.AccessToken;



        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // If the request already has an Authorization header, don't overwrite it.
            // This is useful for the initial /authorize call.
            if (string.IsNullOrEmpty(request.Headers.Authorization?.Parameter) && !string.IsNullOrEmpty(_tokenStore.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(4096);
                try
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    int bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                    ErrorResponse? errorResponse = null;
                    try
                    {
                        errorResponse = JsonSerializer.Deserialize(buffer.AsSpan(0, bytesRead), MercadoBitcoinJsonSerializerContext.Default.ErrorResponse);
                    }
                    catch (JsonException)
                    {
                        var errorString = System.Text.Encoding.UTF8.GetString(buffer.AsSpan(0, bytesRead));
                        errorResponse = new ErrorResponse { Code = "UNKNOWN_ERROR", Message = errorString };
                    }

                    var finalErrorResponse = errorResponse ?? new ErrorResponse { Code = "UNKNOWN_ERROR", Message = "Unknown error" };
                    throw new MercadoBitcoinApiException($"API Error: {finalErrorResponse.Message}", finalErrorResponse);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            // HttpClient is managed by IHttpClientFactory, we don't need to dispose
            base.Dispose(disposing);
        }
    }
}
