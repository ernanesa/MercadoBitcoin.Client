using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Internal.RateLimiting;
using MercadoBitcoin.Client.Errors;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using Microsoft.Extensions.ObjectPool;

namespace MercadoBitcoin.Client

{
    using System.Text.Json;

    public partial class MercadoBitcoinClient : IDisposable
    {
        private static readonly ObjectPool<ErrorResponse> _errorResponsePool = new DefaultObjectPoolProvider().Create<ErrorResponse>();
        private readonly TokenBucketRateLimiter _rateLimiter;
        private readonly MercadoBitcoin.Client.Generated.Client _generatedClient;
        private readonly AuthHttpClient _authHandler;
        private readonly MercadoBitcoin.Client.Generated.OpenClient _openClient;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Constructor for use with DI, allowing real injection of configuration options.
        /// </summary>
        /// <param name="httpClient">HttpClient managed by IHttpClientFactory</param>
        /// <param name="authHandler">Authentication handler</param>
        /// <param name="options">Injected configuration options</param>
        public MercadoBitcoinClient(HttpClient httpClient, AuthHttpClient authHandler, Microsoft.Extensions.Options.IOptions<Configuration.MercadoBitcoinClientOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authHandler = authHandler ?? throw new ArgumentNullException(nameof(authHandler));
            var clientOptions = options?.Value ?? new Configuration.MercadoBitcoinClientOptions();
            _rateLimiter = RateLimiterFactory.CreateTokenBucket(clientOptions.RequestsPerSecond);
            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpClient) { BaseUrl = clientOptions.BaseUrl };
            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpClient) { BaseUrl = clientOptions.BaseUrl };
            if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Environment.GetEnvironmentVariable("MB_USER_AGENT")
                ?? $"MercadoBitcoin.Client/{GetLibraryVersion()} (.NET {Environment.Version.Major}.{Environment.Version.Minor})"))
            {
                // silent fallback
            }
        }

        /// <summary>
        /// Maps ApiException from the generated client to rich MercadoBitcoin exceptions.
        /// </summary>
        private static Exception MapApiException(Exception exception)
        {
            if (exception is MercadoBitcoinApiException)
                return exception;
            if (exception is Generated.ApiException apiException)
            {
                string errorCode = string.Empty;
                string errorMessage = apiException.Message;

                if (!string.IsNullOrWhiteSpace(apiException.Response))
                {
                    // Use pooled object for parsing to avoid allocation
                    var errorResponse = _errorResponsePool.Get();
                    try
                    {
                        errorResponse.Reset();
                        // Manual parsing or using JsonSerializer on the pooled object?
                        // JsonSerializer.Deserialize creates a new object.
                        // We use JsonDocument to parse without allocating the target object, then populate.
                        using (var doc = JsonDocument.Parse(apiException.Response))
                        {
                            if (doc.RootElement.TryGetProperty("code", out var codeProp))
                                errorResponse.Code = codeProp.GetString() ?? string.Empty;
                            if (doc.RootElement.TryGetProperty("message", out var msgProp))
                                errorResponse.Message = msgProp.GetString() ?? string.Empty;
                        }

                        errorCode = errorResponse.Code;
                        if (!string.IsNullOrWhiteSpace(errorResponse.Message))
                        {
                            errorMessage = errorResponse.Message;
                        }
                    }
                    catch
                    {
                        // Fallback: use default values
                        errorCode = $"HTTP_{apiException.StatusCode}";
                    }
                    finally
                    {
                        _errorResponsePool.Return(errorResponse);
                    }
                }
                else
                {
                    errorCode = $"HTTP_{apiException.StatusCode}";
                }

                var code = apiException.StatusCode;
                if (code == 401 || code == 403)
                    return new MercadoBitcoinUnauthorizedException(apiException.Message, errorCode, errorMessage);
                if (code == 400)
                    return new MercadoBitcoinValidationException(apiException.Message, errorCode, errorMessage);
                if (code == 429)
                    return new MercadoBitcoinRateLimitException(apiException.Message, errorCode, errorMessage);

                return new MercadoBitcoinApiException(apiException.Message, errorCode, errorMessage);
            }
            return exception;
        }

        /// <summary>
        /// Constructor for use with IHttpClientFactory (DI)
        /// </summary>
        /// <param name="httpClient">HttpClient managed by IHttpClientFactory</param>
        /// <param name="authHandler">Authentication handler</param>
        public MercadoBitcoinClient(HttpClient httpClient, AuthHttpClient authHandler)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authHandler = authHandler ?? throw new ArgumentNullException(nameof(authHandler));


            // Always uses default options (can be expanded for DI in the future)
            var options = new Configuration.MercadoBitcoinClientOptions();
            _rateLimiter = RateLimiterFactory.CreateTokenBucket(options.RequestsPerSecond);
            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpClient) { BaseUrl = options.BaseUrl };
            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpClient) { BaseUrl = options.BaseUrl };

            if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Environment.GetEnvironmentVariable("MB_USER_AGENT")
                ?? $"MercadoBitcoin.Client/{GetLibraryVersion()} (.NET {Environment.Version.Major}.{Environment.Version.Minor})"))
            {
                // silent fallback
            }
        }

        // ...existing code...

        public async Task AuthenticateAsync(string login, string password)
        {


            var authorizeRequest = new AuthorizeRequest
            {
                Login = login,
                Password = password
            };

            try
            {
                using var lease = await _rateLimiter.AcquireAsync(1);
                if (!lease.IsAcquired)
                {
                    throw new MercadoBitcoinRateLimitException("Rate limit exceeded (client-side).", new ErrorResponse { Code = "CLIENT_RATE_LIMIT", Message = "Rate limit exceeded (client-side)." });
                }
                var response = await _generatedClient.AuthorizeAsync(authorizeRequest);
                _authHandler.SetAccessToken(response.Access_token);
                // basic format validation
                if (string.IsNullOrWhiteSpace(response.Access_token))
                {
                    throw new MercadoBitcoinApiException("Empty access token returned by API", new ErrorResponse { Code = "AUTHORIZE|EMPTY_TOKEN", Message = "Empty access token" });
                }
            }
            catch (Exception exception)
            {
                throw MapApiException(exception);
            }
        }

        // We will expose the public and private methods here

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the current access token (for diagnostics / debug only).
        /// </summary>
        public string? GetAccessToken() => _authHandler.GetAccessToken();

        private static string GetLibraryVersion()
        {
            try
            {
                return typeof(MercadoBitcoinClient).Assembly.GetName().Version?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}