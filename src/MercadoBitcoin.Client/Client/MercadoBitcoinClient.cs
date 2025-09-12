using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Http;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;


namespace MercadoBitcoin.Client

{
    using System.Text.Json;

    public partial class MercadoBitcoinClient : IDisposable
    {
        private readonly Internal.AsyncRateLimiter _rateLimiter;
        private readonly MercadoBitcoin.Client.Generated.Client _generatedClient;
        private readonly AuthHttpClient _authHandler;
        private readonly MercadoBitcoin.Client.Generated.OpenClient _openClient;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Construtor para uso com DI, permitindo injeção real de opções de configuração.
        /// </summary>
        /// <param name="httpClient">HttpClient gerenciado pelo IHttpClientFactory</param>
        /// <param name="authHandler">Handler de autenticação</param>
        /// <param name="options">Opções de configuração injetadas</param>
        public MercadoBitcoinClient(HttpClient httpClient, AuthHttpClient authHandler, Microsoft.Extensions.Options.IOptions<Configuration.MercadoBitcoinClientOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authHandler = authHandler ?? throw new ArgumentNullException(nameof(authHandler));
            var opts = options?.Value ?? new Configuration.MercadoBitcoinClientOptions();
            _rateLimiter = new Internal.AsyncRateLimiter(opts.RequestsPerSecond);
            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpClient) { BaseUrl = opts.BaseUrl };
            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpClient) { BaseUrl = opts.BaseUrl };
            if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Environment.GetEnvironmentVariable("MB_USER_AGENT")
                ?? $"MercadoBitcoin.Client/{GetLibraryVersion()} (.NET {Environment.Version.Major}.{Environment.Version.Minor})"))
            {
                // fallback silencioso
            }
        }

        /// <summary>
        /// Mapeia ApiException do client gerado para exceções ricas MercadoBitcoin.
        /// </summary>
        private static Exception MapApiException(Exception ex)
        {
            if (ex is MercadoBitcoinApiException)
                return ex;
            if (ex is Generated.ApiException apiEx)
            {
                ErrorResponse? error = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(apiEx.Response))
                        error = JsonSerializer.Deserialize(apiEx.Response, MercadoBitcoinJsonSerializerContext.Default.ErrorResponse);
                }
                catch { /* fallback para null */ }
                var code = apiEx.StatusCode;
                if (code == 401 || code == 403)
                    return new MercadoBitcoinUnauthorizedException(apiEx.Message, error ?? new ErrorResponse { Code = $"HTTP_{code}", Message = apiEx.Message });
                if (code == 400)
                    return new MercadoBitcoinValidationException(apiEx.Message, error ?? new ErrorResponse { Code = $"HTTP_{code}", Message = apiEx.Message });
                if (code == 429)
                    return new MercadoBitcoinRateLimitException(apiEx.Message, error ?? new ErrorResponse { Code = $"HTTP_{code}", Message = apiEx.Message });
                return new MercadoBitcoinApiException(apiEx.Message, error ?? new ErrorResponse { Code = $"HTTP_{code}", Message = apiEx.Message });
            }
            return ex;
        }

        /// <summary>
        /// Construtor para uso com IHttpClientFactory (DI)
        /// </summary>
        /// <param name="httpClient">HttpClient gerenciado pelo IHttpClientFactory</param>
        /// <param name="authHandler">Handler de autenticação</param>
        public MercadoBitcoinClient(HttpClient httpClient, AuthHttpClient authHandler)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authHandler = authHandler ?? throw new ArgumentNullException(nameof(authHandler));


            // Usa sempre as opções padrão (pode ser expandido para DI no futuro)
            var options = new Configuration.MercadoBitcoinClientOptions();
            _rateLimiter = new Internal.AsyncRateLimiter(options.RequestsPerSecond);
            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpClient) { BaseUrl = options.BaseUrl };
            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpClient) { BaseUrl = options.BaseUrl };

            if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Environment.GetEnvironmentVariable("MB_USER_AGENT")
                ?? $"MercadoBitcoin.Client/{GetLibraryVersion()} (.NET {Environment.Version.Major}.{Environment.Version.Minor})"))
            {
                // fallback silencioso
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
                await _rateLimiter.WaitAsync();
                var response = await _generatedClient.AuthorizeAsync(authorizeRequest);
                _authHandler.SetAccessToken(response.Access_token);
                // valida formato básico
                if (string.IsNullOrWhiteSpace(response.Access_token))
                {
                    throw new MercadoBitcoinApiException("Token de acesso vazio retornado pela API", new ErrorResponse { Code = "AUTHORIZE|EMPTY_TOKEN", Message = "Access token vazio" });
                }
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        // We will expose the public and private methods here

        private string? ConvertToString(object? value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
            {
                return null;
            }

            if (value is System.Enum)
            {
                return value.ToString();
            }
            else if (value is bool)
            {
                return System.Convert.ToString(value, cultureInfo)?.ToLowerInvariant();
            }
            else if (value is byte[] bytes)
            {
                return System.Convert.ToBase64String(bytes);
            }
            else if (value.GetType().IsArray)
            {
                var array = ((System.Array)value).OfType<object?>();
                return string.Join(",", System.Linq.Enumerable.Select(array, o => ConvertToString(o, cultureInfo)));
            }

            return System.Convert.ToString(value, cultureInfo);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Retorna o token de acesso atual (apenas para diagnóstico / debug).
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