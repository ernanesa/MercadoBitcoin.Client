using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Http;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization;


namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient : IDisposable
    {
        private readonly MercadoBitcoin.Client.Generated.Client _generatedClient;
        private readonly AuthHttpClient _authHandler;
        private readonly MercadoBitcoin.Client.Generated.OpenClient _openClient;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Construtor para uso com IHttpClientFactory (DI)
        /// </summary>
        /// <param name="httpClient">HttpClient gerenciado pelo IHttpClientFactory</param>
        /// <param name="authHandler">Handler de autenticação</param>
        public MercadoBitcoinClient(HttpClient httpClient, AuthHttpClient authHandler)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authHandler = authHandler ?? throw new ArgumentNullException(nameof(authHandler));

            // The generated client will be initialized with the injected HttpClient
            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpClient) { BaseUrl = "https://api.mercadobitcoin.net/api/v4" };
            // OpenAPI generated another client (OpenClient) which contains some operations like cancel_all_open_orders
            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpClient) { BaseUrl = "https://api.mercadobitcoin.net/api/v4" };

            // Define User-Agent customizado para observabilidade (pode ser sobrescrito externamente)
            if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Environment.GetEnvironmentVariable("MB_USER_AGENT")
                ?? $"MercadoBitcoin.Client/{GetLibraryVersion()} (.NET {Environment.Version.Major}.{Environment.Version.Minor})"))
            {
                // fallback silencioso
            }
        }

        /// <summary>
        /// Construtor para compatibilidade com versões anteriores (não recomendado para aplicações ASP.NET Core)
        /// </summary>
        [Obsolete("Use o construtor com HttpClient e AuthHttpClient para melhor integração com IHttpClientFactory")]
        public MercadoBitcoinClient() : this(CreateLegacyHttpClient(), new AuthHttpClient())
        {
        }

        /// <summary>
        /// Construtor para compatibilidade com versões anteriores (não recomendado para aplicações ASP.NET Core)
        /// </summary>
        [Obsolete("Use o construtor com HttpClient e AuthHttpClient para melhor integração com IHttpClientFactory")]
        public MercadoBitcoinClient(AuthHttpClient? authHandler) : this(CreateLegacyHttpClient(), authHandler ?? new AuthHttpClient())
        {
        }

        private static HttpClient CreateLegacyHttpClient()
        {
            var handler = new AuthHttpClient();
            var httpClient = new HttpClient(handler, false);
            var httpConfig = HttpConfiguration.CreateHttp2Default();

            httpClient.DefaultRequestVersion = httpConfig.HttpVersion;
            httpClient.DefaultVersionPolicy = httpConfig.VersionPolicy;
            httpClient.Timeout = TimeSpan.FromSeconds(httpConfig.TimeoutSeconds);
            httpClient.BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4");

            return httpClient;
        }

        public async Task AuthenticateAsync(string login, string password)
        {


            var authorizeRequest = new AuthorizeRequest
            {
                Login = login,
                Password = password
            };

            try
            {
                var response = await _generatedClient.AuthorizeAsync(authorizeRequest);
                _authHandler.SetAccessToken(response.Access_token);
                // valida formato básico
                if (string.IsNullOrWhiteSpace(response.Access_token))
                {
                    throw new MercadoBitcoinApiException("Token de acesso vazio retornado pela API", new ErrorResponse { Code = "AUTHORIZE|EMPTY_TOKEN", Message = "Access token vazio" });
                }

            }
            catch (Exception)
            {

                throw;
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
                // Evita reflexão pesada para compatibilidade AOT: retorna nome simples do enum.
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
            // HttpClient é gerenciado pelo IHttpClientFactory, não fazemos dispose
            // AuthHttpClient também não precisa de dispose explícito quando usado com DI
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