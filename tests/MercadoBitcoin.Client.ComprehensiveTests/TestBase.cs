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
    protected string TestAccountId;
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

        // Autenticação automática se variáveis de ambiente estiverem presentes
        try
        {
            var loginEnv = Environment.GetEnvironmentVariable("MB_LOGIN");
            var passwordEnv = Environment.GetEnvironmentVariable("MB_PASSWORD");
            if (!string.IsNullOrWhiteSpace(loginEnv) && !string.IsNullOrWhiteSpace(passwordEnv))
            {
                Console.WriteLine("[AUTH] Tentando autenticar com MB_LOGIN/MB_PASSWORD...");
                // Uso síncrono controlado no construtor (não há contexto async aqui)
                Client.AuthenticateAsync(loginEnv!, passwordEnv!).GetAwaiter().GetResult();
                var token = Client.GetAccessToken();
                Console.WriteLine($"[AUTH] Autenticado. Token length={token?.Length}");

                // Ajustar automaticamente TestAccountId se estiver usando placeholder
                var placeholder = string.IsNullOrWhiteSpace(TestAccountId) || TestAccountId.Contains("test-account", StringComparison.OrdinalIgnoreCase) || TestAccountId.Length < 10;
                if (placeholder)
                {
                    try
                    {
                        var accounts = Client.GetAccountsAsync().GetAwaiter().GetResult();
                        var first = accounts.FirstOrDefault();
                        if (first?.Id != null)
                        {
                            TestAccountId = first.Id;
                            Console.WriteLine($"[AUTH] TestAccountId atualizado para id real: {TestAccountId}");
                        }
                        else
                        {
                            Console.WriteLine("[AUTH][WARN] Nenhuma conta retornada para atualizar TestAccountId.");
                        }
                    }
                    catch (Exception exAcc)
                    {
                        Console.WriteLine($"[AUTH][WARN] Falha ao obter contas para definir TestAccountId: {exAcc.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("[AUTH] Variáveis MB_LOGIN/MB_PASSWORD ausentes. Endpoints privados podem ser pulados.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH][WARN] Falha ao autenticar automaticamente: {ex.Message}");
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