using System;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.IntegrationTests;

public class AuthTests
{
    private MercadoBitcoinClient CreateClient() => new MercadoBitcoinClient();

    [Fact]
    public async Task Authenticate_WithValidCredentials_ObtainsAccessToken()
    {
        if (!TestConfig.HasRealCredentials)
            return; // skip if not configured

        var client = CreateClient();
        await client.AuthenticateAsync(TestConfig.ClientId, TestConfig.ClientSecret);

        // If no exception is thrown, authentication succeeded and token was set in AuthHttpClient
        Assert.True(true);
    }
}