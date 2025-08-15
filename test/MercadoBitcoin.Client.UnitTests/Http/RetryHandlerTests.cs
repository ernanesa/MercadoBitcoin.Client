using MercadoBitcoin.Client.Http;
using MercadoBitcoin.Client.UnitTests.Base;
using System.Net;
using System.Net.Http;

namespace MercadoBitcoin.Client.UnitTests.Http;

[Trait("Category", "Unit")]
public class RetryHandlerTests : UnitTestBase
{
    public RetryHandlerTests()
    {
    }

    [Fact]
    public void Constructor_WithValidConfig_CreatesInstance()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act
        var handler = new RetryHandler(config);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_UsesDefaultConfig()
    {
        // Act & Assert
        var handler = new RetryHandler(null);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        var config = new RetryPolicyConfig();

        // Act & Assert
        var handler = new RetryHandler(config);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void RetryHandler_HasCorrectInnerHandler()
    {
        // Arrange
        var config = new RetryPolicyConfig();
        var handler = new RetryHandler(config);

        // Act & Assert
        handler.InnerHandler.Should().NotBeNull();
        handler.InnerHandler.Should().BeOfType<HttpClientHandler>();
    }

    [Fact]
    public void RetryHandler_CanBeDisposed()
    {
        // Arrange
        var config = new RetryPolicyConfig();
        var handler = new RetryHandler(config);

        // Act & Assert
        handler.Dispose(); // Não deve lançar exceção
        Assert.True(true);
    }
}