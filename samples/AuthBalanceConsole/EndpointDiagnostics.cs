using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;

namespace AuthBalanceConsole;

internal class EndpointDiagnostics
{
    private readonly MercadoBitcoinClient _client;
    private readonly bool _hasAuth;
    private readonly bool _allowMutations;
    private readonly string? _login;
    private readonly string? _password;
    private string? _accountId;

    public EndpointDiagnostics(string? login, string? password, bool allowMutations)
    {
        _login = login;
        _password = password;
        _allowMutations = allowMutations;
        _client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
        _hasAuth = !string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(password);
    }

    public async Task RunAsync()
    {
        var results = new List<ProbeResult>();

        if (_hasAuth)
        {
            results.Add(await Probe("POST /authorize", ProbeType.Auth, async () =>
            {
                await _client.AuthenticateAsync(_login!, _password!);
                return new { tokenLen = _client.GetAccessToken()?.Length };
            }));
        }

        // Public endpoints
        results.Add(await Probe("GET /BTC-BRL/orderbook", ProbeType.Public, async () => await _client.GetOrderBookAsync("BTC-BRL", limit: "50")));
        results.Add(await Probe("GET /BTC-BRL/trades", ProbeType.Public, async () => await _client.GetTradesAsync("BTC-BRL", limit: 50)));
        results.Add(await Probe("GET /asset/BTC/fees", ProbeType.Public, async () => await _client.GetAssetFeesAsync("BTC", null)));

        // Accounts (private)
        if (_hasAuth)
        {
            var accountsResult = await Probe("GET /accounts", ProbeType.Private, async () =>
            {
                var accounts = await _client.GetAccountsAsync();
                _accountId = accounts.FirstOrDefault()?.Id;
                return new { count = accounts.Count, first = _accountId };
            });
            results.Add(accountsResult);

            if (_accountId != null)
            {
                results.Add(await Probe("GET /accounts/{id}/balances", ProbeType.Private, async () => await _client.GetBalancesAsync(_accountId)));
                results.Add(await Probe("GET /accounts/{id}/tier", ProbeType.Private, async () => await _client.GetTierAsync(_accountId)));
                results.Add(await Probe("GET /accounts/{id}/positions", ProbeType.Private, async () => await _client.GetPositionsAsync(_accountId)));
                results.Add(await Probe("GET /accounts/{id}/wallet/limits", ProbeType.Private, async () => await _client.GetWithdrawLimitsAsync(_accountId)));
                results.Add(await Probe("GET /accounts/{id}/wallet/brl/withdraw-config", ProbeType.Private, async () => await _client.GetBrlWithdrawConfigAsync(_accountId)));
                results.Add(await Probe("GET /accounts/{id}/wallet/withdraw/addresses", ProbeType.Private, async () => await _client.GetWithdrawCryptoWalletAddressesAsync(_accountId)));
                results.Add(await Probe("GET /accounts/{id}/wallet/withdraw/bank-accounts", ProbeType.Private, async () => await _client.GetWithdrawBankAccountsAsync(_accountId)));
            }
        }

        // Trading-related (list orders/trades) — require accountId & symbol
        if (_hasAuth && _accountId != null)
        {
            results.Add(await Probe("GET /accounts/{id}/orders?symbol=BTC-BRL", ProbeType.Private, async () => await _client.ListOrdersAsync("BTC-BRL", _accountId)));
            results.Add(await Probe("GET /accounts/{id}/trades/BTC-BRL", ProbeType.Private, async () => await _client.GetTradesAsync("BTC-BRL")));
        }

        // Mutating endpoints: POST order, cancel all, withdraw, etc. (skip unless allowed)
        if (_hasAuth && _accountId != null && _allowMutations)
        {
            results.Add(await Probe("POST /accounts/{id}/orders (PLACE ORDER)", ProbeType.Mutate, async () =>
            {
                var payload = new PlaceOrderRequest
                {
                    Qty = "0.00001",
                    Side = "buy",
                    Type = "limit",
                    LimitPrice = 1
                };
                var placed = await _client.PlaceOrderAsync("BTC-BRL", _accountId, payload);
                return new { placed.OrderId };
            }));
        }

        // Serialize report
        var report = new
        {
            generatedAtUtc = DateTime.UtcNow,
            authUsed = _hasAuth,
            accountId = _accountId,
            allowMutations = _allowMutations,
            results = results.OrderBy(r => r.Name).ToList()
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText("diagnostics-report.json", json);

        PrintSummary(results);

        _client.Dispose();
    }

    private void PrintSummary(IEnumerable<ProbeResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("=== DIAGNOSTICS SUMMARY ===");
        foreach (var r in results)
        {
            Console.WriteLine($"{r.Type,-7} | {(r.Success ? "OK" : "FAIL"),-4} | {r.Name} | {(r.Success ? r.DurationMs + "ms" : r.ErrorCode)} | {r.ErrorMessage}");
        }
        Console.WriteLine("Report saved to diagnostics-report.json");
    }

    private async Task<ProbeResult> Probe(string name, ProbeType type, Func<Task<object>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var obj = await action();
            sw.Stop();
            return new ProbeResult
            {
                Name = name,
                Type = type,
                Success = true,
                DurationMs = (int)sw.Elapsed.TotalMilliseconds,
                PayloadPreview = Truncate(JsonSerializer.Serialize(obj), 300)
            };
        }
        catch (MercadoBitcoinApiException apiException)
        {
            sw.Stop();
            return new ProbeResult
            {
                Name = name,
                Type = type,
                Success = false,
                DurationMs = (int)sw.Elapsed.TotalMilliseconds,
                ErrorCode = apiException.Error.Code,
                ErrorMessage = apiException.Error.Message
            };
        }
        catch (Exception exception)
        {
            sw.Stop();
            return new ProbeResult
            {
                Name = name,
                Type = type,
                Success = false,
                DurationMs = (int)sw.Elapsed.TotalMilliseconds,
                ErrorCode = exception.GetType().Name,
                ErrorMessage = exception.Message
            };
        }
    }

    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}

internal record ProbeResult
{
    public string Name { get; set; } = string.Empty;
    public ProbeType Type { get; set; }
    public bool Success { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PayloadPreview { get; set; }
}

internal enum ProbeType { Public, Private, Auth, Mutate }
