namespace MercadoBitcoin.Client.WebSocket.Models
{
    /// <summary>
    /// Constantes para WebSocket do Mercado Bitcoin
    /// </summary>
    public static class WebSocketConstants
    {
        /// <summary>
        /// URL base do WebSocket de produção
        /// </summary>
        public const string ProductionUrl = "wss://ws.mercadobitcoin.net/ws";

        /// <summary>
        /// Timeout padrão para conexão (segundos)
        /// </summary>
        public const int DefaultConnectionTimeoutSeconds = 30;

        /// <summary>
        /// Intervalo de ping para manter conexão ativa (segundos)
        /// </summary>
        public const int PingIntervalSeconds = 30;

        /// <summary>
        /// Timeout para inatividade antes do fechamento da conexão (segundos)
        /// </summary>
        public const int InactivityTimeoutSeconds = 5;

        /// <summary>
        /// User-Agent padrão para conexões WebSocket
        /// </summary>
        public const string DefaultUserAgent = "MercadoBitcoin.Client/1.0";
    }

    /// <summary>
    /// Canais disponíveis no WebSocket
    /// </summary>
    public static class WebSocketChannels
    {
        /// <summary>
        /// Canal de trades em tempo real
        /// </summary>
        public const string Trades = "trades";

        /// <summary>
        /// Canal do livro de ofertas completo
        /// </summary>
        public const string OrderBook = "orderbook";

        /// <summary>
        /// Canal de atualizações incrementais do livro de ofertas
        /// </summary>
        public const string OrderBookUpdate = "orderbook_update";

        /// <summary>
        /// Canal do ticker em tempo real
        /// </summary>
        public const string Ticker = "ticker";

        /// <summary>
        /// Canal de candles (OHLCV)
        /// </summary>
        public const string Candles = "candles";

        /// <summary>
        /// Canal de estatísticas de 24h
        /// </summary>
        public const string Stats24h = "stats_24h";
    }

    /// <summary>
    /// Tipos de mensagens WebSocket
    /// </summary>
    public static class MessageTypes
    {
        /// <summary>
        /// Mensagem de inscrição em canal
        /// </summary>
        public const string Subscribe = "subscribe";

        /// <summary>
        /// Mensagem de cancelamento de inscrição
        /// </summary>
        public const string Unsubscribe = "unsubscribe";

        /// <summary>
        /// Confirmação de inscrição
        /// </summary>
        public const string Subscribed = "subscribed";

        /// <summary>
        /// Confirmação de cancelamento
        /// </summary>
        public const string Unsubscribed = "unsubscribed";

        /// <summary>
        /// Mensagem de erro
        /// </summary>
        public const string Error = "error";

        /// <summary>
        /// Dados de trade
        /// </summary>
        public const string Trade = "trade";

        /// <summary>
        /// Dados do livro de ofertas
        /// </summary>
        public const string OrderBook = "orderbook";

        /// <summary>
        /// Atualização do livro de ofertas
        /// </summary>
        public const string OrderBookUpdate = "orderbook_update";

        /// <summary>
        /// Dados do ticker
        /// </summary>
        public const string Ticker = "ticker";

        /// <summary>
        /// Dados de candle
        /// </summary>
        public const string Candle = "candle";

        /// <summary>
        /// Ping para manter conexão
        /// </summary>
        public const string Ping = "ping";

        /// <summary>
        /// Pong em resposta ao ping
        /// </summary>
        public const string Pong = "pong";
    }

    /// <summary>
    /// Lados de negociação
    /// </summary>
    public static class TradeSides
    {
        /// <summary>
        /// Compra
        /// </summary>
        public const string Buy = "buy";

        /// <summary>
        /// Venda
        /// </summary>
        public const string Sell = "sell";
    }

    /// <summary>
    /// Lados do livro de ofertas
    /// </summary>
    public static class OrderBookSides
    {
        /// <summary>
        /// Ofertas de compra
        /// </summary>
        public const string Bid = "bid";

        /// <summary>
        /// Ofertas de venda
        /// </summary>
        public const string Ask = "ask";
    }

    /// <summary>
    /// Intervalos de candles suportados
    /// </summary>
    public static class CandleIntervals
    {
        /// <summary>
        /// 1 minuto
        /// </summary>
        public const string OneMinute = "1m";

        /// <summary>
        /// 5 minutos
        /// </summary>
        public const string FiveMinutes = "5m";

        /// <summary>
        /// 15 minutos
        /// </summary>
        public const string FifteenMinutes = "15m";

        /// <summary>
        /// 30 minutos
        /// </summary>
        public const string ThirtyMinutes = "30m";

        /// <summary>
        /// 1 hora
        /// </summary>
        public const string OneHour = "1h";

        /// <summary>
        /// 4 horas
        /// </summary>
        public const string FourHours = "4h";

        /// <summary>
        /// 1 dia
        /// </summary>
        public const string OneDay = "1d";

        /// <summary>
        /// 1 semana
        /// </summary>
        public const string OneWeek = "1w";

        /// <summary>
        /// 1 mês
        /// </summary>
        public const string OneMonth = "1M";
    }

    /// <summary>
    /// Estados da conexão WebSocket
    /// </summary>
    public enum WebSocketState
    {
        /// <summary>
        /// Desconectado
        /// </summary>
        Disconnected,

        /// <summary>
        /// Conectando
        /// </summary>
        Connecting,

        /// <summary>
        /// Conectado
        /// </summary>
        Connected,

        /// <summary>
        /// Desconectando
        /// </summary>
        Disconnecting,

        /// <summary>
        /// Erro de conexão
        /// </summary>
        Error
    }
}