using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Errors;
using System.Text.Json;

// Small utility to:
// 1. Authenticate using login (API token id) and password (API token secret)
// 2. List accounts
// 3. List balances for each account
// Usage:
//   dotnet run --project samples/AuthBalanceConsole -- <login> <password>
// or define environment variables MB_LOGIN and MB_PASSWORD

var argList = args.ToList();
var verbose = argList.Remove("--verbose") || Environment.GetEnvironmentVariable("MB_VERBOSE") == "1";
var runDiagnostics = argList.Remove("--diag");
var allowMutations = argList.Remove("--allow-mutate") || Environment.GetEnvironmentVariable("MB_ALLOW_MUTATE") == "1";

string? login = null;
string? password = null;

// After removing flags, the first two remaining arguments can be login and password
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
    // For diagnostics authentication is optional: if no credentials, only public endpoints will be tested.
    if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
    {
        Console.WriteLine("[DIAG] No credentials - only public endpoints will be tested.");
    }
    else if (verbose)
    {
        Console.WriteLine("[DIAG] Credentials detected - private endpoints included.");
    }

    var diag = new AuthBalanceConsole.EndpointDiagnostics(login, password, allowMutations);
    await diag.RunAsync();
    return 0;
}

if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
{
    Console.WriteLine("Missing parameters. Usage: dotnet run -- <login> <password> [--verbose] [--diag] [--allow-mutate] or define MB_LOGIN / MB_PASSWORD. For detailed log use --verbose or MB_VERBOSE=1.");
    return 1;
}

var client = MercadoBitcoin.Client.Extensions.MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

try
{
    Console.WriteLine("== Authenticating ==");
    await client.AuthenticateAsync(login, password);
    Console.WriteLine("Authenticated successfully (token stored internally).");

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
        Console.WriteLine("[DEBUG] Authentication completed. Next call: /accounts");
    }

    Console.WriteLine("== Accounts ==");
    var accounts = await client.GetAccountsAsync();
    if (verbose)
    {
        Console.WriteLine($"[DEBUG] /accounts returned {accounts?.Count ?? 0} records");
    }
    if (accounts == null || accounts.Count == 0)
    {
        Console.WriteLine("No accounts returned.");
        return 0;
    }

    var allData = new List<object>();
    foreach (var acct in accounts)
    {
        Console.WriteLine($"Account: {acct.Id} | {acct.Name} | {acct.Currency} ({acct.Type})");
        try
        {
            var balances = await client.GetBalancesAsync(acct.Id!);
            if (verbose)
            {
                Console.WriteLine($"[DEBUG] /accounts/{acct.Id}/balances returned {balances.Count} items");
            }
            allData.Add(new { account = acct, balances });
            Console.WriteLine("  Balances:");
            foreach (var b in balances)
            {
                Console.WriteLine($"    {b.Symbol}: available={b.Available} on_hold={b.On_hold} total={b.Total}");
            }
        }
        catch (Exception exBal)
        {
            Console.WriteLine($"  Error getting balances: {exBal.Message}");
            if (verbose)
            {
                Console.WriteLine(exBal);
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine("== Consolidated JSON ==");
    var json = JsonSerializer.Serialize(allData, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}
catch (MercadoBitcoinApiException apiEx)
{
    Console.WriteLine("API Failure:");
    Console.WriteLine($"  Code: {apiEx.Error.Code}");
    Console.WriteLine($"  Message: {apiEx.Error.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.WriteLine("Unexpected error: " + ex.Message);
    Console.WriteLine(ex);
    return 3;
}
finally
{
    client.Dispose();
}

return 0;
