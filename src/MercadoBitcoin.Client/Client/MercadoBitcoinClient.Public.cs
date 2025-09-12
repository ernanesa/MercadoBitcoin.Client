using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Models;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        /// <summary>
        /// Itera de forma assíncrona sobre todos os depósitos cripto de um usuário, paginando automaticamente.
        /// <para>**Requer autenticação**</para>
        /// </summary>
        /// <param name="accountId">ID da conta</param>
        /// <param name="symbol">Símbolo do ativo (ex: BTC)</param>
        /// <param name="limit">Tamanho da página (padrão: 50, máximo: 50)</param>
        /// <param name="from">Timestamp inicial (opcional)</param>
        /// <param name="to">Timestamp final (opcional)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>IAsyncEnumerable de Deposit</returns>
        public IAsyncEnumerable<Deposit> GetDepositsPagedAsync(
            string accountId,
            string symbol,
            int limit = 50,
            int? from = null,
            int? to = null,
            CancellationToken cancellationToken = default)
        {
            return Internal.AsyncPaginationHelper.PaginateAsync<Deposit>(
                fetchPage: async (pageSize, page, ct) =>
                {
                    await _rateLimiter.WaitAsync(ct);
                    return await _generatedClient.DepositsAsync(
                        accountId,
                        symbol,
                        limit: pageSize.ToString(),
                        page: page.ToString(),
                        from: from?.ToString(),
                        to: to?.ToString(),
                        cancellationToken: ct
                    ).ConfigureAwait(false);
                },
                pageSize: limit,
                startPage: 1,
                cancellationToken: cancellationToken
            );
        }
        #region Public Data

        public Task<AssetFee> GetAssetFeesAsync(string asset, string? network = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.FeesAsync(asset, network, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<OrderBookResponse> GetOrderBookAsync(string symbol, string? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.OrderbookAsync(symbol, limit, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        public Task<ICollection<TradeResponse>> GetTradesAsync(string symbol, int? tid = null, int? since = null, int? from = null, int? to = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.TradesAsync(symbol, tid, since, from, to, limit, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Obtém candles (OHLCV) da API pública, com normalização de entradas e proteção contra janelas invertidas.
        /// <para>**Não requer autenticação**</para>
        /// Parâmetros:
        /// - symbol: par no formato BASE-QUOTE (ex.: BTC-BRL, btcbrl)
        /// - resolution: timeframe (ex.: 1m, 15m, 1h, 1d)
        /// - to: unix timestamp (segundos) do limite direito (inclusivo)
        /// - from: unix timestamp (segundos) do limite esquerdo (opcional)
        /// - countback: quantidade de candles a partir de "to" (prioritário sobre "from")
        /// </summary>
        public Task<ListCandlesResponse> GetCandlesAsync(string symbol, string resolution, int to, int? from = null, int? countback = null, CancellationToken cancellationToken = default)
        {
            if (to <= 0)
                throw new ArgumentException("Parameter 'to' must be a valid unix timestamp (seconds).", nameof(to));

            // Normaliza entradas
            var normalizedSymbol = CandleExtensions.NormalizeSymbol(symbol);
            var normalizedResolution = CandleExtensions.NormalizeResolution(resolution);

            // Se 'from' for informado e maior que 'to', faz swap para evitar janela invertida
            if (from.HasValue && from.Value > to)
            {
                var tmp = to;
                to = from.Value;
                from = tmp;
            }

            try
            {
                return _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Conveniência: busca os últimos N candles até agora (usa countback e ignora "from").
        /// <para>**Não requer autenticação**</para>
        /// </summary>
        public Task<ListCandlesResponse> GetRecentCandlesAsync(string symbol, string resolution, int countback, int? to = null, CancellationToken cancellationToken = default)
        {
            if (countback <= 0) throw new ArgumentOutOfRangeException(nameof(countback));
            var right = to ?? (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var normalizedSymbol = CandleExtensions.NormalizeSymbol(symbol);
            var normalizedResolution = CandleExtensions.NormalizeResolution(resolution);
            try
            {
                return _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from: null, to: right, countback: countback, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Obtém candles com validação e normalização de entradas, retornando lista tipada de CandleData.
        /// <para>**Não requer autenticação**</para>
        /// </summary>
        /// <param name="symbol">Símbolo do par (ex.: BTC-BRL ou btcbrl)</param>
        /// <param name="resolution">Resolução/timeframe (ex.: 1m, 15m, 1h, 1d)</param>
        /// <param name="to">Unix timestamp (segundos) do limite direito (inclusivo)</param>
        /// <param name="from">Unix timestamp (segundos) do limite esquerdo (opcional)</param>
        /// <param name="countback">Quantidade de candles a partir de "to" (prioritário sobre "from")</param>
        /// <returns>Lista de candles mapeada para CandleData</returns>
        public async Task<IReadOnlyList<CandleData>> GetCandlesTypedAsync(string symbol, string resolution, int to, int? from = null, int? countback = null, CancellationToken cancellationToken = default)
        {
            if (!CandleExtensions.IsValidResolution(resolution))
                throw new ArgumentException($"Invalid resolution: {resolution}", nameof(resolution));

            var normalizedSymbol = CandleExtensions.NormalizeSymbol(symbol);
            var normalizedResolution = CandleExtensions.NormalizeResolution(resolution);

            // Protege contra janela invertida
            if (from.HasValue && from.Value > to)
            {
                var tmp = to;
                to = from.Value;
                from = tmp;
            }

            try
            {
                // Chama o cliente gerado com parâmetros normalizados
                var response = await _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback, cancellationToken).ConfigureAwait(false);
                // Converte resposta em lista tipada
                var candles = response.ToCandleDataList(normalizedSymbol, normalizedResolution);
                return candles;
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Sobrecarga prática: busca últimos N candles até agora (usa countback).
        /// <para>**Não requer autenticação**</para>
        /// </summary>
        public Task<IReadOnlyList<CandleData>> GetRecentCandlesTypedAsync(string symbol, string resolution, int countback, CancellationToken cancellationToken = default)
        {
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                return GetCandlesTypedAsync(symbol, resolution, to, from: null, countback: countback, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Obtém a lista de símbolos negociáveis disponíveis na API pública.
        /// <para>**Não requer autenticação**</para>
        /// </summary>
        public Task<ListSymbolInfoResponse> GetSymbolsAsync(string? symbols = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.SymbolsAsync(symbols, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Obtém os tickers atuais para um ou mais símbolos.
        /// <para>**Não requer autenticação**</para>
        /// </summary>
        public Task<ICollection<TickerResponse>> GetTickersAsync(string symbols, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.TickersAsync(symbols, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        /// <summary>
        /// Obtém as redes disponíveis para um ativo (ex: USDC, BTC).
        /// <para>**Não requer autenticação**</para>
        /// </summary>
        public Task<ICollection<Network>> GetAssetNetworksAsync(string asset, CancellationToken cancellationToken = default)
        {
            try
            {
                return _generatedClient.NetworksAsync(asset, cancellationToken);
            }
            catch (Exception ex)
            {
                throw MapApiException(ex);
            }
        }

        #endregion
    }
}
