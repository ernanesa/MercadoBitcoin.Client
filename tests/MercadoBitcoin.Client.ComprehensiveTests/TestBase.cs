using Microsoft.Extensions.Configuration;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.Extensions;
using System.Text.Json;

namespace MercadoBitcoin.Client.ComprehensiveTests;

public abstract class TestBase : IDisposable
{
    protected readonly IConfiguration Configuration;
    protected readonly MercadoBitcoinClient Client;
    protected readonly string TestSymbol;
    protected readonly string TestAccountId;
    protected readonly int DelayBetweenRequests;
    protected readonly int MaxRetries;
    
    protected TestBase()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var apiKey = Configuration["MercadoBitcoin:ApiKey"] ?? throw new InvalidOperationException("API Key not configured");
        var apiSecret = Configuration["MercadoBitcoin:ApiSecret"] ?? throw new InvalidOperationException("API Secret not configured");
        var baseUrl = Configuration["MercadoBitcoin:BaseUrl"] ?? "https://api.mercadobitcoin.net";
        var timeout = int.Parse(Configuration["MercadoBitcoin:Timeout"] ?? "30");

        // Create client with retry policies using extension method
        Client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
        TestSymbol = Configuration["TestSettings:TestSymbol"] ?? "BTC-BRL";
        TestAccountId = Configuration["TestSettings:TestAccountId"] ?? "test-account-id";
        DelayBetweenRequests = int.Parse(Configuration["TestSettings:DelayBetweenRequests"] ?? "1000");
        MaxRetries = int.Parse(Configuration["TestSettings:MaxRetries"] ?? "3");
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