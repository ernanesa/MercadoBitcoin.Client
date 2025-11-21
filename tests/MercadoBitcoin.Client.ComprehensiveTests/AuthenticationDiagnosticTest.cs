using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Xunit;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    /// <summary>
    /// Diagnostic test to understand the authentication flow
    /// </summary>
    public class AuthenticationDiagnosticTest
    {
        [Fact]
        public async Task DiagnoseAuthentication_StepByStep()
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            Console.WriteLine("========================================");
            Console.WriteLine("🔍 AUTHENTICATION DIAGNOSTIC TEST");
            Console.WriteLine("========================================\n");

            // Step 1: Check if credentials are loaded
            Console.WriteLine("Step 1: Checking credential configuration...");
            var apiKey = configuration["MercadoBitcoin:ApiKey"];
            var apiSecret = configuration["MercadoBitcoin:ApiSecret"];
            
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                Console.WriteLine("❌ FAIL: Credentials not found in appsettings.json");
                Console.WriteLine($"   ApiKey present: {!string.IsNullOrEmpty(apiKey)}");
                Console.WriteLine($"   ApiSecret present: {!string.IsNullOrEmpty(apiSecret)}");
                throw new Exception("Credentials not configured");
            }
            
            Console.WriteLine($"✅ Credentials loaded");
            Console.WriteLine($"   ApiKey (first 5 chars): {apiKey?.Substring(0, Math.Min(5, apiKey.Length))}...");
            Console.WriteLine($"   ApiSecret length: {apiSecret?.Length} chars");
            Console.WriteLine();

            // Step 2: Create client with explicit configuration
            Console.WriteLine("Step 2: Creating MercadoBitcoinClient with explicit config...");
            
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
            
            // Verify options were set correctly
            var clientOptions = serviceProvider.GetRequiredService<IOptions<MercadoBitcoinClientOptions>>().Value;
            Console.WriteLine($"✅ Client created");
            Console.WriteLine($"   ApiLogin set: {!string.IsNullOrEmpty(clientOptions.ApiLogin)}");
            Console.WriteLine($"   ApiPassword set: {!string.IsNullOrEmpty(clientOptions.ApiPassword)}");
            Console.WriteLine($"   BaseUrl: {clientOptions.BaseUrl}");
            Console.WriteLine();

            // Step 3: Test public endpoint first (no auth needed)
            Console.WriteLine("Step 3: Testing public endpoint (no auth needed)...");
            try
            {
                var symbols = await client.GetSymbolsAsync();
                Console.WriteLine($"✅ Public endpoint works");
                Console.WriteLine($"   Symbols count: {symbols?.Symbol?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Public endpoint failed: {ex.Message}");
                throw;
            }
            Console.WriteLine();

            // Step 4: Test authenticated endpoint
            Console.WriteLine("Step 4: Testing authenticated endpoint (GetAccountsAsync)...");
            Console.WriteLine("   This should:");
            Console.WriteLine("   a) Send request without token → get 401");
            Console.WriteLine("   b) AuthenticationHandler intercepts 401");
            Console.WriteLine("   c) Call /authorize with ApiLogin + ApiPassword");
            Console.WriteLine("   d) Get Bearer token");
            Console.WriteLine("   e) Retry original request with token");
            Console.WriteLine("   f) Return account data");
            Console.WriteLine();
            
            try
            {
                Console.WriteLine("   Calling GetAccountsAsync()...");
                var accounts = await client.GetAccountsAsync();
                
                if (accounts != null && accounts.Count > 0)
                {
                    Console.WriteLine($"✅ Authentication SUCCESS!");
                    Console.WriteLine($"   Accounts retrieved: {accounts.Count}");
                    foreach (var account in accounts.Take(3))
                    {
                        Console.WriteLine($"   - {account.Name} ({account.Currency}): ID={account.Id}");
                    }
                    
                    // Check if token was stored
                    var token = client.GetAccessToken();
                    Console.WriteLine($"   Access token stored: {!string.IsNullOrEmpty(token)}");
                    if (!string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine($"   Token (first 20 chars): {token.Substring(0, Math.Min(20, token.Length))}...");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Authentication seemed to work but no accounts returned");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Authentication FAILED");
                Console.WriteLine($"   Exception: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Stack trace:");
                Console.WriteLine(ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner exception: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"   Inner message: {ex.InnerException.Message}");
                }
                
                throw;
            }
            
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("✅ DIAGNOSTIC TEST COMPLETED");
            Console.WriteLine("========================================");
        }

        [Fact]
        public async Task TestManualAuthorizationCall()
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            Console.WriteLine("========================================");
            Console.WriteLine("🔍 MANUAL /authorize ENDPOINT TEST");
            Console.WriteLine("========================================\n");

            var apiKey = configuration["MercadoBitcoin:ApiKey"];
            var apiSecret = configuration["MercadoBitcoin:ApiSecret"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                Console.WriteLine("❌ Credentials not found");
                throw new Exception("Credentials not configured");
            }

            Console.WriteLine("Testing direct call to /authorize endpoint...");
            Console.WriteLine($"ApiKey (first 5): {apiKey.Substring(0, Math.Min(5, apiKey.Length))}...");
            Console.WriteLine($"ApiSecret length: {apiSecret.Length} chars");
            Console.WriteLine();

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4/");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MercadoBitcoin.Client/4.0.1");

            var authRequest = new Generated.AuthorizeRequest
            {
                Login = apiKey,
                Password = apiSecret
            };

            try
            {
                Console.WriteLine("Sending POST to /authorize...");
                var response = await httpClient.PostAsJsonAsync(
                    "authorize", 
                    authRequest,
                    Generated.MercadoBitcoinJsonSerializerContext.Default.AuthorizeRequest
                );

                Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body:");
                Console.WriteLine(content);

                if (response.IsSuccessStatusCode)
                {
                    var authResult = await response.Content.ReadFromJsonAsync(
                        Generated.MercadoBitcoinJsonSerializerContext.Default.AuthorizeResponse
                    );
                    
                    if (authResult != null && !string.IsNullOrEmpty(authResult.Access_token))
                    {
                        Console.WriteLine($"✅ Token received!");
                        Console.WriteLine($"   Token (first 20): {authResult.Access_token.Substring(0, Math.Min(20, authResult.Access_token.Length))}...");
                        Console.WriteLine($"   Token length: {authResult.Access_token.Length} chars");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Success but no token in response");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Authorization failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                throw;
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("TEST COMPLETED");
            Console.WriteLine("========================================");
        }
    }
}
