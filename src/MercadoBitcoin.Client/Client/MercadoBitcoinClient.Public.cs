using MercadoBitcoin.Client.Generated;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Models;

using System;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Public Data

        public Task<AssetFee> GetAssetFeesAsync(string asset, string? network = null)
        {
            return _generatedClient.FeesAsync(asset, network);
        }

        public Task<OrderBookResponse> GetOrderBookAsync(string symbol, string? limit = null)
        {
            return _generatedClient.OrderbookAsync(symbol, limit);
        }

        public Task<ICollection<TradeResponse>> GetTradesAsync(string symbol, int? tid = null, int? since = null, int? from = null, int? to = null, int? limit = null)
        {
            return _generatedClient.TradesAsync(symbol, tid, since, from, to, limit);
        }

        /// <summary>
        /// Obtém candles (OHLCV) da API pública, com normalização de entradas e proteção contra janelas invertidas.
        /// Parâmetros:
        /// - symbol: par no formato BASE-QUOTE (ex.: BTC-BRL, btcbrl)
        /// - resolution: timeframe (ex.: 1m, 15m, 1h, 1d)
        /// - to: unix timestamp (segundos) do limite direito (inclusivo)
        /// - from: unix timestamp (segundos) do limite esquerdo (opcional)
        /// - countback: quantidade de candles a partir de "to" (prioritário sobre "from")
        /// </summary>
        public Task<ListCandlesResponse> GetCandlesAsync(string symbol, string resolution, int to, int? from = null, int? countback = null)
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

            return _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback);
        }

        /// <summary>
        /// Conveniência: busca os últimos N candles até agora (usa countback e ignora "from").
        /// </summary>
        public Task<ListCandlesResponse> GetRecentCandlesAsync(string symbol, string resolution, int countback, int? to = null)
        {
            if (countback <= 0) throw new ArgumentOutOfRangeException(nameof(countback));
            var right = to ?? (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var normalizedSymbol = CandleExtensions.NormalizeSymbol(symbol);
            var normalizedResolution = CandleExtensions.NormalizeResolution(resolution);
            return _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from: null, to: right, countback: countback);
        }

        /// <summary>
        /// Obtém candles com validação e normalização de entradas, retornando lista tipada de CandleData.
        /// </summary>
        /// <param name="symbol">Símbolo do par (ex.: BTC-BRL ou btcbrl)</param>
        /// <param name="resolution">Resolução/timeframe (ex.: 1m, 15m, 1h, 1d)</param>
        /// <param name="to">Unix timestamp (segundos) do limite direito (inclusivo)</param>
        /// <param name="from">Unix timestamp (segundos) do limite esquerdo (opcional)</param>
        /// <param name="countback">Quantidade de candles a partir de "to" (prioritário sobre "from")</param>
        /// <returns>Lista de candles mapeada para CandleData</returns>
        public async Task<IReadOnlyList<CandleData>> GetCandlesTypedAsync(string symbol, string resolution, int to, int? from = null, int? countback = null)
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

            // Chama o cliente gerado com parâmetros normalizados
            var response = await _generatedClient.CandlesAsync(normalizedSymbol, normalizedResolution, from, to, countback).ConfigureAwait(false);

            // Converte resposta em lista tipada
            var candles = response.ToCandleDataList(normalizedSymbol, normalizedResolution);
            return candles;
        }

        /// <summary>
        /// Sobrecarga prática: busca últimos N candles até agora (usa countback).
        /// </summary>
        public Task<IReadOnlyList<CandleData>> GetRecentCandlesTypedAsync(string symbol, string resolution, int countback)
        {
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return GetCandlesTypedAsync(symbol, resolution, to, from: null, countback: countback);
        }

        public Task<ListSymbolInfoResponse> GetSymbolsAsync(string? symbols = null)
        {
            return _generatedClient.SymbolsAsync(symbols);
        }

        public Task<ICollection<TickerResponse>> GetTickersAsync(string symbols)
        {
            return _generatedClient.TickersAsync(symbols);
        }

        public Task<ICollection<Network>> GetAssetNetworksAsync(string asset)
        {
            return _generatedClient.NetworksAsync(asset);
        }

        #endregion
    }
}
