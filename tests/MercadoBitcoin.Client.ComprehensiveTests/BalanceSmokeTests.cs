using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Extensions;

namespace MercadoBitcoin.Client.ComprehensiveTests;

/// <summary>
/// Teste simples (smoke) para validar fluxo de autenticação e obtenção de saldo.
/// Ele só executa de fato se as variáveis de ambiente MB_LOGIN e MB_PASSWORD estiverem presentes.
/// Caso contrário, é ignorado silenciosamente (passa sem fazer chamadas).
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
            Console.WriteLine("[SMOKE BALANCE] Variáveis MB_LOGIN/MB_PASSWORD ausentes. Teste ignorado.");
            return; // Considerado sucesso/ignorado
        }

        var trace = Environment.GetEnvironmentVariable("MB_TRACE_HTTP") == "1";

        var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
        try
        {
            await client.AuthenticateAsync(login!, password!);
            var token = client.GetAccessToken();
            Console.WriteLine($"[SMOKE BALANCE] Autenticado. Token len={token?.Length}.");

            var accounts = await client.GetAccountsAsync();
            Assert.NotNull(accounts);
            Assert.NotEmpty(accounts);
            var first = accounts.First();
            Console.WriteLine($"[SMOKE BALANCE] Primeira conta: id={first.Id} currency={first.Currency} type={first.Type}");

            var balances = await client.GetBalancesAsync(first.Id!);
            Assert.NotNull(balances);
            Console.WriteLine($"[SMOKE BALANCE] {balances.Count} ativos retornados");

            foreach (var b in balances.OrderByDescending(b => decimal.TryParse(b.Total, out var d) ? d : 0))
            {
                Console.WriteLine($"  {b.Symbol} => available={b.Available} on_hold={b.On_hold} total={b.Total}");
            }

            // Validação básica: se existir BRL, total >= 0
            var brl = balances.FirstOrDefault(x => x.Symbol == "BRL");
            if (brl != null && decimal.TryParse(brl.Total, out var brlTotal))
            {
                Assert.True(brlTotal >= 0, "Saldo BRL não pode ser negativo");
            }
        }
        catch (MercadoBitcoinApiException apiEx)
        {
            Console.WriteLine($"[SMOKE BALANCE][API ERROR] Code={apiEx.Error.Code} Message={apiEx.Error.Message}");
            throw; // Falha real — queremos ver no pipeline
        }
        finally
        {
            client.Dispose();
        }
    }
}
