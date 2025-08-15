using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace MercadoBitcoin.Client.IntegrationTests
{
    /// <summary>
    /// Configuração para testes de integração
    /// </summary>
    public static class TestConfig
    {
        private static readonly Lazy<IConfigurationRoot> _config = new Lazy<IConfigurationRoot>(() =>
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true) // Para configurações locais
                .AddEnvironmentVariables("MERCADOBITCOIN_") // Prefixo para variáveis de ambiente
                .AddEnvironmentVariables(); // Fallback para variáveis sem prefixo
            return builder.Build();
        });

        /// <summary>
        /// Client ID para autenticação na API
        /// </summary>
        public static string ClientId => GetConfigValue("ClientId");
        
        /// <summary>
        /// Client Secret para autenticação na API
        /// </summary>
        public static string ClientSecret => GetConfigValue("ClientSecret");
        
        /// <summary>
        /// URL base da API (para testes com ambiente diferente)
        /// </summary>
        public static string BaseUrl => _config.Value["MercadoBitcoinApi:BaseUrl"] ?? "https://api.mercadobitcoin.net/api/v4";
        
        /// <summary>
        /// Timeout para requisições em testes (em segundos)
        /// </summary>
        public static int RequestTimeoutSeconds => int.TryParse(_config.Value["MercadoBitcoinApi:RequestTimeoutSeconds"], out var timeout) ? timeout : 30;
        
        /// <summary>
        /// Se deve executar testes que fazem operações reais (trading, etc.)
        /// </summary>
        public static bool EnableRealOperations => bool.TryParse(_config.Value["MercadoBitcoinApi:EnableRealOperations"], out var enable) && enable;
        
        /// <summary>
        /// Símbolo padrão para testes (ex: BTC-BRL)
        /// </summary>
        public static string DefaultSymbol => _config.Value["MercadoBitcoinApi:DefaultSymbol"] ?? "BTC-BRL";
        
        /// <summary>
        /// Asset padrão para testes (ex: btc)
        /// </summary>
        public static string DefaultAsset => _config.Value["MercadoBitcoinApi:DefaultAsset"] ?? "btc";

        private static string GetConfigValue(string key)
        {
            // Tenta primeiro com o prefixo da seção
            var value = _config.Value[$"MercadoBitcoinApi:{key}"];
            
            // Se não encontrar, tenta diretamente
            if (string.IsNullOrEmpty(value))
            {
                value = _config.Value[key];
            }
            
            // Se ainda não encontrar, tenta com prefixo de ambiente
            if (string.IsNullOrEmpty(value))
            {
                value = _config.Value[$"MERCADOBITCOIN_{key.ToUpperInvariant()}"];
            }
            
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"{key} not configured. Set it in appsettings.json, appsettings.local.json, or environment variable MERCADOBITCOIN_{key.ToUpperInvariant()}");
            }
            
            return value;
        }

        private static bool IsPlaceholder(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var v = value.Trim();
            return v.Equals("YOUR_CLIENT_ID", StringComparison.OrdinalIgnoreCase)
                   || v.Equals("YOUR_CLIENT_SECRET", StringComparison.OrdinalIgnoreCase)
                   || v.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
                   || v.StartsWith("PLACEHOLDER_", StringComparison.OrdinalIgnoreCase)
                   || v.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica se credenciais reais estão configuradas (não são placeholders)
        /// </summary>
        public static bool HasRealCredentials
        {
            get
            {
                try
                {
                    var clientId = _config.Value["MercadoBitcoinApi:ClientId"] ?? _config.Value["ClientId"] ?? _config.Value["MERCADOBITCOIN_CLIENTID"];
                    var clientSecret = _config.Value["MercadoBitcoinApi:ClientSecret"] ?? _config.Value["ClientSecret"] ?? _config.Value["MERCADOBITCOIN_CLIENTSECRET"];
                    
                    return !IsPlaceholder(clientId) && !IsPlaceholder(clientSecret);
                }
                catch
                {
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Verifica se o ambiente está configurado para executar todos os tipos de teste
        /// </summary>
        public static bool IsFullTestEnvironment => HasRealCredentials && EnableRealOperations;
        
        /// <summary>
        /// Obtém informações de configuração para debugging
        /// </summary>
        public static string GetConfigurationInfo()
        {
            return $"HasRealCredentials: {HasRealCredentials}, EnableRealOperations: {EnableRealOperations}, BaseUrl: {BaseUrl}";
        }
    }
}