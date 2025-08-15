using System;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.IntegrationTests.Base;

/// <summary>
/// Classe base para testes de integração com setup comum
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected MercadoBitcoinClient? Client;
    protected string? AccountId;

    protected IntegrationTestBase()
    {
    }

    /// <summary>
    /// Cria um cliente básico sem autenticação
    /// </summary>
    protected virtual MercadoBitcoinClient CreateClient()
    {
        return new MercadoBitcoinClient();
    }

    /// <summary>
    /// Cria um cliente autenticado (apenas se credenciais estão disponíveis)
    /// </summary>
    protected virtual async Task<MercadoBitcoinClient> CreateAuthenticatedClientAsync()
    {
        if (!TestConfig.HasRealCredentials)
        {
            throw new InvalidOperationException("Credenciais reais não estão configuradas para este teste");
        }

        var client = CreateClient();
        await client.AuthenticateAsync(TestConfig.ClientId, TestConfig.ClientSecret);
        return client;
    }

    /// <summary>
    /// Cria um cliente autenticado e obtém o primeiro account ID
    /// </summary>
    protected virtual async Task<(MercadoBitcoinClient client, string accountId)> CreateAuthenticatedClientWithAccountAsync()
    {
        var client = await CreateAuthenticatedClientAsync();
        var accounts = await client.GetAccountsAsync();
        var accountId = accounts.First().Id;
        return (client, accountId);
    }

    /// <summary>
    /// Verifica se um teste deve ser executado (baseado na disponibilidade de credenciais)
    /// </summary>
    protected virtual bool ShouldRunTest(bool requiresAuthentication = true)
    {
        if (requiresAuthentication && !TestConfig.HasRealCredentials)
        {
            // Pula o teste se credenciais não estão disponíveis
            return false;
        }
        return true;
    }

    /// <summary>
    /// Executa um teste que requer autenticação, pulando se credenciais não estão disponíveis
    /// </summary>
    protected async Task RunAuthenticatedTestAsync(Func<MercadoBitcoinClient, Task> testAction)
    {
        if (!ShouldRunTest(requiresAuthentication: true))
        {
            return; // Pula o teste
        }

        var client = await CreateAuthenticatedClientAsync();
        try
        {
            await testAction(client);
        }
        finally
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// Executa um teste que requer autenticação e account ID
    /// </summary>
    protected async Task RunAuthenticatedTestWithAccountAsync(Func<MercadoBitcoinClient, string, Task> testAction)
    {
        if (!ShouldRunTest(requiresAuthentication: true))
        {
            return; // Pula o teste
        }

        var (client, accountId) = await CreateAuthenticatedClientWithAccountAsync();
        try
        {
            await testAction(client, accountId);
        }
        finally
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// Executa um teste público (não requer autenticação)
    /// </summary>
    protected async Task RunPublicTestAsync(Func<MercadoBitcoinClient, Task> testAction)
    {
        var client = CreateClient();
        try
        {
            await testAction(client);
        }
        finally
        {
            client.Dispose();
        }
    }



    /// <summary>
    /// Aguarda um tempo específico (útil para testes de rate limiting)
    /// </summary>
    protected static async Task WaitAsync(TimeSpan delay)
    {
        await Task.Delay(delay);
    }

    /// <summary>
    /// Aguarda um tempo específico em milissegundos
    /// </summary>
    protected static async Task WaitAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }

    public virtual void Dispose()
    {
        Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}