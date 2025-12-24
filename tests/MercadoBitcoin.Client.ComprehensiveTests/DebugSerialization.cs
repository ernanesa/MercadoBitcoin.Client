using System;
using System.Text.Json;
using FluentAssertions;
using MercadoBitcoin.Client.WebSocket.Messages;
using Xunit;
using Xunit.Abstractions;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    public class DebugSerialization
    {
        private readonly ITestOutputHelper _output;

        public DebugSerialization(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestTickerDeserialization()
        {
            var json = "{\"type\":\"ticker\",\"id\":\"BRLBTC\",\"ts\":1766594413719967177,\"data\":{\"high\":\"489065.00000000\",\"low\":\"478731.00000000\",\"vol\":\"22.19770645\",\"last\":\"483714.00000000\",\"buy\":\"483560.00000000\",\"sell\":\"484052.00000000\",\"open\":\"487009.00000000\",\"date\":1766594413}}";

            var ticker = JsonSerializer.Deserialize(json, MercadoBitcoinJsonSerializerContext.Default.TickerMessage);

            _output.WriteLine($"Id: {ticker?.Id}");
            _output.WriteLine($"Instrument: {ticker?.Instrument}");
            _output.WriteLine($"EffectiveInstrument: {ticker?.EffectiveInstrument}");

            ticker.Should().NotBeNull();
            ticker.Id.Should().Be("BRLBTC");
            ticker.EffectiveInstrument.Should().Be("BRLBTC");
        }
    }
}