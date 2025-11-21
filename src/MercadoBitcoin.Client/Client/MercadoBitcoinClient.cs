using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Internal.RateLimiting;
using MercadoBitcoin.Client.Internal.Time;
using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using Microsoft.Extensions.ObjectPool;
using System.Text.Json;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient : IDisposable
    {
        private static readonly ObjectPool<ErrorResponse> _errorResponsePool = new DefaultObjectPoolProvider().Create<ErrorResponse>();
        private readonly TokenBucketRateLimiter _rateLimiter;
        private readonly AuthHttpClient? _authHandler;
        private readonly HttpClient _httpClient;
        private readonly MercadoBitcoin.Client.Generated.Client _generatedClient;
        private readonly MercadoBitcoin.Client.Generated.OpenClient _openClient;
        private readonly ServerTimeEstimator _timeEstimator;

        /// <summary>
        /// Constructor for use with DI, allowing real injection of configuration options.
        /// </summary>
        /// <param name="httpClient">HttpClient configured by IHttpClientFactory</param>
        /// <param name="options">Configuration options</param>
        public MercadoBitcoinClient(HttpClient httpClient, MercadoBitcoinClientOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            var clientOptions = options ?? new MercadoBitcoinClientOptions();

            if (string.IsNullOrWhiteSpace(clientOptions.BaseUrl))
            {
                throw new ArgumentException("BaseUrl cannot be null or empty.", nameof(clientOptions.BaseUrl));
            }

            _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = clientOptions.RateLimiterConfig.PermitLimit,
                QueueLimit = clientOptions.RateLimiterConfig.QueueLimit,
                ReplenishmentPeriod = clientOptions.RateLimiterConfig.ReplenishmentPeriod,
                TokensPerPeriod = clientOptions.RateLimiterConfig.TokensPerPeriod,
                AutoReplenishment = clientOptions.RateLimiterConfig.AutoReplenishment
            });

            // Initialize ServerTimeEstimator
            _timeEstimator = new ServerTimeEstimator(_httpClient, null);

            // Configure JSON Serialization (Default)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            };

            // Add FastDecimalConverter
            jsonOptions.Converters.Add(new Internal.Converters.FastDecimalConverter());

            // Combine internal context with user provided context if any
            var internalContext = MercadoBitcoinJsonSerializerContext.Default;
            if (clientOptions.JsonSerializerContext != null)
            {
                jsonOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
                    clientOptions.JsonSerializerContext,
                    internalContext
                );
            }
            else
            {
                jsonOptions.TypeInfoResolver = internalContext;
            }

            // Apply custom configuration
            clientOptions.ConfigureJsonOptions?.Invoke(jsonOptions);

            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpClient) { BaseUrl = clientOptions.BaseUrl };
            _generatedClient.SetSerializerOptions(jsonOptions);

            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpClient) { BaseUrl = clientOptions.BaseUrl };
            _openClient.SetSerializerOptions(jsonOptions);
        }

        /// <summary>
        /// Manual constructor for standalone usage (without DI).
        /// </summary>
        /// <param name="clientOptions">Configuration options</param>
        public MercadoBitcoinClient(MercadoBitcoinClientOptions clientOptions)
        {
            if (clientOptions == null)
            {
                throw new ArgumentNullException(nameof(clientOptions));
            }

            if (string.IsNullOrWhiteSpace(clientOptions.BaseUrl))
            {
                throw new ArgumentException("BaseUrl cannot be null or empty.", nameof(clientOptions.BaseUrl));
            }

            _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = clientOptions.RateLimiterConfig.PermitLimit,
                QueueLimit = clientOptions.RateLimiterConfig.QueueLimit,
                ReplenishmentPeriod = clientOptions.RateLimiterConfig.ReplenishmentPeriod,
                TokensPerPeriod = clientOptions.RateLimiterConfig.TokensPerPeriod,
                AutoReplenishment = clientOptions.RateLimiterConfig.AutoReplenishment
            });

            // Manual composition of the HTTP pipeline
            var tokenStore = new Internal.Security.TokenStore();
            
            // 1. AuthHttpClient (Inner-most delegating handler, adds token)
            _authHandler = new AuthHttpClient(tokenStore, clientOptions.RetryPolicyConfig, clientOptions.HttpConfiguration);
            
            // 2. AuthenticationHandler (Outer delegating handler, handles 401)
            var authHandler = new AuthenticationHandler(tokenStore, clientOptions);
            authHandler.InnerHandler = _authHandler;

            // 3. SocketsHttpHandler (Bottom)
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = 20
            };
            
            // Wire up the chain: AuthHandler -> AuthHttpClient -> RetryHandler -> SocketsHttpHandler
            // Note: AuthHttpClient wraps RetryHandler internally.
            if (_authHandler.InnerHandler is DelegatingHandler retryHandler)
            {
                retryHandler.InnerHandler = socketsHandler;
            }
            else
            {
                _authHandler.InnerHandler = socketsHandler;
            }

            _httpClient = new HttpClient(authHandler)
            {
                BaseAddress = new Uri(clientOptions.BaseUrl),
                Timeout = TimeSpan.FromSeconds(clientOptions.TimeoutSeconds)
            };

            // Initialize ServerTimeEstimator
            _timeEstimator = new ServerTimeEstimator(_httpClient, null);

            // Configure JSON Serialization (Default)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            };

            // Add FastDecimalConverter
            jsonOptions.Converters.Add(new Internal.Converters.FastDecimalConverter());

            // Combine internal context with user provided context if any
            var internalContext = MercadoBitcoinJsonSerializerContext.Default;
            if (clientOptions.JsonSerializerContext != null)
            {
                jsonOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
                    clientOptions.JsonSerializerContext,
                    internalContext
                );
            }
            else
            {
                jsonOptions.TypeInfoResolver = internalContext;
            }

            // Apply custom configuration
            clientOptions.ConfigureJsonOptions?.Invoke(jsonOptions);

            _generatedClient = new MercadoBitcoin.Client.Generated.Client(_httpClient) { BaseUrl = clientOptions.BaseUrl };
            _generatedClient.SetSerializerOptions(jsonOptions);

            _openClient = new MercadoBitcoin.Client.Generated.OpenClient(_httpClient) { BaseUrl = clientOptions.BaseUrl };
            _openClient.SetSerializerOptions(jsonOptions);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _httpClient?.Dispose();
            _rateLimiter?.Dispose();
        }

        /// <summary>
        /// Returns the current access token (for diagnostics / debug only).
        /// </summary>
        public string? GetAccessToken() => _authHandler?.GetAccessToken();

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

        private Exception MapApiException(Exception ex)
        {
            if (ex is ApiException apiEx)
            {
                return new MercadoBitcoinException(apiEx.Message, apiEx.StatusCode, apiEx.Response, apiEx.Headers, apiEx);
            }
            return new MercadoBitcoinException(ex.Message, 0, null, null, ex);
        }

        /// <summary>
        /// Força uma sincronização de relógio com o servidor do MB.
        /// Recomendado chamar na inicialização da aplicação.
        /// </summary>
        public Task SynchronizeTimeAsync(CancellationToken ct = default) => _timeEstimator.SynchronizeAsync(ct);

        /// <summary>
        /// [BEAST MODE] Executa múltiplas tarefas em paralelo (HTTP/2 Multiplexing).
        /// Dispara todas as requisições simultaneamente sem aguardar sequencialmente.
        /// </summary>
        /// <typeparam name="T">Tipo do resultado esperado</typeparam>
        /// <param name="tasks">Coleção de tarefas a executar</param>
        /// <returns>Resultados na ordem de conclusão (ou aguarda todas)</returns>
        public async Task<IEnumerable<T>> ExecuteBatchAsync<T>(IEnumerable<Task<T>> tasks)
        {
            // Materializa a lista para disparar as tasks imediatamente (Hot Tasks)
            var taskList = tasks.ToList();
                        // Aguarda todas completarem (Sucesso ou Falha)
            // Em HTTP/2, isso envia múltiplos frames na mesma conexão TCP
            await Task.WhenAll(taskList);

            // Retorna os resultados das que tiveram sucesso (ou lança a primeira exceção se preferir fail-fast)
            // Aqui optamos por retornar tudo, assumindo que o caller tratará exceções individuais se necessário
            return taskList.Select(t => t.Result);
        }

        /// <summary>
        /// Método auxiliar para obter o timestamp corrigido para assinaturas (uso interno/avançado).
        /// </summary>
        public long GetCurrentTimestamp() => _timeEstimator.GetCorrectedUnixTimeSeconds();
    }
}