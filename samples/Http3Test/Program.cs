using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

Console.WriteLine("Testing HTTP/3 support for api.mercadobitcoin.net...");

using var handler = new SocketsHttpHandler();
// Force HTTP/3 setup
handler.SslOptions.ApplicationProtocols = new List<System.Net.Security.SslApplicationProtocol>
{
    System.Net.Security.SslApplicationProtocol.Http3,
    System.Net.Security.SslApplicationProtocol.Http2,
    System.Net.Security.SslApplicationProtocol.Http11
};

using var client = new HttpClient(handler);
client.DefaultRequestVersion = HttpVersion.Version30;
client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

try
{
    var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mercadobitcoin.net/api/v4/symbols?type=crypto");
    using var response = await client.SendAsync(request);

    Console.WriteLine($"Status Code: {response.StatusCode}");
    Console.WriteLine($"Protocol Version: {response.Version}");

    if (response.Version == HttpVersion.Version30)
    {
        Console.WriteLine("SUCCESS: HTTP/3 is supported!");
    }
    else
    {
        Console.WriteLine($"FALLBACK: Server negotiated {response.Version}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.ToString());
}
