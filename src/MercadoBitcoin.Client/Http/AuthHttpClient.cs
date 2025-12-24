using System.Buffers;
using System.IO.Pipelines;
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
                // Beast Mode v5.0: Zero-allocation error parsing using PipeReader
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var reader = PipeReader.Create(stream);

                try
                {
                    while (true)
                    {
                        ReadResult result = await reader.ReadAsync(cancellationToken);
                        ReadOnlySequence<byte> buffer = result.Buffer;

                        if (result.IsCompleted)
                        {
                            if (!buffer.IsEmpty)
                            {
                                var jsonReader = new Utf8JsonReader(buffer);
                                var errorResponse = JsonSerializer.Deserialize(ref jsonReader, MercadoBitcoinJsonSerializerContext.Default.ErrorResponse);

                                var statusMsg = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase ?? "Unknown"}";
                                var finalErrorResponse = errorResponse ?? new ErrorResponse { Code = "UNKNOWN_ERROR", Message = statusMsg };

                                if (string.IsNullOrWhiteSpace(finalErrorResponse.Message))
                                {
                                    finalErrorResponse.Message = statusMsg;
                                }

                                throw new MercadoBitcoinApiException($"API Error: {finalErrorResponse.Message}", finalErrorResponse);
                            }
                            break;
                        }

                        // We only need the first chunk for most error responses, but we could buffer more if needed.
                        // For simplicity and performance, we assume the error fits in the first read or we wait for completion.
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
                finally
                {
                    await reader.CompleteAsync();
                }

                // Fallback if pipe was empty
                throw new MercadoBitcoinApiException($"API Error: HTTP {(int)response.StatusCode} {response.ReasonPhrase}", new ErrorResponse { Code = "UNKNOWN_ERROR", Message = response.ReasonPhrase ?? "Unknown" });
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
