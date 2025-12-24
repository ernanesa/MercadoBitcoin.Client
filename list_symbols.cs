using System;
using System.Linq;
using System.Threading.Tasks;
using MercadoBitcoin.Client;

var client = new MercadoBitcoinClient();
var symbols = await client.GetSymbolsAsync();
Console.WriteLine($"Total symbols: {symbols.Symbol.Count}");
foreach (var symbol in symbols.Symbol.Take(10))
{
    Console.WriteLine($"- {symbol}");
}
