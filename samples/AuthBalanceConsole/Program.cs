using MercadoBitcoin.Client;
using System.Text.Json;

// Pequeno utilitário para:
// 1. Autenticar usando login (API token id) e password (API token secret)
// 2. Listar contas
// 3. Listar saldos de cada conta
// Uso:
//   dotnet run --project samples/AuthBalanceConsole -- <login> <password>
// ou definir variáveis de ambiente MB_LOGIN e MB_PASSWORD

var argList = args.ToList();
var verbose = argList.Remove("--verbose") || Environment.GetEnvironmentVariable("MB_VERBOSE") == "1";
var runDiagnostics = argList.Remove("--diag");
var allowMutations = argList.Remove("--allow-mutate") || Environment.GetEnvironmentVariable("MB_ALLOW_MUTATE") == "1";

string? login = null;
string? password = null;

// Após remover flags, os dois primeiros argumentos remanescentes podem ser login e senha
if (argList.Count >= 2)
{
    login = argList[0];
    password = argList[1];
}
else
{
    login = Environment.GetEnvironmentVariable("MB_LOGIN");
    password = Environment.GetEnvironmentVariable("MB_PASSWORD");
}

if (runDiagnostics)
{
    // Para diagnostics a autenticação é opcional: se não houver credenciais, testaremos só endpoints públicos.
    if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
    {
        Console.WriteLine("[DIAG] Sem credenciais - serão testados apenas endpoints públicos.");
    }
    else if (verbose)
    {
        Console.WriteLine("[DIAG] Credenciais detectadas - endpoints privados inclusos.");
    }

    var diag = new AuthBalanceConsole.EndpointDiagnostics(login, password, allowMutations);
    await diag.RunAsync();
    return 0;
}

if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
{
    Console.WriteLine("Parâmetros ausentes. Uso: dotnet run -- <login> <password> [--verbose] [--diag] [--allow-mutate] ou defina MB_LOGIN / MB_PASSWORD. Para log detalhado use --verbose ou MB_VERBOSE=1.");
    return 1;
}

var client = MercadoBitcoin.Client.Extensions.MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

try
{
    Console.WriteLine("== Autenticando ==");
    await client.AuthenticateAsync(login, password);
    Console.WriteLine("Autenticado com sucesso (token armazenado internamente).");

    if (verbose)
    {
        var token = client.GetAccessToken();
        string Mask(string? t)
        {
            if (string.IsNullOrEmpty(t)) return "<null>";
            var head = t.Length > 6 ? t[..6] : t;
            var tail = t.Length > 6 ? t[^6..] : string.Empty;
            return $"{head}...{tail} (len={t.Length})";
        }
        Console.WriteLine($"[DEBUG] Token: {Mask(token)}");
        Console.WriteLine("[DEBUG] Autenticação concluída. Próxima chamada: /accounts");
    }

    Console.WriteLine("== Contas ==");
    var accounts = await client.GetAccountsAsync();
    if (verbose)
    {
        Console.WriteLine($"[DEBUG] /accounts retornou {accounts?.Count ?? 0} registros");
    }
    if (accounts == null || accounts.Count == 0)
    {
        Console.WriteLine("Nenhuma conta retornada.");
        return 0;
    }

    var allData = new List<object>();
    foreach (var acct in accounts)
    {
        Console.WriteLine($"Conta: {acct.Id} | {acct.Name} | {acct.Currency} ({acct.Type})");
        try
        {
            var balances = await client.GetBalancesAsync(acct.Id!);
            if (verbose)
            {
                Console.WriteLine($"[DEBUG] /accounts/{acct.Id}/balances retornou {balances.Count} itens");
            }
            allData.Add(new { account = acct, balances });
            Console.WriteLine("  Saldos:");
            foreach (var b in balances)
            {
                Console.WriteLine($"    {b.Symbol}: available={b.Available} on_hold={b.On_hold} total={b.Total}");
            }
        }
        catch (Exception exBal)
        {
            Console.WriteLine($"  Erro ao obter saldos: {exBal.Message}");
            if (verbose)
            {
                Console.WriteLine(exBal);
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine("== JSON Consolidado ==");
    var json = JsonSerializer.Serialize(allData, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}
catch (MercadoBitcoinApiException apiEx)
{
    Console.WriteLine("Falha de API:");
    Console.WriteLine($"  Code: {apiEx.Error.Code}");
    Console.WriteLine($"  Message: {apiEx.Error.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.WriteLine("Erro inesperado: " + ex.Message);
    Console.WriteLine(ex);
    return 3;
}
finally
{
    client.Dispose();
}

return 0;
