using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.WebSocket;
using MercadoBitcoin.Client.WebSocket.Interfaces;
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
        private readonly AuthHttpClient _httpPipeline;
        private readonly MercadoBitcoin.Client.Generated.OpenClient _openClient;
        private readonly Lazy<IWebSocketClient> _webSocketClient;

        /// <summary>
        /// Cliente WebSocket para dados de mercado em tempo real
        /// </summary>
        public IWebSocketClient WebSocket => _webSocketClient.Value;


        public MercadoBitcoinClient(AuthHttpClient? httpClient = null, IWebSocketConfiguration? webSocketConfig = null)
        {
            _httpPipeline = httpClient ?? AuthHttpClient.Create<MercadoBitcoinClient>();

            // The generated client will be initialized after we get the access token.
            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpPipeline.HttpClient) { BaseUrl = "https://api.mercadobitcoin.net/api/v4" };
            // OpenAPI generated another client (OpenClient) which contains some operations like cancel_all_open_orders
            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpPipeline.HttpClient) { BaseUrl = "https://api.mercadobitcoin.net/api/v4" };
            
            // Initialize WebSocket client lazily
            _webSocketClient = new Lazy<IWebSocketClient>(() => 
                new MercadoBitcoinWebSocketClient(webSocketConfig ?? WebSocketConfiguration.CreateProduction()));
        }

        /// <summary>
        /// Construtor para compatibilidade com versões anteriores
        /// </summary>
        public MercadoBitcoinClient() : this(null)
        {
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
                _httpPipeline.SetAccessToken(response.Access_token);

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
                string? name = System.Enum.GetName(value.GetType(), value);
                if (name != null)
                {
                    var enumAttribute = System.Reflection.IntrospectionExtensions.GetTypeInfo(value.GetType()).GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).OfType<System.Reflection.FieldInfo>().Where(f => f.Name == name).FirstOrDefault();
                    if (enumAttribute != null)
                    {
                        var attribute = System.Reflection.CustomAttributeExtensions.GetCustomAttribute(enumAttribute, typeof(System.Runtime.Serialization.EnumMemberAttribute))
                            as System.Runtime.Serialization.EnumMemberAttribute;
                        if (attribute != null)
                        {
                            return attribute.Value ?? name;
                        }
                    }
                }
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
            // Dispose WebSocket client if it was created
            if (_webSocketClient.IsValueCreated)
            {
                _webSocketClient.Value?.Dispose();
            }
            
            _httpPipeline?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}