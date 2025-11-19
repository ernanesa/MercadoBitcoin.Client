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
    /// Usage examples of MercadoBitcoinClient with HTTP/2
    /// </summary>
    public class Http2Usage
    {
        /// <summary>
        /// Example 1: Basic usage with default HTTP/2
        /// </summary>
        public static async Task BasicHttp2Example()
        {
            // Create client with default HTTP/2 configuration
            var client = MercadoBitcoinClientExtensions.CreateWithHttp2();

            try
            {
                // Make a test request
                var tickers = await client.GetTickersAsync("BRLBTC");
                var ticker = System.Linq.Enumerable.FirstOrDefault(tickers);
                if (ticker != null)
                {
                    Console.WriteLine($"Ticker BTC: {ticker.Last}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        /// <summary>
        /// Example 2: Custom HTTP/2 configuration
        /// </summary>
        public static async Task CustomHttp2Example()
        {
            // Custom HTTP configuration
            var httpConfig = new HttpConfiguration
            {
                HttpVersion = new Version(2, 0),
                VersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionExact,
                TimeoutSeconds = 20,
                EnableCompression = true,
                MaxConnectionsPerServer = 150
            };

            // Custom retry configuration
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
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        /// <summary>
        /// Example 3: Trading optimized client
        /// </summary>
        public static async Task TradingOptimizedExample()
        {
            var client = MercadoBitcoinClientExtensions.CreateForTrading();

            try
            {
                // Low latency trading operations
                var trades = await client.GetTradesAsync("BRLBTC");
                Console.WriteLine($"Last trades: {trades.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        /// <summary>
        /// Example 4: Configuration via appsettings.json
        /// </summary>
        public static async Task ConfigurationExample()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            // Read HTTP configuration from appsettings
            var httpSection = configuration.GetSection("MercadoBitcoin:Http");
            var httpConfig = new HttpConfiguration();
            httpSection.Bind(httpConfig);

            // Read retry configuration from appsettings
            var retrySection = configuration.GetSection("MercadoBitcoin:Retry");
            var retryConfig = new RetryPolicyConfig();
            retrySection.Bind(retryConfig);

            var client = MercadoBitcoinClientExtensions.CreateWithHttp2(retryConfig, httpConfig);

            try
            {
                var tickers = await client.GetTickersAsync("BRLBTC");
                var ticker = System.Linq.Enumerable.FirstOrDefault(tickers);
                if (ticker != null)
                {
                    Console.WriteLine($"Day Summary - Volume: {ticker.Vol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        /// <summary>
        /// Example 5: Dependency Injection with HTTP/2
        /// </summary>
        public static void DependencyInjectionExample(IServiceCollection services, IConfiguration configuration)
        {
            // Register configurations
            services.Configure<HttpConfiguration>(configuration.GetSection("MercadoBitcoin:Http"));
            services.Configure<RetryPolicyConfig>(configuration.GetSection("MercadoBitcoin:Retry"));

            // Register client as singleton
            services.AddSingleton<MercadoBitcoinClient>(provider =>
            {
                var httpConfig = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HttpConfiguration>>().Value;
                var retryConfig = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RetryPolicyConfig>>().Value;

                return MercadoBitcoinClientExtensions.CreateWithHttp2(retryConfig, httpConfig);
            });
        }

        /// <summary>
        /// Example 6: Performance Comparison HTTP/1.1 vs HTTP/2
        /// </summary>
        public static async Task PerformanceComparisonExample()
        {
            Console.WriteLine("=== Performance Comparison HTTP/1.1 vs HTTP/2 ===");

            // HTTP/1.1 Client
            var http11Config = HttpConfiguration.CreateHttp11Default();
            var clientHttp11 = MercadoBitcoinClientExtensions.CreateWithHttp2(null, http11Config);

            // HTTP/2 Client
            var clientHttp2 = MercadoBitcoinClientExtensions.CreateWithHttp2();

            var symbols = new[] { "BRLBTC", "BRLETH", "BRLLTC", "BRLXRP", "BRLADA" };

            try
            {
                // Test HTTP/1.1
                var startTime = DateTime.UtcNow;
                var tasks11 = new List<Task>();
                foreach (var symbol in symbols)
                {
                    tasks11.Add(clientHttp11.GetTickersAsync(symbol));
                }
                await Task.WhenAll(tasks11);
                var http11Time = DateTime.UtcNow - startTime;

                // Test HTTP/2
                startTime = DateTime.UtcNow;
                var tasks2 = new List<Task>();
                foreach (var symbol in symbols)
                {
                    tasks2.Add(clientHttp2.GetTickersAsync(symbol));
                }
                await Task.WhenAll(tasks2);
                var http2Time = DateTime.UtcNow - startTime;

                Console.WriteLine($"HTTP/1.1 - Time: {http11Time.TotalMilliseconds:F2}ms");
                Console.WriteLine($"HTTP/2.0 - Time: {http2Time.TotalMilliseconds:F2}ms");
                Console.WriteLine($"Improvement: {((http11Time.TotalMilliseconds - http2Time.TotalMilliseconds) / http11Time.TotalMilliseconds * 100):F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in performance test: {ex.Message}");
            }
            finally
            {
                clientHttp11.Dispose();
                clientHttp2.Dispose();
            }
        }
    }
}