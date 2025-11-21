using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthDiagnostic
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("🔍 MercadoBitcoin Authentication Diagnostic");
            Console.WriteLine("========================================\n");

            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var apiKey = configuration["MercadoBitcoin:ApiKey"];
            var apiSecret = configuration["MercadoBitcoin:ApiSecret"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                Console.WriteLine("❌ ERROR: Credentials not found in appsettings.json");
                Console.WriteLine("Please ensure appsettings.json contains:");
                Console.WriteLine("{");
                Console.WriteLine("  \"MercadoBitcoin\": {");
                Console.WriteLine("    \"ApiKey\": \"your_key\",");
                Console.WriteLine("    \"ApiSecret\": \"your_secret\"");
                Console.WriteLine("  }");
                Console.WriteLine("}");
                return;
            }

            Console.WriteLine("✅ Step 1: Credentials loaded from appsettings.json");
            Console.WriteLine($"   ApiKey (first 5): {apiKey.Substring(0, Math.Min(5, apiKey.Length))}...");
            Console.WriteLine($"   ApiSecret length: {apiSecret.Length} chars\n");

            // TEST #1: Manual /authorize endpoint call
            Console.WriteLine("========================================");
            Console.WriteLine("Test #1: Direct /authorize endpoint call");
            Console.WriteLine("========================================");
            await TestDirectAuthorizationCall(apiKey, apiSecret);

            Console.WriteLine("\n");

            // TEST #2: Using MercadoBitcoinClient with DI
            Console.WriteLine("========================================");
            Console.WriteLine("Test #2: Using MercadoBitcoinClient (DI)");
            Console.WriteLine("========================================");
            await TestWithDependencyInjection(apiKey, apiSecret);

            Console.WriteLine("\n");

            // TEST #3: Using MercadoBitcoinClient standalone
            Console.WriteLine("========================================");
            Console.WriteLine("Test #3: Using MercadoBitcoinClient (Standalone)");
            Console.WriteLine("========================================");
            await TestStandaloneClient(apiKey, apiSecret);

            Console.WriteLine("\n========================================");
            Console.WriteLine("✅ Diagnostic completed!");
            Console.WriteLine("========================================");
        }

        static async Task TestDirectAuthorizationCall(string apiKey, string apiSecret)
        {
            try
            {
                Console.WriteLine("Making direct HTTP call to /authorize...");
                
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4/");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "MercadoBitcoin.Client/4.0.1-Diagnostic");

                var authRequest = new AuthorizeRequest
                {
                    Login = apiKey,
                    Password = apiSecret
                };

                var response = await httpClient.PostAsJsonAsync(
                    "authorize",
                    authRequest,
                    MercadoBitcoinJsonSerializerContext.Default.AuthorizeRequest
                );

                Console.WriteLine($"Response status: {(int)response.StatusCode} {response.StatusCode}");
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body: {content.Substring(0, Math.Min(200, content.Length))}...");

                if (response.IsSuccessStatusCode)
                {
                    var authResult = await response.Content.ReadFromJsonAsync(
                        MercadoBitcoinJsonSerializerContext.Default.AuthorizeResponse
                    );

                    if (authResult != null && !string.IsNullOrEmpty(authResult.Access_token))
                    {
                        Console.WriteLine($"✅ Token received successfully!");
                        Console.WriteLine($"   Token (first 30): {authResult.Access_token.Substring(0, Math.Min(30, authResult.Access_token.Length))}...");
                        Console.WriteLine($"   Token length: {authResult.Access_token.Length} chars");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Request succeeded but no access_token in response");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Authorization failed with status {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred:");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }
        }

        static async Task TestWithDependencyInjection(string apiKey, string apiSecret)
        {
            try
            {
                Console.WriteLine("Setting up MercadoBitcoinClient via DI...");
                
                var services = new ServiceCollection();
                
                services.AddMercadoBitcoinClient(options =>
                {
                    options.ApiLogin = apiKey;
                    options.ApiPassword = apiSecret;
                    options.BaseUrl = "https://api.mercadobitcoin.net/api/v4";
                    options.TimeoutSeconds = 30;
                });

                var serviceProvider = services.BuildServiceProvider();
                var client = serviceProvider.GetRequiredService<MercadoBitcoinClient>();
                var clientOptions = serviceProvider.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;

                Console.WriteLine($"✅ Client created");
                Console.WriteLine($"   ApiLogin set: {!string.IsNullOrEmpty(clientOptions.ApiLogin)}");
                Console.WriteLine($"   ApiPassword set: {!string.IsNullOrEmpty(clientOptions.ApiPassword)}");
                Console.WriteLine($"   BaseUrl: {clientOptions.BaseUrl}");
                Console.WriteLine();

                // Test public endpoint first
                Console.WriteLine("Testing public endpoint (GetSymbolsAsync)...");
                var symbols = await client.GetSymbolsAsync();
                Console.WriteLine($"✅ Public endpoint works! Symbols count: {symbols?.Symbol?.Count ?? 0}");
                Console.WriteLine();

                // Test authenticated endpoint
                Console.WriteLine("Testing authenticated endpoint (GetAccountsAsync)...");
                Console.WriteLine("This should trigger the lazy authentication flow:");
                Console.WriteLine("  1. Send request without token → 401");
                Console.WriteLine("  2. AuthenticationHandler intercepts 401");
                Console.WriteLine("  3. Call /authorize to get Bearer token");
                Console.WriteLine("  4. Retry request with token");
                Console.WriteLine();

                var accounts = await client.GetAccountsAsync();

                if (accounts != null && accounts.Count > 0)
                {
                    Console.WriteLine($"✅ Authentication SUCCESS!");
                    Console.WriteLine($"   Accounts retrieved: {accounts.Count}");
                    foreach (var account in accounts.Take(5))
                    {
                        Console.WriteLine($"   - {account.Name} ({account.Currency}): ID={account.Id}");
                    }

                    var token = client.GetAccessToken();
                    Console.WriteLine($"   Token stored: {!string.IsNullOrEmpty(token)}");
                    if (!string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine($"   Token (first 30): {token.Substring(0, Math.Min(30, token.Length))}...");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Request succeeded but no accounts returned");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred:");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }
        }

        static async Task TestStandaloneClient(string apiKey, string apiSecret)
        {
            try
            {
                Console.WriteLine("Creating standalone MercadoBitcoinClient...");
                
                var options = new MercadoBitcoinClientOptions
                {
                    ApiLogin = apiKey,
                    ApiPassword = apiSecret,
                    BaseUrl = "https://api.mercadobitcoin.net/api/v4",
                    TimeoutSeconds = 30,
                    RetryPolicyConfig = new RetryPolicyConfig
                    {
                        MaxRetryAttempts = 3,
                        BaseDelaySeconds = 1.0,
                        RetryOnRateLimit = true
                    }
                };

                var client = new MercadoBitcoinClient(options);

                Console.WriteLine($"✅ Client created");
                Console.WriteLine($"   ApiLogin: {!string.IsNullOrEmpty(options.ApiLogin)}");
                Console.WriteLine($"   ApiPassword: {!string.IsNullOrEmpty(options.ApiPassword)}");
                Console.WriteLine();

                Console.WriteLine("Testing authenticated endpoint (GetAccountsAsync)...");
                var accounts = await client.GetAccountsAsync();

                if (accounts != null && accounts.Count > 0)
                {
                    Console.WriteLine($"✅ Authentication SUCCESS!");
                    Console.WriteLine($"   Accounts: {accounts.Count}");
                    var token = client.GetAccessToken();
                    Console.WriteLine($"   Token stored: {!string.IsNullOrEmpty(token)}");
                }
                else
                {
                    Console.WriteLine("⚠️ No accounts returned");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred:");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }
        }
    }
}
