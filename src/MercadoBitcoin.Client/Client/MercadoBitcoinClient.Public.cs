using MercadoBitcoin.Client.Generated;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MercadoBitcoin.Client
{
    public partial class MercadoBitcoinClient
    {
        #region Public Data

        public Task<AssetFee> GetAssetFeesAsync(string asset, string? network = null)
        {
            return _generatedClient.FeesAsync(asset, network);
        }

        public Task<OrderBookResponse> GetOrderBookAsync(string symbol, string? limit = null)
        {
            return _generatedClient.OrderbookAsync(symbol, limit);
        }

        public Task<ICollection<TradeResponse>> GetTradesAsync(string symbol, int? tid = null, int? since = null, int? from = null, int? to = null, int? limit = null)
        {
            return _generatedClient.TradesAsync(symbol, tid, since, from, to, limit);
        }

        public Task<ListCandlesResponse> GetCandlesAsync(string symbol, string resolution, int to, int? from = null, int? countback = null)
        {
            return _generatedClient.CandlesAsync(symbol, resolution, from, to, countback);
        }

        public Task<ListSymbolInfoResponse> GetSymbolsAsync(string? symbols = null)
        {
            return _generatedClient.SymbolsAsync(symbols);
        }

        public Task<ICollection<TickerResponse>> GetTickersAsync(string symbols)
        {
            return _generatedClient.TickersAsync(symbols);
        }

        public Task<ICollection<Network>> GetAssetNetworksAsync(string asset)
        {
            return _generatedClient.NetworksAsync(asset);
        }

        #endregion
    }
}
