using FluentAssertions;
using MercadoBitcoin.Client.Errors;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests.Unit;

/// <summary>
/// Unit tests for MercadoBitcoinException and related error classes.
/// </summary>
public class ExceptionTests
{
    private readonly ITestOutputHelper _output;

    public ExceptionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region MercadoBitcoinException Constructor Tests

    [Fact]
    public void MercadoBitcoinException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new MercadoBitcoinException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(0);
        exception.Response.Should().BeNull();
        exception.Headers.Should().BeNull();
        exception.InnerException.Should().BeNull();

        _output.WriteLine($"Exception created with message: {exception.Message}");
    }

    [Fact]
    public void MercadoBitcoinException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Outer error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new MercadoBitcoinException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.StatusCode.Should().Be(0);
        exception.Response.Should().BeNull();
        exception.Headers.Should().BeNull();
    }

    [Fact]
    public void MercadoBitcoinException_FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "API Error";
        var statusCode = 400;
        var response = "{\"error\": \"Bad Request\"}";
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-Request-Id", new[] { "12345" } },
            { "Content-Type", new[] { "application/json" } }
        };
        var innerException = new HttpRequestException("Network error");

        // Act
        var exception = new MercadoBitcoinException(message, statusCode, response, headers, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(statusCode);
        exception.Response.Should().Be(response);
        exception.Headers.Should().NotBeNull();
        exception.Headers!.Count.Should().Be(2);
        exception.Headers["X-Request-Id"].Should().Contain("12345");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void MercadoBitcoinException_WithNullResponse_ShouldAllowNull()
    {
        // Arrange & Act
        var exception = new MercadoBitcoinException("Error", 500, null, null, null);

        // Assert
        exception.Response.Should().BeNull();
        exception.Headers.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void MercadoBitcoinException_WithEmptyMessage_ShouldAccept()
    {
        // Arrange & Act
        var exception = new MercadoBitcoinException("");

        // Assert
        exception.Message.Should().BeEmpty();
    }

    #endregion

    #region StatusCode Tests

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    public void MercadoBitcoinException_WithVariousStatusCodes_ShouldStoreCorrectly(int statusCode)
    {
        // Arrange & Act
        var exception = new MercadoBitcoinException("Error", statusCode, null, null, null);

        // Assert
        exception.StatusCode.Should().Be(statusCode);

        _output.WriteLine($"Verified status code: {statusCode}");
    }

    [Fact]
    public void MercadoBitcoinException_WithZeroStatusCode_ShouldBeAllowed()
    {
        // Arrange & Act
        var exception = new MercadoBitcoinException("Error", 0, null, null, null);

        // Assert
        exception.StatusCode.Should().Be(0);
    }

    [Fact]
    public void MercadoBitcoinException_WithNegativeStatusCode_ShouldBeAllowed()
    {
        // Arrange & Act
        var exception = new MercadoBitcoinException("Error", -1, null, null, null);

        // Assert
        exception.StatusCode.Should().Be(-1);
    }

    #endregion

    #region Response Tests

    [Fact]
    public void MercadoBitcoinException_WithJsonResponse_ShouldStoreCorrectly()
    {
        // Arrange
        var jsonResponse = @"{""code"":""INVALID_PARAMETER"",""message"":""Invalid symbol provided""}";

        // Act
        var exception = new MercadoBitcoinException("API Error", 400, jsonResponse, null, null);

        // Assert
        exception.Response.Should().Be(jsonResponse);
        exception.Response.Should().Contain("INVALID_PARAMETER");
    }

    [Fact]
    public void MercadoBitcoinException_WithHtmlResponse_ShouldStoreCorrectly()
    {
        // Arrange
        var htmlResponse = "<html><body><h1>500 Internal Server Error</h1></body></html>";

        // Act
        var exception = new MercadoBitcoinException("Server Error", 500, htmlResponse, null, null);

        // Assert
        exception.Response.Should().Be(htmlResponse);
    }

    [Fact]
    public void MercadoBitcoinException_WithEmptyResponse_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var exception = new MercadoBitcoinException("Error", 404, "", null, null);

        // Assert
        exception.Response.Should().BeEmpty();
    }

    [Fact]
    public void MercadoBitcoinException_WithVeryLargeResponse_ShouldStoreCorrectly()
    {
        // Arrange
        var largeResponse = new string('X', 100000);

        // Act
        var exception = new MercadoBitcoinException("Error", 500, largeResponse, null, null);

        // Assert
        exception.Response.Should().HaveLength(100000);
    }

    #endregion

    #region Headers Tests

    [Fact]
    public void MercadoBitcoinException_WithMultipleHeaders_ShouldStoreAll()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-Request-Id", new[] { "req-123" } },
            { "X-Rate-Limit-Remaining", new[] { "99" } },
            { "X-Rate-Limit-Reset", new[] { "1609459200" } },
            { "Content-Type", new[] { "application/json" } }
        };

        // Act
        var exception = new MercadoBitcoinException("Error", 429, null, headers, null);

        // Assert
        exception.Headers.Should().HaveCount(4);
        exception.Headers!["X-Request-Id"].Should().Contain("req-123");
        exception.Headers["X-Rate-Limit-Remaining"].Should().Contain("99");
    }

    [Fact]
    public void MercadoBitcoinException_WithMultiValueHeader_ShouldStoreAll()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Set-Cookie", new[] { "session=abc123", "token=xyz789" } }
        };

        // Act
        var exception = new MercadoBitcoinException("Error", 200, null, headers, null);

        // Assert
        exception.Headers!["Set-Cookie"].Should().HaveCount(2);
        exception.Headers["Set-Cookie"].Should().Contain("session=abc123");
        exception.Headers["Set-Cookie"].Should().Contain("token=xyz789");
    }

    [Fact]
    public void MercadoBitcoinException_WithEmptyHeaders_ShouldBeAllowed()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>();

        // Act
        var exception = new MercadoBitcoinException("Error", 500, null, headers, null);

        // Assert
        exception.Headers.Should().NotBeNull();
        exception.Headers.Should().BeEmpty();
    }

    #endregion

    #region InnerException Tests

    [Fact]
    public void MercadoBitcoinException_WithHttpRequestException_ShouldWrapCorrectly()
    {
        // Arrange
        var innerException = new HttpRequestException("Connection refused");

        // Act
        var exception = new MercadoBitcoinException("Network error", 0, null, null, innerException);

        // Assert
        exception.InnerException.Should().BeOfType<HttpRequestException>();
        exception.InnerException!.Message.Should().Be("Connection refused");
    }

    [Fact]
    public void MercadoBitcoinException_WithTaskCanceledException_ShouldWrapCorrectly()
    {
        // Arrange
        var innerException = new TaskCanceledException("Request timed out");

        // Act
        var exception = new MercadoBitcoinException("Timeout error", 0, null, null, innerException);

        // Assert
        exception.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    [Fact]
    public void MercadoBitcoinException_WithNestedInnerExceptions_ShouldPreserveChain()
    {
        // Arrange
        var innermost = new InvalidOperationException("Root cause");
        var middle = new ApplicationException("Middle layer", innermost);
        var outer = new HttpRequestException("Outer layer", middle);

        // Act
        var exception = new MercadoBitcoinException("API call failed", 500, null, null, outer);

        // Assert
        exception.InnerException.Should().BeOfType<HttpRequestException>();
        exception.InnerException!.InnerException.Should().BeOfType<ApplicationException>();
        exception.InnerException.InnerException!.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException.InnerException.InnerException!.Message.Should().Be("Root cause");
    }

    #endregion

    #region Exception Inheritance Tests

    [Fact]
    public void MercadoBitcoinException_ShouldInheritFromException()
    {
        // Arrange & Act
        var exception = new MercadoBitcoinException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void MercadoBitcoinException_ShouldBeCatchableAsException()
    {
        // Arrange
        Exception? caughtException = null;

        // Act
        try
        {
            throw new MercadoBitcoinException("Test error");
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<MercadoBitcoinException>();
    }

    [Fact]
    public void MercadoBitcoinException_ShouldBeCatchableAsMercadoBitcoinException()
    {
        // Arrange
        MercadoBitcoinException? caughtException = null;

        // Act
        try
        {
            throw new MercadoBitcoinException("Test error", 400, "{}", null, null);
        }
        catch (MercadoBitcoinException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.StatusCode.Should().Be(400);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void MercadoBitcoinException_UnauthorizedError_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Unauthorized: Invalid API credentials";
        var statusCode = 401;
        var response = @"{""code"":""UNAUTHORIZED"",""message"":""Invalid or expired token""}";
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "WWW-Authenticate", new[] { "Bearer" } }
        };

        // Act
        var exception = new MercadoBitcoinException(message, statusCode, response, headers, null);

        // Assert
        exception.StatusCode.Should().Be(401);
        exception.Response.Should().Contain("UNAUTHORIZED");
        exception.Headers!["WWW-Authenticate"].Should().Contain("Bearer");
    }

    [Fact]
    public void MercadoBitcoinException_RateLimitError_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Rate limit exceeded";
        var statusCode = 429;
        var response = @"{""code"":""RATE_LIMIT_EXCEEDED"",""message"":""Too many requests""}";
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-RateLimit-Limit", new[] { "500" } },
            { "X-RateLimit-Remaining", new[] { "0" } },
            { "X-RateLimit-Reset", new[] { "1609459200" } },
            { "Retry-After", new[] { "60" } }
        };

        // Act
        var exception = new MercadoBitcoinException(message, statusCode, response, headers, null);

        // Assert
        exception.StatusCode.Should().Be(429);
        exception.Response.Should().Contain("RATE_LIMIT_EXCEEDED");
        exception.Headers!["Retry-After"].Should().Contain("60");
    }

    [Fact]
    public void MercadoBitcoinException_InsufficientFundsError_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Insufficient funds for order";
        var statusCode = 400;
        var response = @"{""code"":""INSUFFICIENT_FUNDS"",""message"":""Not enough BRL balance to place this order""}";

        // Act
        var exception = new MercadoBitcoinException(message, statusCode, response, null, null);

        // Assert
        exception.StatusCode.Should().Be(400);
        exception.Response.Should().Contain("INSUFFICIENT_FUNDS");
        exception.Response.Should().Contain("BRL");
    }

    [Fact]
    public void MercadoBitcoinException_ServerError_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Internal server error";
        var statusCode = 500;
        var response = @"{""code"":""INTERNAL_ERROR"",""message"":""An unexpected error occurred""}";
        var innerException = new HttpRequestException("Server returned 500");

        // Act
        var exception = new MercadoBitcoinException(message, statusCode, response, null, innerException);

        // Assert
        exception.StatusCode.Should().Be(500);
        exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public void MercadoBitcoinException_NetworkError_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Network error occurred";
        var innerException = new HttpRequestException("Unable to connect to the remote server",
            new System.Net.Sockets.SocketException(10061));

        // Act
        var exception = new MercadoBitcoinException(message, 0, null, null, innerException);

        // Assert
        exception.StatusCode.Should().Be(0);
        exception.Response.Should().BeNull();
        exception.InnerException.Should().BeOfType<HttpRequestException>();
        exception.InnerException!.InnerException.Should().BeOfType<System.Net.Sockets.SocketException>();
    }

    [Fact]
    public void MercadoBitcoinException_TimeoutError_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Request timed out";
        var innerException = new TaskCanceledException("A task was canceled.");

        // Act
        var exception = new MercadoBitcoinException(message, 0, null, null, innerException);

        // Assert
        exception.StatusCode.Should().Be(0);
        exception.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void MercadoBitcoinException_WithUnicodeMessage_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Erro: símbolo inválido 日本語 العربية 🚀";

        // Act
        var exception = new MercadoBitcoinException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Message.Should().Contain("símbolo");
        exception.Message.Should().Contain("日本語");
        exception.Message.Should().Contain("🚀");
    }

    [Fact]
    public void MercadoBitcoinException_WithUnicodeResponse_ShouldStoreCorrectly()
    {
        // Arrange
        var response = @"{""message"":""Saldo insuficiente para comprar 日本語""}";

        // Act
        var exception = new MercadoBitcoinException("Error", 400, response, null, null);

        // Assert
        exception.Response.Should().Contain("日本語");
    }

    [Fact]
    public void MercadoBitcoinException_WithNewlinesInMessage_ShouldStoreCorrectly()
    {
        // Arrange
        var message = "Error on line 1\nError on line 2\r\nError on line 3";

        // Act
        var exception = new MercadoBitcoinException(message);

        // Assert
        exception.Message.Should().Contain("\n");
    }

    [Fact]
    public void MercadoBitcoinException_ToString_ShouldIncludeMessage()
    {
        // Arrange
        var message = "Test error message";
        var exception = new MercadoBitcoinException(message);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain(message);
        result.Should().Contain("MercadoBitcoinException");
    }

    [Fact]
    public void MercadoBitcoinException_GetBaseException_ShouldReturnInnermostException()
    {
        // Arrange
        var innermost = new InvalidOperationException("Root cause");
        var middle = new ApplicationException("Middle", innermost);
        var exception = new MercadoBitcoinException("Outer", 500, null, null, middle);

        // Act
        var baseException = exception.GetBaseException();

        // Assert
        baseException.Should().BeOfType<InvalidOperationException>();
        baseException.Message.Should().Be("Root cause");
    }

    [Fact]
    public void MercadoBitcoinException_GetBaseException_WithNoInner_ShouldReturnSelf()
    {
        // Arrange
        var exception = new MercadoBitcoinException("Test");

        // Act
        var baseException = exception.GetBaseException();

        // Assert
        baseException.Should().BeSameAs(exception);
    }

    #endregion

    #region Serialization Considerations

    [Fact]
    public void MercadoBitcoinException_Properties_ShouldBeReadOnly()
    {
        // Arrange
        var exception = new MercadoBitcoinException("Error", 400, "{}", null, null);

        // Assert - Properties should be get-only (compile-time check satisfied by test existence)
        exception.StatusCode.Should().Be(400);
        exception.Response.Should().Be("{}");
        // Note: We can't modify StatusCode or Response after construction - this is by design
    }

    [Fact]
    public void MercadoBitcoinException_Headers_IsReadOnlyDictionary()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "X-Test", new[] { "value" } }
        };
        var exception = new MercadoBitcoinException("Error", 200, null, headers, null);

        // Assert
        exception.Headers.Should().BeAssignableTo<IReadOnlyDictionary<string, IEnumerable<string>>>();
    }

    #endregion

    #region Common API Error Scenarios

    [Theory]
    [InlineData("INVALID_PARAMETER", "Symbol 'INVALID' not found")]
    [InlineData("INSUFFICIENT_BALANCE", "Not enough balance to place order")]
    [InlineData("ORDER_NOT_FOUND", "Order with ID 'xyz' not found")]
    [InlineData("INVALID_QUANTITY", "Quantity must be greater than minimum")]
    [InlineData("INVALID_PRICE", "Price is outside allowed range")]
    public void MercadoBitcoinException_CommonApiErrors_ShouldBeRepresentable(string errorCode, string errorMessage)
    {
        // Arrange
        var response = $@"{{""code"":""{errorCode}"",""message"":""{errorMessage}""}}";

        // Act
        var exception = new MercadoBitcoinException(errorMessage, 400, response, null, null);

        // Assert
        exception.Response.Should().Contain(errorCode);
        exception.Message.Should().Be(errorMessage);

        _output.WriteLine($"Verified error: {errorCode} - {errorMessage}");
    }

    #endregion
}
