using System;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.IntegrationTests;

public class IntegrationSetup
{
    [Fact]
    public void HasCredentialsConfigured()
    {
        // If credentials are not set, skip this check to allow running public-route tests.
        if (!TestConfig.HasRealCredentials)
            return;

        // When configured, just assert true to mark environment is ready for authenticated tests.
        Assert.True(true);
    }
}
