using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using Microsoft.Extensions.Options;

namespace MercadoBitcoin.Client.Benchmarks;

/// <summary>
/// Performance benchmarks for ticker operations.
/// Measures throughput, latency, and memory allocations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class TickerBenchmarks
{
    private MercadoBitcoinClient _client = null!;
    private HttpClient _httpClient = null!;

    [GlobalSetup]
    public void Setup()
    {
        var handler = new MockHttpMessageHandler();

        // Mock single ticker response
        handler.AddResponse("/tickers?symbols=BTC-BRL", """
        [{
            "pair": "BTC-BRL",
            "high": "500000.00",
            "low": "480000.00",
            "vol": "123.45",
            "last": "495000.00",
            "buy": "494900.00",
            "sell": "495100.00",
            "open": "485000.00",
            "date": 1703433600
        }]
        """);

        // Mock multiple tickers response
        var multipleTickers = new System.Text.StringBuilder("[");
        for (int index = 0; index < 50; index++)
        {
            if (index > 0)
                multipleTickers.Append(',');

            multipleTickers.Append($$"""
            {
                "pair": "ASSET{{index}}-BRL",
                "high": "500000.00",
                "low": "480000.00",
                "vol": "123.45",
                "last": "495000.00",
                "buy": "494900.00",
                "sell": "495100.00",
                "open": "485000.00",
                "date": 1703433600
            }
            """);
        }
        multipleTickers.Append(']');

        handler.AddResponse("/tickers?symbols=", multipleTickers.ToString());

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.mercadobitcoin.net/api/v4")
        };

        var options = Options.Create(new MercadoBitcoinClientOptions
        {
            ApiTokenId = "test",
            ApiTokenSecret = "test",
            EnableRateLimiting = false // Disable for benchmarking
        });

        _client = new MercadoBitcoinClient(_httpClient, options);
    }

    [Benchmark(Description = "Get single ticker")]
    public async Task<Generated.TickerResponse> GetSingleTicker()
    {
        var tickers = await _client.GetTickersAsync(["BTC-BRL"]);
        return tickers.First();
    }

    [Benchmark(Description = "Get 3 tickers")]
    public async Task<ICollection<Generated.TickerResponse>> GetMultipleTickers_3()
    {
        return await _client.GetTickersAsync(["BTC-BRL", "ETH-BRL", "SOL-BRL"]);
    }

    [Benchmark(Description = "Get 50 tickers")]
    public async Task<ICollection<Generated.TickerResponse>> GetMultipleTickers_50()
    {
        var symbols = Enumerable.Range(0, 50).Select(i => $"ASSET{i}-BRL").ToArray();
        return await _client.GetTickersAsync(symbols);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Mock HTTP message handler for testing without network calls.
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses = new();

    public void AddResponse(string urlPattern, string responseBody)
    {
        _responses[urlPattern] = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;

        foreach (var (pattern, response) in _responses)
        {
            if (requestPath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        });
    }
}
