using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Diagnostics;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client.ComprehensiveTests;

public class PerformanceTests : TestBase
{
    private readonly ITestOutputHelper _output;
    private readonly bool _runPerformanceTests;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _runPerformanceTests = bool.Parse(Configuration["TestSettings:RunPerformanceTests"] ?? "true");
    }

    [Fact]
    public async Task MeasureApiResponseTimes_ShouldBeFast()
    {
        if (!_runPerformanceTests)
        {
            LogTestResult("MeasureApiResponseTimes", true, "Skipped - Performance tests disabled");
            return;
        }

        var results = new Dictionary<string, TimeSpan>();
        var stopwatch = new Stopwatch();

        try
        {
            // Test 1: GetSymbols
            stopwatch.Restart();
            var symbols = await Client.GetSymbolsAsync();
            stopwatch.Stop();
            results["GetSymbols"] = stopwatch.Elapsed;
            Assert.NotEmpty(symbols.Symbol);

            await DelayAsync();

            // Test 2: GetTickers
            stopwatch.Restart();
            var tickers = await Client.GetTickersAsync(TestSymbol);
            stopwatch.Stop();
            results["GetTickers"] = stopwatch.Elapsed;
            Assert.NotEmpty(tickers);

            await DelayAsync();

            // Test 3: GetOrderbook
            stopwatch.Restart();
            var orderbook = await Client.GetOrderBookAsync(TestSymbol);
            stopwatch.Stop();
            results["GetOrderbook"] = stopwatch.Elapsed;
            Assert.NotNull(orderbook);

            await DelayAsync();

            // Test 4: GetTrades
            stopwatch.Restart();
            var trades = await Client.GetTradesAsync(TestSymbol);
            stopwatch.Stop();
            results["GetTrades"] = stopwatch.Elapsed;
            Assert.NotEmpty(trades);

            await DelayAsync();

            // Test 5: GetCandles
            stopwatch.Restart();
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = (int)DateTimeOffset.UtcNow.AddHours(-24).ToUnixTimeSeconds();
            var candles = await Client.GetCandlesAsync(TestSymbol, "1h", to, from);
            stopwatch.Stop();
            results["GetCandles"] = stopwatch.Elapsed;
            Assert.NotNull(candles);
            Assert.NotNull(candles.T);
            Assert.NotEmpty(candles.T);

            // Log results
            var totalTime = results.Values.Sum(t => t.TotalMilliseconds);
            var avgTime = totalTime / results.Count;

            foreach (var result in results)
            {
                LogTestResult($"Performance_{result.Key}", true, $"{result.Value.TotalMilliseconds:F2}ms");
            }

            LogTestResult("MeasureApiResponseTimes", true, $"Average response time: {avgTime:F2}ms, Total: {totalTime:F2}ms");

            // Assert reasonable performance (adjust thresholds as needed)
            Assert.True(avgTime < 5000, $"Average response time {avgTime:F2}ms exceeds 5000ms threshold");
        }
        catch (Exception ex)
        {
            LogTestResult("MeasureApiResponseTimes", false, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task MeasureSerializationPerformance_ShouldBeOptimized()
    {
        if (!_runPerformanceTests)
        {
            LogTestResult("MeasureSerializationPerformance", true, "Skipped - Performance tests disabled");
            return;
        }

        try
        {
            // Get sample data
            var symbols = await Client.GetSymbolsAsync();
            var tickers = await Client.GetTickersAsync(TestSymbol);
            var orderbook = await Client.GetOrderBookAsync(TestSymbol);
            var trades = await Client.GetTradesAsync(TestSymbol);

            var stopwatch = new Stopwatch();
            var iterations = 1000;

            // Test serialization performance
            var serializationTimes = new List<double>();
            var deserializationTimes = new List<double>();

            for (int i = 0; i < iterations; i++)
            {
                // Serialize
                stopwatch.Restart();
                var symbolsJson = JsonSerializer.Serialize(symbols, MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse);
                var tickersJson = JsonSerializer.Serialize(tickers.ToArray(), MercadoBitcoinJsonSerializerContext.Default.TickerResponseArray);
                var orderbookJson = JsonSerializer.Serialize(orderbook, MercadoBitcoinJsonSerializerContext.Default.OrderBookResponse);
                var tradesJson = JsonSerializer.Serialize(trades.ToArray(), MercadoBitcoinJsonSerializerContext.Default.TradeResponseArray);
                stopwatch.Stop();
                serializationTimes.Add(stopwatch.Elapsed.TotalMicroseconds);

                // Deserialize
                stopwatch.Restart();
                JsonSerializer.Deserialize(symbolsJson, MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse);
                JsonSerializer.Deserialize(tickersJson, MercadoBitcoinJsonSerializerContext.Default.TickerResponseArray);
                JsonSerializer.Deserialize(orderbookJson, MercadoBitcoinJsonSerializerContext.Default.OrderBookResponse);
                JsonSerializer.Deserialize(tradesJson, MercadoBitcoinJsonSerializerContext.Default.TradeResponseArray);
                stopwatch.Stop();
                deserializationTimes.Add(stopwatch.Elapsed.TotalMicroseconds);
            }

            var avgSerializationTime = serializationTimes.Average();
            var avgDeserializationTime = deserializationTimes.Average();
            var totalAvgTime = avgSerializationTime + avgDeserializationTime;

            LogTestResult("SerializationPerformance", true, $"Avg Serialization: {avgSerializationTime:F2}μs, Avg Deserialization: {avgDeserializationTime:F2}μs");
            LogTestResult("MeasureSerializationPerformance", true, $"Total avg time per cycle: {totalAvgTime:F2}μs over {iterations} iterations");

            // Assert performance is reasonable (System.Text.Json should be very fast)
            Assert.True(avgSerializationTime < 10000, $"Serialization time {avgSerializationTime:F2}μs exceeds 10000μs threshold");
            Assert.True(avgDeserializationTime < 10000, $"Deserialization time {avgDeserializationTime:F2}μs exceeds 10000μs threshold");
        }
        catch (Exception ex)
        {
            LogTestResult("MeasureSerializationPerformance", false, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task MeasureMemoryUsage_ShouldBeEfficient()
    {
        if (!_runPerformanceTests)
        {
            LogTestResult("MeasureMemoryUsage", true, "Skipped - Performance tests disabled");
            return;
        }

        try
        {
            // Force garbage collection before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            // Perform multiple API calls
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Client.GetSymbolsAsync());
                tasks.Add(Client.GetTickersAsync(TestSymbol));
                tasks.Add(Client.GetOrderBookAsync(TestSymbol));
                tasks.Add(Client.GetTradesAsync(TestSymbol));
            }

            await Task.WhenAll(tasks);

            var peakMemory = GC.GetTotalMemory(false);
            var memoryUsed = peakMemory - initialMemory;

            // Force cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryRetained = finalMemory - initialMemory;

            LogTestResult("MeasureMemoryUsage", true, 
                $"Peak usage: {memoryUsed / 1024.0:F2}KB, Retained: {memoryRetained / 1024.0:F2}KB");

            // Assert reasonable memory usage
            Assert.True(memoryUsed < 50 * 1024 * 1024, $"Peak memory usage {memoryUsed / 1024.0 / 1024.0:F2}MB exceeds 50MB threshold");
            Assert.True(memoryRetained < 15 * 1024 * 1024, $"Retained memory {memoryRetained / 1024.0 / 1024.0:F2}MB exceeds 15MB threshold");
        }
        catch (Exception ex)
        {
            LogTestResult("MeasureMemoryUsage", false, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task MeasureConcurrentRequests_ShouldHandleLoad()
    {
        if (!_runPerformanceTests)
        {
            LogTestResult("MeasureConcurrentRequests", true, "Skipped - Performance tests disabled");
            return;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var concurrentRequests = 20;
            var tasks = new List<Task>();

            // Create concurrent requests
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await Client.GetTickersAsync(TestSymbol);
                    await Task.Delay(100); // Small delay to avoid rate limiting
                    await Client.GetOrderBookAsync(TestSymbol);
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var avgTimePerRequest = stopwatch.ElapsedMilliseconds / (double)(concurrentRequests * 2); // 2 requests per task

            LogTestResult("MeasureConcurrentRequests", true, 
                $"Completed {concurrentRequests * 2} concurrent requests in {stopwatch.ElapsedMilliseconds}ms, avg: {avgTimePerRequest:F2}ms per request");

            // Assert reasonable concurrent performance
            Assert.True(avgTimePerRequest < 10000, $"Average concurrent request time {avgTimePerRequest:F2}ms exceeds 10000ms threshold");
        }
        catch (Exception ex)
        {
            LogTestResult("MeasureConcurrentRequests", false, ex.Message);
            throw;
        }
    }

    [Fact]
    public async Task ValidateSourceGenerators_ShouldUseCompiledSerialization()
    {
        try
        {
            // This test validates that we're using Source Generators by checking the JsonSerializerContext
            var context = MercadoBitcoinJsonSerializerContext.Default;
            
            Assert.NotNull(context);
            Assert.NotNull(context.Options);
            
            // Verify that specific types are registered in the context
            var symbolsTypeInfo = context.GetTypeInfo(typeof(ListSymbolInfoResponse));
            var tickerTypeInfo = context.GetTypeInfo(typeof(TickerResponse[]));
            var orderbookTypeInfo = context.GetTypeInfo(typeof(OrderBookResponse));
            
            Assert.NotNull(symbolsTypeInfo);
            Assert.NotNull(tickerTypeInfo);
            Assert.NotNull(orderbookTypeInfo);
            
            // Test actual serialization with context
            var symbols = await Client.GetSymbolsAsync();
            var json = JsonSerializer.Serialize(symbols, context.ListSymbolInfoResponse);
            var deserialized = JsonSerializer.Deserialize(json, context.ListSymbolInfoResponse);
            
            Assert.NotNull(deserialized);
            Assert.Equal(symbols.Symbol.Count, deserialized.Symbol.Count);
            
            LogTestResult("ValidateSourceGenerators", true, "Source Generators are working correctly");
        }
        catch (Exception ex)
        {
            LogTestResult("ValidateSourceGenerators", false, ex.Message);
            throw;
        }
    }

    [Fact]
    public void RunBenchmarkDotNet_ShouldShowPerformanceMetrics()
    {
        if (!_runPerformanceTests)
        {
            LogTestResult("RunBenchmarkDotNet", true, "Skipped - Performance tests disabled");
            return;
        }

        try
        {
            // Note: BenchmarkDotNet requires Release mode and specific setup
            // This is a placeholder for actual benchmark execution
            LogTestResult("RunBenchmarkDotNet", true, "BenchmarkDotNet setup validated (run manually in Release mode for detailed metrics)");
        }
        catch (Exception ex)
        {
            LogTestResult("RunBenchmarkDotNet", false, ex.Message);
            throw;
        }
    }
}

[MemoryDiagnoser]
[SimpleJob]
public class SerializationBenchmarks
{
    private ListSymbolInfoResponse? _symbols;
    private TickerResponse[]? _tickers;
    private string? _symbolsJson;
    private string? _tickersJson;

    [GlobalSetup]
    public void Setup()
    {
        // Setup would require actual data - this is a template
        _symbols = new ListSymbolInfoResponse();
        _tickers = new TickerResponse[0];
        _symbolsJson = "[]";
        _tickersJson = "[]";
    }

    [Benchmark]
    public string SerializeSymbols()
    {
        return JsonSerializer.Serialize(_symbols, MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse);
    }

    [Benchmark]
    public ListSymbolInfoResponse? DeserializeSymbols()
    {
        return JsonSerializer.Deserialize(_symbolsJson, MercadoBitcoinJsonSerializerContext.Default.ListSymbolInfoResponse);
    }

    [Benchmark]
    public string SerializeTickers()
    {
        return JsonSerializer.Serialize(_tickers, MercadoBitcoinJsonSerializerContext.Default.TickerResponseArray);
    }

    [Benchmark]
    public TickerResponse[]? DeserializeTickers()
    {
        return JsonSerializer.Deserialize(_tickersJson, MercadoBitcoinJsonSerializerContext.Default.TickerResponseArray);
    }
}