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

            // 1. AuthenticationHandler (Inner logic handler, handles 401)
            var authenticationHandler = new AuthenticationHandler(tokenStore, clientOptions);

            // 2. AuthHttpClient (Outer handler, adds token, contains RetryHandler)
            // Pass true to enable embedded retry logic for standalone client
            _authHandler = new AuthHttpClient(tokenStore, clientOptions.RetryPolicyConfig, clientOptions.HttpConfiguration, true);

            // Wire: AuthHttpClient -> RetryHandler -> AuthenticationHandler
            if (_authHandler.InnerHandler is DelegatingHandler retryHandler)
            {
                retryHandler.InnerHandler = authenticationHandler;
            }
            else
            {
                _authHandler.InnerHandler = authenticationHandler;
            }

            // 3. SocketsHttpHandler (Bottom)
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = 20
            };

            // Wire: AuthenticationHandler -> SocketsHttpHandler
            authenticationHandler.InnerHandler = socketsHandler;

            _httpClient = new HttpClient(_authHandler)
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
        /// Forces a clock synchronization with the MB server.
        /// Recommended to call at application initialization.
        /// </summary>
        public Task SynchronizeTimeAsync(CancellationToken ct = default) => _timeEstimator.SynchronizeAsync(ct);

        /// <summary>
        /// [BEAST MODE] Executes multiple tasks in parallel (HTTP/2 Multiplexing).
        /// Fires all requests simultaneously without waiting sequentially.
        /// </summary>
        /// <typeparam name="T">Expected result type</typeparam>
        /// <param name="tasks">Collection of tasks to execute</param>
        /// <returns>Results in completion order (or waits for all)</returns>
        public async Task<IEnumerable<T>> ExecuteBatchAsync<T>(IEnumerable<Task<T>> tasks)
        {
            // Materialize the list to fire tasks immediately (Hot Tasks)
            var taskList = tasks.ToList();
            // Wait for all to complete (Success or Failure)
            // In HTTP/2, this sends multiple frames on the same TCP connection
            await Task.WhenAll(taskList);

            // Returns the results of successful ones (or throws the first exception if fail-fast is preferred)
            // Here we choose to return everything, assuming the caller will handle individual exceptions if needed
            return taskList.Select(t => t.Result);
        }

        /// <summary>
        /// Helper method to get the corrected timestamp for signatures (internal/advanced use).
        /// </summary>
        public long GetCurrentTimestamp() => _timeEstimator.GetCorrectedUnixTimeSeconds();
    }
}