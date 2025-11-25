using Microsoft.Extensions.Configuration;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Configuration;
using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.ComprehensiveTests;

public abstract class TestBase : IDisposable
{
    protected readonly IConfiguration Configuration;
    protected readonly MercadoBitcoinClient Client;
    protected readonly string TestSymbol;
    protected string TestAccountId;
    protected readonly int DelayBetweenRequests;
    protected readonly int MaxRetries;

    protected TestBase()
    {
        // NOTE: This configuration loading is specific to the TEST PROJECT.
        // The library itself does not depend on appsettings.json or .env files.
        // We use this here to load credentials for integration tests.
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var apiKey = Configuration["MercadoBitcoin:ApiKey"];
        var apiSecret = Configuration["MercadoBitcoin:ApiSecret"];
        var baseUrl = Configuration["MercadoBitcoin:BaseUrl"] ?? "https://api.mercadobitcoin.net/api/v4";
        var timeout = int.Parse(Configuration["MercadoBitcoin:Timeout"] ?? "30");

        TestSymbol = Configuration["TestSettings:TestSymbol"] ?? "BTC-BRL";
        TestAccountId = Configuration["TestSettings:TestAccountId"] ?? "test-account-id";
        DelayBetweenRequests = int.Parse(Configuration["TestSettings:DelayBetweenRequests"] ?? "1000");
        MaxRetries = int.Parse(Configuration["TestSettings:MaxRetries"] ?? "3");

        // Prepare options
        var options = new MercadoBitcoinClientOptions
        {
            BaseUrl = baseUrl,
            TimeoutSeconds = timeout,
            RetryPolicyConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig()
        };

        // Try environment variables first, then fall back to appsettings.json
        var loginEnv = Environment.GetEnvironmentVariable("MB_LOGIN");
        var passwordEnv = Environment.GetEnvironmentVariable("MB_PASSWORD");

        if (!string.IsNullOrWhiteSpace(loginEnv) && !string.IsNullOrWhiteSpace(passwordEnv))
        {
            Console.WriteLine("[AUTH] Configuring client with MB_LOGIN/MB_PASSWORD environment variables...");
            options.ApiLogin = loginEnv;
            options.ApiPassword = passwordEnv;
        }
        else if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiSecret))
        {
            Console.WriteLine("[AUTH] Configuring client with ApiKey/ApiSecret from appsettings.json...");
            options.ApiLogin = apiKey;
            options.ApiPassword = apiSecret;
        }
        else
        {
            Console.WriteLine("[AUTH] No credentials found. Private endpoints will fail.");
        }

        // Create client
        Client = new MercadoBitcoinClient(options);

        if (!string.IsNullOrWhiteSpace(options.ApiLogin))
        {
            try
            {
                // Verify authentication by fetching accounts (optional, but good for setting up TestAccountId)
                // Controlled synchronous usage in constructor
                var accounts = Client.GetAccountsAsync().GetAwaiter().GetResult();
                var token = Client.GetAccessToken();
                Console.WriteLine($"[AUTH] Authenticated. Token length={token?.Length}");

                // Automatically adjust TestAccountId if using placeholder
                var placeholder = string.IsNullOrWhiteSpace(TestAccountId) || TestAccountId.Contains("test-account", StringComparison.OrdinalIgnoreCase) || TestAccountId.Length < 10;
                if (placeholder)
                {
                    var first = accounts.FirstOrDefault();
                    if (first?.Id != null)
                    {
                        TestAccountId = first.Id;
                        Console.WriteLine($"[AUTH] TestAccountId updated to real id: {TestAccountId}");
                    }
                    else
                    {
                        Console.WriteLine("[AUTH][WARN] No accounts returned to update TestAccountId.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH][WARN] Failed to verify authentication or get accounts: {ex.Message}");
            }
        }
    }

    protected async Task DelayAsync()
    {
        await Task.Delay(DelayBetweenRequests);
    }

    protected void LogTestResult(string testName, bool success, string? details = null)
    {
        var status = success ? "✅ PASS" : "❌ FAIL";
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {status} - {testName}");
        if (!string.IsNullOrEmpty(details))
        {
            Console.WriteLine($"    Details: {details}");
        }
    }

    protected void LogApiCall(string endpoint, object? request = null, object? response = null)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🌐 API Call: {endpoint}");
        if (request != null)
        {
            Console.WriteLine($"    Request: {JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true })}");
        }
        if (response != null)
        {
            var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            var truncated = responseJson.Length > 500 ? responseJson[..500] + "..." : responseJson;
            Console.WriteLine($"    Response: {truncated}");
        }
    }

    public virtual void Dispose()
    {
        Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}