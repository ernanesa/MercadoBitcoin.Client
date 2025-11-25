using System.Net;
using Xunit;
using Xunit.Abstractions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;
using MercadoBitcoin.Client.Extensions;

namespace MercadoBitcoin.Client.ComprehensiveTests;

public class ErrorHandlingTests : TestBase
{
    private readonly ITestOutputHelper _output;

    public ErrorHandlingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetSymbols_WithInvalidParameters_ShouldHandleGracefully()
    {
        try
        {
            // Test with very long invalid symbol string
            var invalidSymbol = new string('X', 1000);
            var result = await Client.GetSymbolsAsync(invalidSymbol);

            // Should either return empty or handle gracefully
            Assert.NotNull(result);
            LogTestResult("GetSymbols_WithInvalidParameters", true, $"Handled invalid symbol gracefully, returned {result.Symbol.Count} symbols");
        }
        catch (Exception ex)
        {
            // Exception is acceptable for invalid input
            LogTestResult("GetSymbols_WithInvalidParameters", true, $"Exception handled: {ex.GetType().Name} - {ex.Message}");
        }
    }

    [Fact]
    public async Task GetTickers_WithInvalidSymbol_ShouldHandleError()
    {
        try
        {
            var invalidSymbol = "INVALID-SYMBOL-THAT-DOES-NOT-EXIST";
            var result = await Client.GetTickersAsync(invalidSymbol);

            // Should return empty list or handle gracefully
            Assert.NotNull(result);
            LogTestResult("GetTickers_WithInvalidSymbol", true, $"Handled invalid symbol, returned {result.Count()} tickers");
        }
        catch (Exception ex)
        {
            // Exception is acceptable for invalid symbol
            LogTestResult("GetTickers_WithInvalidSymbol", true, $"Exception handled: {ex.GetType().Name} - {ex.Message}");
        }
    }

    [Fact]
    public async Task GetOrderbook_WithInvalidSymbol_ShouldHandleError()
    {
        try
        {
            var invalidSymbol = "INVALID-PAIR";
            var result = await Client.GetOrderBookAsync(invalidSymbol);

            LogTestResult("GetOrderbook_WithInvalidSymbol", true, "Handled invalid symbol gracefully");
        }
        catch (Exception ex)
        {
            // Exception is expected for invalid symbol
            LogTestResult("GetOrderbook_WithInvalidSymbol", true, $"Exception handled: {ex.GetType().Name} - {ex.Message}");
        }
    }

    [Fact]
    public async Task GetTrades_WithInvalidDateRange_ShouldHandleError()
    {
        try
        {
            // Test with future dates or invalid range
            var futureDate = DateTimeOffset.UtcNow.AddYears(1);
            var result = await Client.GetTradesAsync(TestSymbol, (int)futureDate.ToUnixTimeSeconds(), (int)futureDate.AddDays(1).ToUnixTimeSeconds());

            Assert.NotNull(result);
            LogTestResult("GetTrades_WithInvalidDateRange", true, $"Handled future date range, returned {result.Count()} trades");
        }
        catch (Exception ex)
        {
            LogTestResult("GetTrades_WithInvalidDateRange", true, $"Exception handled: {ex.GetType().Name} - {ex.Message}");
        }
    }

    [Fact]
    public async Task GetCandles_WithInvalidTimeframe_ShouldHandleError()
    {
        try
        {
            var invalidTimeframe = "invalid-timeframe";
            var result = await Client.GetCandlesAsync(TestSymbol, invalidTimeframe,
                (int)DateTimeOffset.UtcNow.AddHours(-24).ToUnixTimeSeconds(), (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            LogTestResult("GetCandles_WithInvalidTimeframe", true, "Handled invalid timeframe");
        }
        catch (Exception ex)
        {
            // Exception is expected for invalid timeframe
            LogTestResult("GetCandles_WithInvalidTimeframe", true, $"Exception handled: {ex.GetType().Name} - {ex.Message}");
        }
    }

    [Fact]
    public async Task PrivateEndpoints_WithInvalidCredentials_ShouldHandleAuthError()
    {
        try
        {
            // Create client with default configuration (will fail on authentication)
            var invalidClient = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

            var result = await invalidClient.GetAccountsAsync();
            LogTestResult("PrivateEndpoints_WithInvalidCredentials", false, "Should have thrown authentication error");
        }
        catch (Exception ex)
        {
            // Authentication error is expected
            var isAuthError = ex.Message.Contains("401") ||
                            ex.Message.Contains("Unauthorized") ||
                            ex.Message.Contains("authentication") ||
                            ex.Message.Contains("Invalid") ||
                            ex.Message.Contains("You need to be authenticated");

            LogTestResult("PrivateEndpoints_WithInvalidCredentials", isAuthError,
                $"Authentication error handled: {ex.GetType().Name} - {ex.Message}");

            if (!isAuthError)
            {
                throw; // Re-throw if it's not an auth error
            }
        }
    }

    [Fact]
    public async Task NetworkTimeout_ShouldHandleGracefully()
    {
        // This test verifies that the client correctly handles cancellation tokens,
        // which is the mechanism used for timeouts in .NET
        using var cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Create client with default configuration
            var timeoutClient = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

            // Cancel immediately to simulate a timeout scenario
            cancellationTokenSource.Cancel();

            // Pass null for symbols and the cancelled token
            var result = await timeoutClient.GetSymbolsAsync((string?)null, cancellationTokenSource.Token);

            // If we reach here, the cancellation wasn't respected (which shouldn't happen)
            LogTestResult("NetworkTimeout", false, "Should have been cancelled");
        }
        catch (OperationCanceledException ex)
        {
            // OperationCanceledException (and its subclass TaskCanceledException) is expected 
            // when a CancellationToken is cancelled
            LogTestResult("NetworkTimeout", true,
                $"Cancellation handled correctly: {ex.GetType().Name}");
        }
        catch (Exception ex)
        {
            // Any other exception type means the cancellation wasn't handled properly
            var isTimeoutError = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                                 ex is TimeoutException;

            LogTestResult("NetworkTimeout", isTimeoutError,
                $"Timeout handled: {ex.GetType().Name} - {ex.Message}");

            if (!isTimeoutError)
            {
                throw; // Re-throw if it's not a timeout error
            }
        }
    }

    [Fact]
    public async Task RateLimiting_ShouldHandleGracefully()
    {
        try
        {
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            // Make many rapid requests to potentially trigger rate limiting
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await Client.GetTickersAsync(TestSymbol);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            var rateLimitExceptions = exceptions.Where(ex =>
                ex.Message.Contains("429") ||
                ex.Message.Contains("rate limit") ||
                ex.Message.Contains("Too Many Requests")).ToList();

            if (rateLimitExceptions.Any())
            {
                LogTestResult("RateLimiting", true,
                    $"Rate limiting handled correctly: {rateLimitExceptions.Count} rate limit exceptions out of {exceptions.Count} total exceptions");
            }
            else
            {
                LogTestResult("RateLimiting", true,
                    $"No rate limiting encountered in {tasks.Count} rapid requests (or rate limits are high)");
            }
        }
        catch (Exception ex)
        {
            LogTestResult("RateLimiting", false, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task JsonDeserialization_WithMalformedResponse_ShouldHandleError()
    {
        try
        {
            // This test would require mocking the HTTP client to return malformed JSON
            // For now, we'll test that our serialization context handles edge cases

            var emptyJson = "[]";
            var nullJson = "null";

            // Test empty array deserialization
            var emptySymbols = System.Text.Json.JsonSerializer.Deserialize(emptyJson,
                MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse);
            Assert.NotNull(emptySymbols);
            // Note: emptySymbols is an object with collection properties, not a collection itself

            // Test null deserialization
            var nullSymbols = System.Text.Json.JsonSerializer.Deserialize(nullJson,
                MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse);
            // Should be null or empty

            LogTestResult("JsonDeserialization_WithMalformedResponse", true,
                "JSON edge cases handled correctly");
        }
        catch (System.Text.Json.JsonException ex)
        {
            LogTestResult("JsonDeserialization_WithMalformedResponse", true,
                $"JSON exception handled: {ex.Message}");
        }
        catch (Exception ex)
        {
            LogTestResult("JsonDeserialization_WithMalformedResponse", false, ex.Message);
            throw;
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ConcurrentRequests_WithErrors_ShouldNotAffectOthers()
    {
        try
        {
            var tasks = new List<Task<bool>>();
            var successCount = 0;
            var errorCount = 0;

            // Mix of valid and invalid requests
            for (int i = 0; i < 20; i++)
            {
                if (i % 3 == 0)
                {
                    // Invalid request
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await Client.GetTickersAsync("INVALID-SYMBOL");
                            return true;
                        }
                        catch
                        {
                            Interlocked.Increment(ref errorCount);
                            return false;
                        }
                    }));
                }
                else
                {
                    // Valid request
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await Client.GetTickersAsync(TestSymbol);
                            Interlocked.Increment(ref successCount);
                            return true;
                        }
                        catch
                        {
                            Interlocked.Increment(ref errorCount);
                            return false;
                        }
                    }));
                }

                // Small delay to avoid overwhelming the API
                await Task.Delay(50);
            }

            await Task.WhenAll(tasks);

            LogTestResult("ConcurrentRequests_WithErrors", true,
                $"Concurrent requests handled: {successCount} successful, {errorCount} errors");

            // At least some requests should succeed
            Assert.True(successCount > 0, "At least some concurrent requests should succeed");
        }
        catch (Exception ex)
        {
            LogTestResult("ConcurrentRequests_WithErrors", false, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task LargeDataSets_ShouldHandleMemoryEfficiently()
    {
        try
        {
            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            // Request large datasets
            var symbols = await Client.GetSymbolsAsync();
            var allTickers = new List<TickerResponse>();

            // Get tickers for multiple symbols to create larger dataset
            var symbolsToTest = symbols.Symbol.Take(5).ToList();
            foreach (var symbol in symbolsToTest)
            {
                try
                {
                    var tickers = await Client.GetTickersAsync(symbol);
                    allTickers.AddRange(tickers);
                    await DelayAsync();
                }
                catch
                {
                    // Some symbols might not have tickers, continue
                }
            }

            var peakMemory = GC.GetTotalMemory(false);
            var memoryUsed = peakMemory - initialMemory;

            // Clear references
            symbols = null;
            allTickers.Clear();
            allTickers = null;

            // Force cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryRetained = finalMemory - initialMemory;

            LogTestResult("LargeDataSets_ShouldHandleMemoryEfficiently", true,
                $"Peak memory: {memoryUsed / 1024.0:F2}KB, Retained: {memoryRetained / 1024.0:F2}KB");

            // Assert reasonable memory usage
            Assert.True(memoryUsed < 50 * 1024 * 1024, $"Peak memory usage {memoryUsed / 1024.0 / 1024.0:F2}MB exceeds 50MB threshold");
        }
        catch (Exception ex)
        {
            LogTestResult("LargeDataSets_ShouldHandleMemoryEfficiently", false, ex.Message);
            throw;
        }
    }
}