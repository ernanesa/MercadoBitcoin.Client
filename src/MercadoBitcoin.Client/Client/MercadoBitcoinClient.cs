using System.Text.Json;
using System.Threading.RateLimiting;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Internal.Optimization;
using MercadoBitcoin.Client.Internal.Security;
using MercadoBitcoin.Client.Internal.Time;
using Microsoft.Extensions.Caching.Memory;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient : IDisposable
    {
        private readonly TokenBucketRateLimiter _rateLimiter;
        private readonly AuthHttpClient? _authHandler;
        private readonly HttpClient _httpClient;
        private readonly Generated.Client _generatedClient;
        private readonly Generated.OpenClient _openClient;
        private readonly ServerTimeEstimator _timeEstimator;
        private readonly IMemoryCache? _cache;
        private readonly RequestCoalescer _coalescer = new();
        private readonly MercadoBitcoinClientOptions _options;
        private readonly IMercadoBitcoinCredentialProvider? _credentialProvider;

        /// <summary>
        /// Constructor for use with DI, allowing real injection of configuration options.
        /// </summary>
        /// <param name="httpClient">HttpClient configured by IHttpClientFactory</param>
        /// <param name="options">Configuration options</param>
        /// <param name="credentialProvider">Optional credential provider for multi-user scenarios</param>
        /// <param name="cache">Optional memory cache for L1 caching</param>
        public MercadoBitcoinClient(
            HttpClient httpClient,
            Microsoft.Extensions.Options.IOptionsSnapshot<MercadoBitcoinClientOptions> options,
            IMercadoBitcoinCredentialProvider? credentialProvider = null,
            Microsoft.Extensions.Caching.Memory.IMemoryCache? cache = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _credentialProvider = credentialProvider;
            _cache = cache;

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new ArgumentException("BaseUrl cannot be null or empty.", nameof(_options.BaseUrl));
            }

            _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = _options.RateLimiterConfig.PermitLimit,
                QueueLimit = _options.RateLimiterConfig.QueueLimit,
                ReplenishmentPeriod = _options.RateLimiterConfig.ReplenishmentPeriod,
                TokensPerPeriod = _options.RateLimiterConfig.TokensPerPeriod,
                AutoReplenishment = _options.RateLimiterConfig.AutoReplenishment
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
            if (_options.JsonSerializerContext != null)
            {
                jsonOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
                    _options.JsonSerializerContext,
                    internalContext
                );
            }
            else
            {
                jsonOptions.TypeInfoResolver = internalContext;
            }

            // Apply custom configuration
            _options.ConfigureJsonOptions?.Invoke(jsonOptions);

            _generatedClient = new Generated.Client(_httpClient) { BaseUrl = _options.BaseUrl };
            _generatedClient.SetSerializerOptions(jsonOptions);

            _openClient = new Generated.OpenClient(_httpClient) { BaseUrl = _options.BaseUrl };
            _openClient.SetSerializerOptions(jsonOptions);
        }

        /// <summary>
        /// Manual constructor for standalone usage (without DI).
        /// </summary>
        /// <param name="clientOptions">Configuration options</param>
        /// <param name="credentialProvider">Optional credential provider for multi-user scenarios</param>
        /// <param name="cache">Optional memory cache for L1 caching</param>
        public MercadoBitcoinClient(MercadoBitcoinClientOptions clientOptions, IMercadoBitcoinCredentialProvider? credentialProvider = null, IMemoryCache? cache = null)
        {
            _options = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _credentialProvider = credentialProvider;
            _cache = cache;

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new ArgumentException("BaseUrl cannot be null or empty.", nameof(_options.BaseUrl));
            }

            _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = _options.RateLimiterConfig.PermitLimit,
                QueueLimit = _options.RateLimiterConfig.QueueLimit,
                ReplenishmentPeriod = _options.RateLimiterConfig.ReplenishmentPeriod,
                TokensPerPeriod = _options.RateLimiterConfig.TokensPerPeriod,
                AutoReplenishment = _options.RateLimiterConfig.AutoReplenishment
            });

            // Manual composition of the HTTP pipeline
            var tokenStore = new Internal.Security.TokenStore();

            // 1. AuthenticationHandler (Inner logic handler, handles 401)
            var authenticationHandler = new AuthenticationHandler(tokenStore, _options, _credentialProvider);

            // 2. AuthHttpClient (Outer handler, adds token, contains RetryHandler)
            // Pass true to enable embedded retry logic for standalone client
            _authHandler = new AuthHttpClient(tokenStore, _options.RetryPolicyConfig, _options.HttpConfiguration, true);

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
                MaxConnectionsPerServer = 50,
                EnableMultipleHttp2Connections = true,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests
            };

            // Wire: AuthenticationHandler -> SocketsHttpHandler
            authenticationHandler.InnerHandler = socketsHandler;

            _httpClient = new HttpClient(_authHandler)
            {
                BaseAddress = new Uri(_options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
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
            if (_options.JsonSerializerContext != null)
            {
                jsonOptions.TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
                    _options.JsonSerializerContext,
                    internalContext
                );
            }
            else
            {
                jsonOptions.TypeInfoResolver = internalContext;
            }

            // Apply custom configuration
            _options.ConfigureJsonOptions?.Invoke(jsonOptions);

            _generatedClient = new Generated.Client(_httpClient) { BaseUrl = _options.BaseUrl };
            _generatedClient.SetSerializerOptions(jsonOptions);

            _openClient = new Generated.OpenClient(_httpClient) { BaseUrl = _options.BaseUrl };
            _openClient.SetSerializerOptions(jsonOptions);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _httpClient?.Dispose();
            _rateLimiter?.Dispose();
            _coalescer?.Dispose();
        }

        /// <summary>
        /// Returns the current access token (for diagnostics / debug only).
        /// </summary>
        public string? GetAccessToken() => _authHandler?.GetAccessToken();

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

        private async Task<T> ExecuteCachedAsync<T>(string cacheKey, Func<CancellationToken, Task<T>> action, CancellationToken ct, TimeSpan? expiration = null)
        {
            if (_cache == null || !_options.CacheConfig.EnableL1Cache)
            {
                if (_options.CacheConfig.EnableRequestCoalescing)
                {
                    return await _coalescer.ExecuteAsync(cacheKey, action, ct).ConfigureAwait(false);
                }
                return await action(ct).ConfigureAwait(false);
            }

            if (_cache.TryGetValue(cacheKey, out T? cachedResult))
            {
                return cachedResult!;
            }

            T result;
            if (_options.CacheConfig.EnableRequestCoalescing)
            {
                result = await _coalescer.ExecuteAsync(cacheKey, action, ct).ConfigureAwait(false);
            }
            else
            {
                result = await action(ct).ConfigureAwait(false);
            }

            if (result != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? _options.CacheConfig.DefaultL1Expiration
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }
            else if (_options.CacheConfig.EnableNegativeCaching)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.CacheConfig.NegativeCacheExpiration
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            return result!;
        }
    }
}