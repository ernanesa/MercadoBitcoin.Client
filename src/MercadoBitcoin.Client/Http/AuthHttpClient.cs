using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using MercadoBitcoin.Client.Http;

namespace MercadoBitcoin.Client
{
    public class AuthHttpClient : DelegatingHandler
    {
        private string? _accessToken;
        private readonly HttpConfiguration _httpConfig;
        
        private static readonly JsonSerializerOptions JsonSerializerOptions = MercadoBitcoinJsonSerializerContext.Default.Options;

        public AuthHttpClient(RetryPolicyConfig? retryConfig = null, HttpConfiguration? httpConfig = null)
        {
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            
            // Create the retry handler with HTTP configuration
            var retryHandler = new RetryHandler(retryConfig, _httpConfig);
            InnerHandler = retryHandler;
        }

        /// <summary>
        /// Construtor para uso com IHttpClientFactory (DI)
        /// </summary>
        public AuthHttpClient() : this(null, null)
        {
        }

        /// <summary>
        /// Define o token de acesso para autenticação
        /// </summary>
        /// <param name="accessToken">Token de acesso</param>
        public void SetAccessToken(string? accessToken)
        {
            _accessToken = accessToken;
        }



        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            // If the request already has an Authorization header, don't overwrite it.
            // This is useful for the initial /authorize call.
            if (string.IsNullOrEmpty(request.Headers.Authorization?.Parameter) && !string.IsNullOrEmpty(_accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }


            var response = await base.SendAsync(request, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                ErrorResponse? errorResponse = null;
                try
                {
                   errorResponse = JsonSerializer.Deserialize(responseContent, MercadoBitcoinJsonSerializerContext.Default.ErrorResponse);
                }
                catch (System.Text.Json.JsonException)
                {
                    // If deserialization fails, create a generic error response
                    errorResponse = new ErrorResponse { Code = "UNKNOWN_ERROR", Message = responseContent };
                }

                // Ensure errorResponse is not null before using it
                var finalErrorResponse = errorResponse ?? new ErrorResponse { Code = "UNKNOWN_ERROR", Message = responseContent };
                throw new MercadoBitcoinApiException($"API Error: {finalErrorResponse.Message}", finalErrorResponse);
            }

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            // HttpClient é gerenciado pelo IHttpClientFactory, não precisamos fazer dispose
            base.Dispose(disposing);
        }
    }
}
