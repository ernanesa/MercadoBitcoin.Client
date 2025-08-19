using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Handler que implementa retry policies com Polly para melhorar a robustez das chamadas HTTP
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private readonly RetryPolicyConfig _config;
        private readonly HttpConfiguration _httpConfig;

        public RetryHandler(RetryPolicyConfig? config = null, HttpConfiguration? httpConfig = null)
        {
            _config = config ?? new RetryPolicyConfig();
            _httpConfig = httpConfig ?? HttpConfiguration.CreateHttp2Default();
            _retryPolicy = CreateRetryPolicy();
            
            // Configurar HttpClientHandler com as configurações HTTP
            var handler = new HttpClientHandler()
            {
                MaxConnectionsPerServer = _httpConfig.MaxConnectionsPerServer
            };
            
            // Configurar compressão se habilitada
            if (_httpConfig.EnableCompression)
            {
                handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
            }
            
            InnerHandler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Aplicar configurações HTTP da configuração
            request.Version = _httpConfig.HttpVersion;
            request.VersionPolicy = _httpConfig.VersionPolicy;
            
            // Configurar timeout se especificado
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_httpConfig.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await base.SendAsync(request, combinedCts.Token);

                return response;
            });
        }

        private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            var policyBuilder = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>(ex => _config.RetryOnTimeout)
                .OrResult<HttpResponseMessage>(r => ShouldRetry(r));

            return policyBuilder.WaitAndRetryAsync(
                retryCount: _config.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => _config.CalculateDelay(retryAttempt),
                onRetry: (outcome, timespan, retryCount, context) =>
                {

                });
        }

        private bool ShouldRetry(HttpResponseMessage response)
        {
            return (_config.RetryOnTimeout && response.StatusCode == HttpStatusCode.RequestTimeout) ||
                   (_config.RetryOnRateLimit && response.StatusCode == HttpStatusCode.TooManyRequests) ||
                   (_config.RetryOnServerErrors && (
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.BadGateway ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.GatewayTimeout));
        }
    }
}