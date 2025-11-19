using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace MercadoBitcoin.Client.ComprehensiveTests;

/// <summary>
/// Simple smoke test to validate authentication flow and balance retrieval.
/// It only actually runs if the MB_LOGIN and MB_PASSWORD environment variables are present.
/// Otherwise, it is silently ignored (passes without making calls).
/// </summary>
public class BalanceSmokeTests
{
    [Fact]
    public async Task Authenticate_And_GetFirstAccountBalances()
    {
        var login = Environment.GetEnvironmentVariable("MB_LOGIN");
        var password = Environment.GetEnvironmentVariable("MB_PASSWORD");

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("[SMOKE BALANCE] MB_LOGIN/MB_PASSWORD variables missing. Test ignored.");
            return; // Considered success/ignored
        }

        var trace = Environment.GetEnvironmentVariable("MB_TRACE_HTTP") == "1";

        var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
        try
        {
            await client.AuthenticateAsync(login!, password!);
            var token = client.GetAccessToken();
            Console.WriteLine($"[SMOKE BALANCE] Authenticated. Token len={token?.Length}.");

            var accounts = await client.GetAccountsAsync();
            Assert.NotNull(accounts);
            Assert.NotEmpty(accounts);
            var first = accounts.First();
            Console.WriteLine($"[SMOKE BALANCE] First account: id={first.Id} currency={first.Currency} type={first.Type}");

            var balances = await client.GetBalancesAsync(first.Id!);
            Assert.NotNull(balances);
            Console.WriteLine($"[SMOKE BALANCE] {balances.Count} assets returned");

            foreach (var b in balances.OrderByDescending(b => decimal.TryParse(b.Total, out var d) ? d : 0))
            {
                Console.WriteLine($"  {b.Symbol} => available={b.Available} on_hold={b.On_hold} total={b.Total}");
            }

            // Basic validation: if BRL exists, total >= 0
            var brl = balances.FirstOrDefault(x => x.Symbol == "BRL");
            if (brl != null && decimal.TryParse(brl.Total, out var brlTotal))
            {
                Assert.True(brlTotal >= 0, "BRL balance cannot be negative");
            }
        }
        catch (MercadoBitcoinApiException apiEx)
        {
            Console.WriteLine($"[SMOKE BALANCE][API ERROR] Code={apiEx.Error.Code} Message={apiEx.Error.Message}");
            throw; // Real failure — we want to see it in the pipeline
        }
        finally
        {
            client.Dispose();
        }
    }
}
