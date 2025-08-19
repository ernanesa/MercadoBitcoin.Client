using System;

namespace MercadoBitcoin.Client.Http
{
    /// <summary>
    /// Configurações HTTP para o cliente MercadoBitcoin
    /// </summary>
    public class HttpConfiguration
    {
        /// <summary>
        /// Versão HTTP a ser utilizada (padrão: 2.0 para HTTP/2)
        /// </summary>
        public Version HttpVersion { get; set; } = new Version(2, 0);

        /// <summary>
        /// Política de versão HTTP (padrão: RequestVersionOrLower)
        /// </summary>
        public HttpVersionPolicy VersionPolicy { get; set; } = HttpVersionPolicy.RequestVersionOrLower;

        /// <summary>
        /// Timeout para requisições HTTP em segundos (padrão: 30)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Habilitar compressão automática (padrão: true)
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Habilitar HTTP/2 Server Push (padrão: true)
        /// </summary>
        public bool EnableHttp2ServerPush { get; set; } = true;

        /// <summary>
        /// Tamanho máximo do pool de conexões (padrão: 100)
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = 100;

        /// <summary>
        /// Tempo de vida das conexões em segundos (padrão: 300 - 5 minutos)
        /// </summary>
        public int ConnectionLifetimeSeconds { get; set; } = 300;

        /// <summary>
        /// Cria uma configuração padrão otimizada para HTTP/2
        /// </summary>
        /// <returns>Configuração HTTP otimizada</returns>
        public static HttpConfiguration CreateHttp2Default()
        {
            return new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                TimeoutSeconds = 30,
                EnableCompression = true,
                EnableHttp2ServerPush = true,
                MaxConnectionsPerServer = 100,
                ConnectionLifetimeSeconds = 300
            };
        }

        /// <summary>
        /// Cria uma configuração para HTTP/1.1 (compatibilidade)
        /// </summary>
        /// <returns>Configuração HTTP 1.1</returns>
        public static HttpConfiguration CreateHttp11Fallback()
        {
            return new HttpConfiguration
            {
                HttpVersion = new Version(1, 1),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                TimeoutSeconds = 30,
                EnableCompression = true,
                EnableHttp2ServerPush = false,
                MaxConnectionsPerServer = 50,
                ConnectionLifetimeSeconds = 120
            };
        }

        /// <summary>
        /// Cria uma configuração otimizada para trading (baixa latência)
        /// </summary>
        /// <returns>Configuração HTTP para trading</returns>
        public static HttpConfiguration CreateTradingOptimized()
        {
            return new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                TimeoutSeconds = 15, // Timeout mais baixo para trading
                EnableCompression = false, // Desabilitar compressão para menor latência
                EnableHttp2ServerPush = true,
                MaxConnectionsPerServer = 200, // Mais conexões para trading
                ConnectionLifetimeSeconds = 600 // Conexões mais duradouras
            };
        }
    }
}