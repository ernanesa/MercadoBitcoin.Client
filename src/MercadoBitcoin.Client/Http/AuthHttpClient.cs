using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Errors;

namespace MercadoBitcoin.Client
{
    public class AuthHttpClient : DelegatingHandler
    {
        private string? _accessToken;
        private readonly HttpConfiguration _httpConfig;
        private readonly bool _traceHttp;

        private static readonly JsonSerializerOptions JsonSerializerOptions = MercadoBitcoinJsonSerializerContext.Default.Options;

        public AuthHttpClient(RetryPolicyConfig? retryConfig = null, HttpConfiguration? httpConfig = null)
        {
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            _traceHttp = Environment.GetEnvironmentVariable("MB_TRACE_HTTP") == "1";

            // Create the retry handler with HTTP configuration
            var retryHandler = new RetryHandler(retryConfig, _httpConfig);
            InnerHandler = retryHandler;
        }

        /// <summary>
        /// Constructor for use with IHttpClientFactory (DI)
        /// </summary>
        public AuthHttpClient() : this(null, null)
        {
        }

        /// <summary>
        /// Sets the access token for authentication
        /// </summary>
        /// <param name="accessToken">Access token</param>
        public void SetAccessToken(string? accessToken)
        {
            _accessToken = accessToken;
        }

        /// <summary>
        /// Gets the current access token (for diagnostics). DO NOT expose this publicly in production logs.
        /// </summary>
        public string? GetAccessToken() => _accessToken;



        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            // If the request already has an Authorization header, don't overwrite it.
            // This is useful for the initial /authorize call.
            if (string.IsNullOrEmpty(request.Headers.Authorization?.Parameter) && !string.IsNullOrEmpty(_accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }


            if (_traceHttp)
            {
                Console.WriteLine($"[TRACE HTTP] => {request.Method} {request.RequestUri} (AuthHeader={(request.Headers.Authorization != null ? (request.Headers.Authorization.Scheme + " ...") : "<none>")})");
            }

            var response = await base.SendAsync(request, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            if (_traceHttp)
            {
                Console.WriteLine($"[TRACE HTTP] <= {(int)response.StatusCode} {response.ReasonPhrase} ({duration.TotalMilliseconds:N0} ms) {request.RequestUri}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var buffer = Internal.Pooling.ArrayPoolManager.RentBytes(4096);
                try
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    int bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                    ErrorResponse? errorResponse = null;
                    try
                    {
                        errorResponse = JsonSerializer.Deserialize(buffer.AsSpan(0, bytesRead), MercadoBitcoinJsonSerializerContext.Default.ErrorResponse);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        var errorString = System.Text.Encoding.UTF8.GetString(buffer.AsSpan(0, bytesRead));
                        errorResponse = new ErrorResponse { Code = "UNKNOWN_ERROR", Message = errorString };
                    }

                    var finalErrorResponse = errorResponse ?? new ErrorResponse { Code = "UNKNOWN_ERROR", Message = "Unknown error" };
                    if (_traceHttp)
                    {
                        Console.WriteLine($"[TRACE HTTP][ERROR] {finalErrorResponse.Code} {finalErrorResponse.Message}");
                    }
                    throw new MercadoBitcoinApiException($"API Error: {finalErrorResponse.Message}", finalErrorResponse);
                }
                finally
                {
                    Internal.Pooling.ArrayPoolManager.ReturnBytes(buffer);
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
