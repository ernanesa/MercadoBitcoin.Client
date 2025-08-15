using MercadoBitcoin.Client.UnitTests.Base;
using System.Net;
using System.Net.Http;

namespace MercadoBitcoin.Client.UnitTests.Client;

[Trait("Category", "Unit")]
public class MercadoBitcoinClientTests : UnitTestBase
{
    public MercadoBitcoinClientTests()
    {
    }

    [Fact]
    public void Constructor_Default_CreatesValidInstance()
    {
        // Act
        var client = new MercadoBitcoinClient();

        // Assert
        client.Should().NotBeNull();

        client.Dispose();
    }

    [Fact]
    public void Constructor_WithHttpClient_CreatesValidInstance()
    {
        // Arrange
        var authClient = new AuthHttpClient();

        // Act
        var client = new MercadoBitcoinClient(authClient);

        // Assert
        client.Should().NotBeNull();

        client.Dispose();
    }

    [Fact]
    public void Constructor_WithNullHttpClient_CreatesDefaultHttpClient()
    {
        // Act
        var client = new MercadoBitcoinClient();

        // Assert
        client.Should().NotBeNull();

        client.Dispose();
    }

    [Fact]
    public void Constructor_WithDefaults_DoesNotThrow()
    {
        // Arrange
        var authClient = new AuthHttpClient();

        // Act & Assert
        var client = new MercadoBitcoinClient(authClient);
        client.Should().NotBeNull();

        client.Dispose();
    }

    [Fact]
    public void Dispose_MultipleCallsDoNotThrow()
    {
        // Arrange
        var client = new MercadoBitcoinClient();

        // Act & Assert
        client.Dispose();
        client.Dispose(); // Segunda chamada não deve lançar exceção
    }

    [Fact]
    public void MercadoBitcoinClient_ImplementsIDisposable()
    {
        // Arrange & Act
        var client = new MercadoBitcoinClient();

        // Assert
        client.Should().BeAssignableTo<IDisposable>();

        client.Dispose();
    }

    [Fact]
    public void MercadoBitcoinClient_CanBeCreatedWithDifferentConfigurations()
    {
        // Arrange & Act
        var client1 = new MercadoBitcoinClient();
        var client2 = new MercadoBitcoinClient();
        var authClient = new AuthHttpClient();
        var client3 = new MercadoBitcoinClient(authClient);

        // Assert
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client3.Should().NotBeNull();

        client1.Dispose();
        client2.Dispose();
        client3.Dispose();
    }
}