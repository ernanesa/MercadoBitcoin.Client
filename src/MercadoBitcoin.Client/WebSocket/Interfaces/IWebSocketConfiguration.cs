using System;
using System.Collections.Generic;

namespace MercadoBitcoin.Client.WebSocket.Interfaces
{
    /// <summary>
    /// Interface para configuração do cliente WebSocket
    /// </summary>
    public interface IWebSocketConfiguration
    {
        /// <summary>
        /// URL do WebSocket
        /// </summary>
        string Url { get; }

        /// <summary>
        /// User-Agent para conexões
        /// </summary>
        string UserAgent { get; }

        /// <summary>
        /// Timeout para conexão em segundos
        /// </summary>
        int ConnectionTimeoutSeconds { get; }

        /// <summary>
        /// Intervalo de ping em segundos
        /// </summary>
        int PingIntervalSeconds { get; }

        /// <summary>
        /// Habilita reconexão automática
        /// </summary>
        bool EnableAutoReconnect { get; }

        /// <summary>
        /// Intervalo inicial para reconexão em segundos
        /// </summary>
        int ReconnectIntervalSeconds { get; }

        /// <summary>
        /// Número máximo de tentativas de reconexão
        /// </summary>
        int MaxReconnectAttempts { get; }

        /// <summary>
        /// Multiplicador para backoff exponencial na reconexão
        /// </summary>
        double ReconnectBackoffMultiplier { get; }

        /// <summary>
        /// Intervalo máximo entre tentativas de reconexão em segundos
        /// </summary>
        int MaxReconnectIntervalSeconds { get; }

        /// <summary>
        /// Cabeçalhos HTTP adicionais
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Habilita logging detalhado
        /// </summary>
        bool EnableVerboseLogging { get; }

        /// <summary>
        /// Tamanho do buffer de recepção em bytes
        /// </summary>
        int ReceiveBufferSize { get; }

        /// <summary>
        /// Tamanho do buffer de envio em bytes
        /// </summary>
        int SendBufferSize { get; }
    }

    /// <summary>
    /// Implementação padrão da configuração do WebSocket
    /// </summary>
    public class WebSocketConfiguration : IWebSocketConfiguration
    {
        /// <inheritdoc />
        public string Url { get; set; } = Models.WebSocketConstants.ProductionUrl;

        /// <inheritdoc />
        public string UserAgent { get; set; } = Models.WebSocketConstants.DefaultUserAgent;

        /// <inheritdoc />
        public int ConnectionTimeoutSeconds { get; set; } = Models.WebSocketConstants.DefaultConnectionTimeoutSeconds;

        /// <inheritdoc />
        public int PingIntervalSeconds { get; set; } = Models.WebSocketConstants.PingIntervalSeconds;

        /// <inheritdoc />
        public bool EnableAutoReconnect { get; set; } = true;

        /// <inheritdoc />
        public int ReconnectIntervalSeconds { get; set; } = 5;

        /// <inheritdoc />
        public int MaxReconnectAttempts { get; set; } = 10;

        /// <inheritdoc />
        public double ReconnectBackoffMultiplier { get; set; } = 1.5;

        /// <inheritdoc />
        public int MaxReconnectIntervalSeconds { get; set; } = 300; // 5 minutos

        /// <inheritdoc />
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <inheritdoc />
        public bool EnableVerboseLogging { get; set; } = false;

        /// <inheritdoc />
        public int ReceiveBufferSize { get; set; } = 4096;

        /// <inheritdoc />
        public int SendBufferSize { get; set; } = 4096;

        /// <summary>
        /// Cria uma nova instância com configurações padrão
        /// </summary>
        public WebSocketConfiguration()
        {
            Headers["User-Agent"] = UserAgent;
        }

        /// <summary>
        /// Cria uma nova instância com URL personalizada
        /// </summary>
        /// <param name="url">URL do WebSocket</param>
        public WebSocketConfiguration(string url) : this()
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        /// <summary>
        /// Cria uma configuração para ambiente de produção
        /// </summary>
        /// <returns>Configuração para produção</returns>
        public static WebSocketConfiguration CreateProduction()
        {
            return new WebSocketConfiguration(Models.WebSocketConstants.ProductionUrl)
            {
                EnableVerboseLogging = false,
                EnableAutoReconnect = true,
                MaxReconnectAttempts = 10
            };
        }

        /// <summary>
        /// Cria uma configuração para desenvolvimento/debug
        /// </summary>
        /// <returns>Configuração para desenvolvimento</returns>
        public static WebSocketConfiguration CreateDevelopment()
        {
            return new WebSocketConfiguration(Models.WebSocketConstants.ProductionUrl)
            {
                EnableVerboseLogging = true,
                EnableAutoReconnect = true,
                MaxReconnectAttempts = 3,
                ReconnectIntervalSeconds = 2
            };
        }

        /// <summary>
        /// Cria uma configuração para testes
        /// </summary>
        /// <returns>Configuração para testes</returns>
        public static WebSocketConfiguration CreateTesting()
        {
            return new WebSocketConfiguration(Models.WebSocketConstants.ProductionUrl)
            {
                EnableVerboseLogging = false,
                EnableAutoReconnect = false,
                ConnectionTimeoutSeconds = 10,
                PingIntervalSeconds = 10
            };
        }
    }
}