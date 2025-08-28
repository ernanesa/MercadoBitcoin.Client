using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client
{
    /// <summary>
    /// Wrappers adicionais (diagnóstico) para endpoints públicos que não tinham métodos de conveniência.
    /// </summary>
    public partial class MercadoBitcoinClient
    {
        // Duplicated wrappers removidos: já existem métodos equivalentes em MercadoBitcoinClient.Public.cs (GetOrderBookAsync, GetTradesAsync, GetAssetFeesAsync)
        // Este arquivo permanece para futura expansão diagnóstica se necessário.
    }
}
