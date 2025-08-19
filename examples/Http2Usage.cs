using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Examples
{
    /// <summary>
    /// Exemplos de uso do MercadoBitcoinClient com HTTP/2
    /// </summary>
    public class Http2Usage
    {
        /// <summary>
        /// Exemplo 1: Uso básico com HTTP/2 padrão
        /// </summary>
        public static async Task BasicHttp2Example()
        {
            // Criar cliente com configuração HTTP/2 padrão
            var client = MercadoBitcoinClientExtensions.CreateWithHttp2();
            
            try
            {
                // Fazer uma requisição de teste
                var ticker = await client.GetTickerAsync("BRLBTC");
                Console.WriteLine($"Ticker BTC: {ticker.Last}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }
        
        /// <summary>
        /// Exemplo 2: Configuração personalizada de HTTP/2
        /// </summary>
        public static async Task CustomHttp2Example()
        {
            // Configuração HTTP personalizada
            var httpConfig = new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                VersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionExact,
                TimeoutSeconds = 20,
                EnableCompression = true,
                MaxConnectionsPerServer = 150
            };
            
            // Configuração de retry personalizada
            var retryConfig = new RetryPolicyConfig
            {
                MaxRetryAttempts = 5,
                BaseDelaySeconds = 0.3,
                BackoffMultiplier = 1.8,
                MaxDelaySeconds = 12.0,
                RetryOnTimeout = true,
                RetryOnRateLimit = true,
                RetryOnServerErrors = true
            };
            
            var client = MercadoBitcoinClientExtensions.CreateWithHttp2(retryConfig, httpConfig);
            
            try
            {
                var orderBook = await client.GetOrderBookAsync("BRLBTC");
                Console.WriteLine($"OrderBook - Bids: {orderBook.Bids.Count}, Asks: {orderBook.Asks.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }
        
        /// <summary>
        /// Exemplo 3: Cliente otimizado para trading
        /// </summary>
        public static async Task TradingOptimizedExample()
        {
            var client = MercadoBitcoinClientExtensions.CreateForTrading();
            
            try
            {
                // Operações de trading com baixa latência
                var trades = await client.GetTradesAsync("BRLBTC");
                Console.WriteLine($"Últimos trades: {trades.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }
        
        /// <summary>
        /// Exemplo 4: Configuração via appsettings.json
        /// </summary>
        public static async Task ConfigurationExample()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            
            // Ler configuração HTTP do appsettings
            var httpSection = configuration.GetSection("MercadoBitcoin:Http");
            var httpConfig = new HttpConfiguration();
            httpSection.Bind(httpConfig);
            
            // Ler configuração de retry do appsettings
            var retrySection = configuration.GetSection("MercadoBitcoin:Retry");
            var retryConfig = new RetryPolicyConfig();
            retrySection.Bind(retryConfig);
            
            var client = MercadoBitcoinClientExtensions.CreateWithHttp2(retryConfig, httpConfig);
            
            try
            {
                var summary = await client.GetDaySummaryAsync("BRLBTC");
                Console.WriteLine($"Resumo do dia - Volume: {summary.Volume}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }
        
        /// <summary>
        /// Exemplo 5: Injeção de dependência com HTTP/2
        /// </summary>
        public static void DependencyInjectionExample(IServiceCollection services, IConfiguration configuration)
        {
            // Registrar configurações
            services.Configure<HttpConfiguration>(configuration.GetSection("MercadoBitcoin:Http"));
            services.Configure<RetryPolicyConfig>(configuration.GetSection("MercadoBitcoin:Retry"));
            
            // Registrar cliente como singleton
            services.AddSingleton<MercadoBitcoinClient>(provider =>
            {
                var httpConfig = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HttpConfiguration>>().Value;
                var retryConfig = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RetryPolicyConfig>>().Value;
                
                return MercadoBitcoinClientExtensions.CreateWithHttp2(retryConfig, httpConfig);
            });
        }
        
        /// <summary>
        /// Exemplo 6: Comparação de performance HTTP/1.1 vs HTTP/2
        /// </summary>
        public static async Task PerformanceComparisonExample()
        {
            Console.WriteLine("=== Comparação de Performance HTTP/1.1 vs HTTP/2 ===");
            
            // Cliente HTTP/1.1
            var http11Config = HttpConfiguration.CreateHttp11Default();
            var clientHttp11 = MercadoBitcoinClientExtensions.CreateWithHttp2(null, http11Config);
            
            // Cliente HTTP/2
            var clientHttp2 = MercadoBitcoinClientExtensions.CreateWithHttp2();
            
            var symbols = new[] { "BRLBTC", "BRLETH", "BRLLTC", "BRLXRP", "BRLADA" };
            
            try
            {
                // Teste HTTP/1.1
                var startTime = DateTime.UtcNow;
                var tasks11 = new List<Task>();
                foreach (var symbol in symbols)
                {
                    tasks11.Add(clientHttp11.GetTickerAsync(symbol));
                }
                await Task.WhenAll(tasks11);
                var http11Time = DateTime.UtcNow - startTime;
                
                // Teste HTTP/2
                startTime = DateTime.UtcNow;
                var tasks2 = new List<Task>();
                foreach (var symbol in symbols)
                {
                    tasks2.Add(clientHttp2.GetTickerAsync(symbol));
                }
                await Task.WhenAll(tasks2);
                var http2Time = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"HTTP/1.1 - Tempo: {http11Time.TotalMilliseconds:F2}ms");
                Console.WriteLine($"HTTP/2.0 - Tempo: {http2Time.TotalMilliseconds:F2}ms");
                Console.WriteLine($"Melhoria: {((http11Time.TotalMilliseconds - http2Time.TotalMilliseconds) / http11Time.TotalMilliseconds * 100):F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no teste de performance: {ex.Message}");
            }
            finally
            {
                clientHttp11.Dispose();
                clientHttp2.Dispose();
            }
        }
    }
}