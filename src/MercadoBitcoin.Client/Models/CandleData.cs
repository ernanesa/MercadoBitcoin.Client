using System;

namespace MercadoBitcoin.Client.Models
{
    /// <summary>
    /// Representa dados de candle (OHLCV) para análise técnica
    /// </summary>
    public class CandleData
    {
        /// <summary>
        /// Símbolo do par de negociação (ex: BTC-BRL)
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Intervalo do candle (ex: 1m, 5m, 15m, 1h, 1d)
        /// </summary>
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp de abertura do candle (em milliseconds)
        /// </summary>
        public long OpenTime { get; set; }

        /// <summary>
        /// Timestamp de fechamento do candle (em milliseconds)
        /// </summary>
        public long CloseTime { get; set; }

        /// <summary>
        /// Preço de abertura
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// Preço máximo
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Preço mínimo
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Preço de fechamento
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Volume negociado
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Data/hora de abertura do candle
        /// </summary>
        public DateTime OpenDateTime => DateTimeOffset.FromUnixTimeMilliseconds(OpenTime).DateTime;

        /// <summary>
        /// Data/hora de fechamento do candle
        /// </summary>
        public DateTime CloseDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CloseTime).DateTime;

        /// <summary>
        /// Retorna uma representação string do candle
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol} {Interval} - Open: {Open}, High: {High}, Low: {Low}, Close: {Close}, Volume: {Volume}";
        }
    }
}