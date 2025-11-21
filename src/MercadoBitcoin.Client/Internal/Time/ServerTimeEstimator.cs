using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MercadoBitcoin.Client.Internal.Time
{
    /// <summary>
    /// Responsável por estimar e corrigir o desvio de tempo (drift) entre o cliente e o servidor do Mercado Bitcoin.
    /// </summary>
    public class ServerTimeEstimator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger? _logger;
        private TimeSpan _timeOffset = TimeSpan.Zero;
        private bool _isSynchronized = false;
        private readonly object _lock = new();

        public ServerTimeEstimator(HttpClient httpClient, ILogger? logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
        }

        /// <summary>
        /// Obtém o horário atual corrigido (Server Time estimado).
        /// </summary>
        public DateTimeOffset GetCorrectedTime()
        {
            return DateTimeOffset.UtcNow.Add(_timeOffset);
        }

        /// <summary>
        /// Obtém o Unix Timestamp atual corrigido em segundos.
        /// </summary>
        public long GetCorrectedUnixTimeSeconds()
        {
            return GetCorrectedTime().ToUnixTimeSeconds();
        }

        /// <summary>
        /// Sincroniza o relógio local com o servidor do Mercado Bitcoin.
        /// Dispara uma requisição leve para calcular a latência e o delta do header 'Date'.
        /// </summary>
        public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Usamos um endpoint leve público para "pingar" o servidor
                // O header 'Date' é padrão HTTP e reflete o relógio do servidor
                var sw = Stopwatch.StartNew();
                var request = new HttpRequestMessage(HttpMethod.Head, "https://api.mercadobitcoin.net/api/v4/symbols");
                
                // Evita cache para garantir timestamp real
                request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                sw.Stop();

                if (response.Headers.Date.HasValue)
                {
                    var serverDate = response.Headers.Date.Value;
                    var localNow = DateTimeOffset.UtcNow;
                    
                    // O 'Date' HTTP tem precisão de segundos. Adicionamos metade do RTT (Round Trip Time) para ajustar a latência.
                    var latencyAdjustment = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds / 2);
                    var estimatedServerTime = serverDate.Add(latencyAdjustment);

                    lock (_lock)
                    {
                        _timeOffset = estimatedServerTime - localNow;
                        _isSynchronized = true;
                    }

                    _logger?.LogInformation("Relógio sincronizado. Offset: {Offset}ms. Latência: {Latency}ms", 
                        _timeOffset.TotalMilliseconds, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Falha ao sincronizar tempo com o servidor. Usando horário local.");
                // Em caso de falha, mantemos o offset anterior ou Zero (fallback seguro)
            }
        }
    }
}
