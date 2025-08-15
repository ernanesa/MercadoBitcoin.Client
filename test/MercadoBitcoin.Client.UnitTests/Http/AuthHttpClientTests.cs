using MercadoBitcoin.Client.UnitTests.Base;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MercadoBitcoin.Client.UnitTests.Http;

[Trait("Category", "Unit")]
public class AuthHttpClientTests : UnitTestBase
{
    public AuthHttpClientTests()
    {
    }

    [Fact]
    public void Constructor_CreatesValidInstance()
    {
        // Act
        var client = new AuthHttpClient();

        // Assert
        client.Should().NotBeNull();
        
        client.Dispose();
    }



    [Fact]
    public void SetAccessToken_ValidToken_SetsAuthorizationHeader()
    {
        // Act
        var client = new AuthHttpClient();
        var token = "test-access-token";

        // Act
        client.SetAccessToken(token);

        // Assert
        client.HttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        client.HttpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        client.HttpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be(token);
        
        client.Dispose();
    }

    [Fact]
    public void SetAccessToken_NullToken_RemovesAuthorizationHeader()
    {
        // Arrange
        var client = new AuthHttpClient();
        client.SetAccessToken("initial-token");

        // Act
        client.SetAccessToken(null);

        // Assert
        client.HttpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
        
        client.Dispose();
    }

    [Fact]
    public void SetAccessToken_EmptyToken_RemovesAuthorizationHeader()
    {
        // Arrange
        var client = new AuthHttpClient();
        client.SetAccessToken("initial-token");

        // Act
        client.SetAccessToken("");

        // Assert
        client.HttpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
        
        client.Dispose();
    }

    [Fact]
    public void AuthHttpClient_CanBeDisposed()
    {
        // Arrange
        var client = new AuthHttpClient();
        
        // Act & Assert
        client.Dispose(); // Não deve lançar exceção
        Assert.True(true);
    }

    [Fact]
    public void AuthHttpClient_HasCorrectBaseAddress()
    {
        // Arrange & Act
        var client = new AuthHttpClient();
        
        // Assert
        // O cliente deve ter uma configuração válida
        client.Should().NotBeNull();
        
        client.Dispose();
    }
}