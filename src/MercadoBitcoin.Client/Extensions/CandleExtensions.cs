using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Models;


namespace MercadoBitcoin.Client.Extensions
{
    /// <summary>
    /// Extensões para trabalhar com dados de candles
    /// </summary>
    public static class CandleExtensions
    {
        private static readonly Dictionary<string, string> ResolutionMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Mapeamento de diferentes formatos para o formato padrão da API
            { "1m", "1m" },
            { "1min", "1m" },
            { "1minute", "1m" },
            { "5m", "5m" },
            { "5min", "5m" },
            { "5minutes", "5m" },
            { "15m", "15m" },
            { "15min", "15m" },
            { "15minutes", "15m" },
            { "30m", "30m" },
            { "30min", "30m" },
            { "30minutes", "30m" },
            { "1h", "1h" },
            { "1hour", "1h" },
            { "3h", "3h" },
            { "3hour", "3h" },
            { "4h", "4h" },
            { "4hour", "4h" },
            { "6h", "6h" },
            { "6hour", "6h" },
            { "12h", "12h" },
            { "12hour", "12h" },
            { "1d", "1d" },
            { "1day", "1d" },
            { "daily", "1d" },
            { "1w", "1w" },
            { "1week", "1w" },
            { "weekly", "1w" },
            { "1month", "1M" },
            { "monthly", "1M" }
        };

        /// <summary>
        /// Normaliza o símbolo para o formato esperado pela API (BTC-BRL)
        /// </summary>
        /// <param name="symbol">Símbolo no formato original</param>
        /// <returns>Símbolo normalizado</returns>
        public static string NormalizeSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));

            // Remove espaços e converte para maiúsculo
            var normalizedSymbol = symbol.Trim().ToUpperInvariant();

            // Se já está no formato correto (contém hífen), retorna como está
            if (normalizedSymbol.Contains("-"))
                return normalizedSymbol;

            // Tenta mapear formatos comuns sem hífen para com hífen
            // Ex: BTCBRL -> BTC-BRL, btcbrl -> BTC-BRL
            var commonMappings = new Dictionary<string, string>
            {
                { "BTCBRL", "BTC-BRL" },
                { "ETHBRL", "ETH-BRL" },
                { "LTCBRL", "LTC-BRL" },
                { "XRPBRL", "XRP-BRL" },
                { "BCHBRL", "BCH-BRL" },
                { "ADABRL", "ADA-BRL" },
                { "DOTBRL", "DOT-BRL" },
                { "LINKBRL", "LINK-BRL" },
                { "USDCBRL", "USDC-BRL" },
                { "USDTBRL", "USDT-BRL" }
            };

            if (commonMappings.TryGetValue(normalizedSymbol, out var mappedSymbol))
                return mappedSymbol;

            // Se não conseguir mapear, assume que precisa adicionar hífen no meio
            // Para símbolos de 6 caracteres (3+3), adiciona hífen no meio
            if (normalizedSymbol.Length == 6)
            {
                return $"{normalizedSymbol.Substring(0, 3)}-{normalizedSymbol.Substring(3)}";
            }

            // Para outros casos, retorna como recebido
            return normalizedSymbol;
        }

        /// <summary>
        /// Normaliza a resolução/timeframe para o formato esperado pela API
        /// </summary>
        /// <param name="resolution">Resolução no formato original</param>
        /// <returns>Resolução normalizada</returns>
        public static string NormalizeResolution(string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
                throw new ArgumentException("Resolution cannot be null or empty.", nameof(resolution));

            var trimmedResolution = resolution.Trim();

            if (ResolutionMapping.TryGetValue(trimmedResolution, out var normalizedResolution))
                return normalizedResolution;

            // Se não encontrar mapeamento, retorna como recebido
            return trimmedResolution;
        }

        /// <summary>
        /// Converte ListCandlesResponse em lista de CandleData
        /// </summary>
        /// <param name="response">Resposta da API de candles</param>
        /// <param name="symbol">Símbolo do par de negociação</param>
        /// <param name="interval">Intervalo dos candles</param>
        /// <returns>Lista de CandleData</returns>
        public static List<CandleData> ToCandleDataList(this ListCandlesResponse response, string symbol, string interval)
        {
            if (response == null)
                return new List<CandleData>();

            var candleCount = response.T?.Count ?? 0;
            if (candleCount == 0)
                return new List<CandleData>();

            var candles = new List<CandleData>();

            // Converte arrays paralelos em objetos CandleData
            for (int i = 0; i < candleCount; i++)
            {
                var candle = new CandleData
                {
                    Symbol = symbol,
                    Interval = interval,
                    OpenTime = GetValueAtIndex(response.T, i) * 1000L, // Converte para milliseconds
                    CloseTime = GetValueAtIndex(response.T, i) * 1000L + GetIntervalInMilliseconds(interval),
                    Open = ParseDecimal(GetValueAtIndex(response.O, i)),
                    High = ParseDecimal(GetValueAtIndex(response.H, i)),
                    Low = ParseDecimal(GetValueAtIndex(response.L, i)),
                    Close = ParseDecimal(GetValueAtIndex(response.C, i)),
                    Volume = ParseDecimal(GetValueAtIndex(response.V, i))
                };

                candles.Add(candle);
            }

            return candles;
        }

        /// <summary>
        /// Obtém o valor no índice especificado de uma coleção, ou valor padrão se não existir
        /// </summary>
        private static T? GetValueAtIndex<T>(ICollection<T>? collection, int index)
        {
            if (collection == null || index >= collection.Count)
                return default(T);

            return collection.ElementAtOrDefault(index);
        }

        /// <summary>
        /// Converte string para decimal de forma segura
        /// </summary>
        private static decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            return 0m;
        }

        /// <summary>
        /// Calcula a duração do intervalo em milliseconds
        /// </summary>
        private static long GetIntervalInMilliseconds(string interval)
        {
            var normalizedInterval = NormalizeResolution(interval);

            return normalizedInterval switch
            {
                "1m" => 60 * 1000L,
                "5m" => 5 * 60 * 1000L,
                "15m" => 15 * 60 * 1000L,
                "30m" => 30 * 60 * 1000L,
                "1h" => 60 * 60 * 1000L,
                "3h" => 3 * 60 * 60 * 1000L,
                "4h" => 4 * 60 * 60 * 1000L,
                "6h" => 6 * 60 * 60 * 1000L,
                "12h" => 12 * 60 * 60 * 1000L,
                "1d" => 24 * 60 * 60 * 1000L,
                "1w" => 7 * 24 * 60 * 60 * 1000L,
                "1M" => 30 * 24 * 60 * 60 * 1000L, // Aproximação
                _ => 60 * 1000L // Default para 1 minuto
            };
        }

        /// <summary>
        /// Valida se a resolução é suportada pela API
        /// </summary>
        /// <param name="resolution">Resolução a ser validada</param>
        /// <returns>True se a resolução é válida</returns>
        public static bool IsValidResolution(string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
                return false;

            var normalizedResolution = NormalizeResolution(resolution);
            var validResolutions = new[] { "1m", "5m", "15m", "30m", "1h", "3h", "4h", "6h", "12h", "1d", "1w", "1M" };
            
            return validResolutions.Contains(normalizedResolution);
        }

        /// <summary>
        /// Valida se o símbolo tem formato válido
        /// </summary>
        /// <param name="symbol">Símbolo a ser validado</param>
        /// <returns>True se o símbolo é válido</returns>
        public static bool IsValidSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            var normalizedSymbol = NormalizeSymbol(symbol);
            
            // Verifica se tem formato BASE-QUOTE
            return normalizedSymbol.Contains("-") && normalizedSymbol.Split('-').Length == 2;
        }
    }
}