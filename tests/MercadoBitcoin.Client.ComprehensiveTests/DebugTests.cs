using System;
using System.Threading.Tasks;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using FluentAssertions;
using System.Net.Http;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    public class DebugTests
    {
        [Fact]
        public async Task DebugSymbols()
        {
            using var httpClient = new HttpClient();
            var url = "https://api.mercadobitcoin.net/api/v4/symbols";

            try
            {
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Content: {content.Substring(0, Math.Min(content.Length, 500))}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        [Fact]
        public async Task DebugCandles()
        {
            using var httpClient = new HttpClient();
            var symbol = "BTC-BRL";
            var resolution = "1h";
            var from = 1600000000;
            var to = -1;

            var url = $"https://api.mercadobitcoin.net/api/v4/candles?symbol={symbol}&resolution={resolution}&from={from}&to={to}";

            try
            {
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Content: {content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
