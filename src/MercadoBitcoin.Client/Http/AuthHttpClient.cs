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
        private readonly HttpClient _httpClient;
        private readonly HttpConfiguration _httpConfig;
        
        private static readonly JsonSerializerOptions JsonSerializerOptions = MercadoBitcoinJsonSerializerContext.Default.Options;

        public AuthHttpClient(RetryPolicyConfig? retryConfig = null, HttpConfiguration? httpConfig = null)
        {
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            
            // Create the retry handler with HTTP configuration
            var retryHandler = new RetryHandler(retryConfig, _httpConfig);
            InnerHandler = retryHandler;

            // Create HttpClient with this handler
            _httpClient = new HttpClient(this, false);
            
            // Aplicar configurações HTTP do HttpConfiguration
            _httpClient.DefaultRequestVersion = _httpConfig.HttpVersion;
            _httpClient.DefaultVersionPolicy = _httpConfig.VersionPolicy;
            _httpClient.Timeout = TimeSpan.FromSeconds(_httpConfig.TimeoutSeconds);
        }

        // Método estático para criar com configurações padrão
        public static AuthHttpClient Create<T>()
        {
            return new AuthHttpClient();
        }
        
        // Método estático para criar com configurações personalizadas
        public static AuthHttpClient Create<T>(RetryPolicyConfig? retryConfig, HttpConfiguration? httpConfig = null)
        {
            return new AuthHttpClient(retryConfig, httpConfig);
        }
        
        // Método estático para criar com HTTP/2 otimizado
        public static AuthHttpClient CreateWithHttp2<T>()
        {
            return new AuthHttpClient(null, HttpConfiguration.CreateHttp2Default());
        }
        
        // Método estático para criar otimizado para trading
        public static AuthHttpClient CreateForTrading<T>()
        {
            return new AuthHttpClient(null, HttpConfiguration.CreateTradingOptimized());
        }

        // Classe interna para converter ILogger<T> em ILogger

        public HttpClient HttpClient => _httpClient;

        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
            if (string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
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
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
