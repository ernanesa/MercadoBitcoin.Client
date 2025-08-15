using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace MercadoBitcoin.Client.UnitTests.Base;

/// <summary>
/// Classe base para testes unitários com helpers comuns
/// </summary>
public abstract class UnitTestBase : IDisposable
{
    protected UnitTestBase()
    {
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}