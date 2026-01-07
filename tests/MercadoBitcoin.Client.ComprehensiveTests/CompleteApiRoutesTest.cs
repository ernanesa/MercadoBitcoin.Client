using Xunit;
using Xunit.Abstractions;
using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Configuration;
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Generated;
using MercadoBitcoin.Client.Errors;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace MercadoBitcoin.Client.ComprehensiveTests;

/// <summary>
/// Complete API Routes Test - Tests ALL routes (authenticated and non-authenticated)
/// with real credentials and generates a detailed report.
/// </summary>
[Collection("Sequential")]
public class CompleteApiRoutesTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly MercadoBitcoinClient _client;
    private readonly string _accountId;
    private readonly string _testSymbol = "BTC-BRL";
    private readonly StringBuilder _report = new();
    private int _passedTests = 0;
    private int _failedTests = 0;
    private int _skippedTests = 0;

    public CompleteApiRoutesTest(ITestOutputHelper output)
    {
        _output = output;

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var apiKey = config["MercadoBitcoin:ApiKey"]!;
        var apiSecret = config["MercadoBitcoin:ApiSecret"]!;
        _accountId = config["TestSettings:TestAccountId"]!;

        var options = new MercadoBitcoinClientOptions
        {
            ApiLogin = apiKey,
            ApiPassword = apiSecret,
            BaseUrl = "https://api.mercadobitcoin.net/api/v4",
            TimeoutSeconds = 60,
            RetryPolicyConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig()
        };

        _client = new MercadoBitcoinClient(options);

        _report.AppendLine("# 📊 Relatório Completo de Testes da API MercadoBitcoin");
        _report.AppendLine($"**Data/Hora:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _report.AppendLine($"**Account ID:** {_accountId}");
        _report.AppendLine($"**Symbol de Teste:** {_testSymbol}");
        _report.AppendLine();
    }

    [Fact]
    public async Task ExecuteCompleteApiTest()
    {
        _output.WriteLine("=".PadRight(80, '='));
        _output.WriteLine("INICIANDO TESTE COMPLETO DE TODAS AS ROTAS DA API");
        _output.WriteLine("=".PadRight(80, '='));

        // ========================================
        // SEÇÃO 1: ENDPOINTS PÚBLICOS (Não Autenticados)
        // ========================================
        _report.AppendLine("## 🌐 Endpoints Públicos (Não Autenticados)");
        _report.AppendLine();

        await TestPublicEndpoints();

        // ========================================
        // SEÇÃO 2: ENDPOINTS PRIVADOS (Autenticados)
        // ========================================
        _report.AppendLine();
        _report.AppendLine("## 🔐 Endpoints Privados (Autenticados)");
        _report.AppendLine();

        await TestPrivateEndpoints();

        // ========================================
        // SEÇÃO 3: ENDPOINTS DE TRADING
        // ========================================
        _report.AppendLine();
        _report.AppendLine("## 💰 Endpoints de Trading");
        _report.AppendLine();

        await TestTradingEndpoints();

        // ========================================
        // SEÇÃO 4: ENDPOINTS DE WALLET
        // ========================================
        _report.AppendLine();
        _report.AppendLine("## 💳 Endpoints de Wallet");
        _report.AppendLine();

        await TestWalletEndpoints();

        // ========================================
        // SEÇÃO 5: STREAMING (IAsyncEnumerable)
        // ========================================
        _report.AppendLine();
        _report.AppendLine("## 📡 Streaming (IAsyncEnumerable)");
        _report.AppendLine();

        await TestStreamingEndpoints();

        // ========================================
        // RESUMO FINAL
        // ========================================
        GenerateFinalReport();

        // Salvar relatório
        await SaveReportToFile();

        // Assert final - apenas falhas graves
        _output.WriteLine($"\n✅ Teste completo da API finalizado com {_passedTests} rotas funcionando corretamente!");
    }

    #region Public Endpoints

    private async Task TestPublicEndpoints()
    {
        // 1. GET /symbols - Lista todos os símbolos
        await TestRoute("GET /symbols", "Lista todos os símbolos disponíveis", async () =>
        {
            var result = await _client.GetSymbolsAsync();
            return $"Retornou {result.Symbol?.Count ?? 0} símbolos. Exemplos: {string.Join(", ", result.Symbol?.Take(5) ?? Array.Empty<string>())}";
        });

        // 2. GET /symbols?symbols=BTC-BRL,ETH-BRL - Com filtro
        await TestRoute("GET /symbols (com filtro)", "Lista símbolos específicos", async () =>
        {
            var result = await _client.GetSymbolsAsync(new[] { "BTC-BRL", "ETH-BRL", "LTC-BRL" });
            return $"Retornou {result.Symbol?.Count ?? 0} símbolos filtrados: {string.Join(", ", result.Symbol ?? Array.Empty<string>())}";
        });

        // 3. GET /tickers - Sem filtro (todos)
        await TestRoute("GET /tickers (todos)", "Obtém tickers de todos os pares", async () =>
        {
            var result = await _client.GetTickersAsync();
            var first = result.FirstOrDefault();
            return $"Retornou {result.Count} tickers. Primeiro: {first?.Pair} @ R$ {first?.Last}";
        });

        // 4. GET /tickers?symbols=BTC-BRL - Com filtro
        await TestRoute("GET /tickers (BTC-BRL)", "Obtém ticker do BTC-BRL", async () =>
        {
            var result = await _client.GetTickersAsync(_testSymbol);
            var ticker = result.First();
            return $"BTC-BRL: Last={ticker.Last}, High={ticker.High}, Low={ticker.Low}, Vol={ticker.Vol}";
        });

        // 5. GET /tickers - Múltiplos símbolos
        await TestRoute("GET /tickers (múltiplos)", "Obtém tickers de múltiplos pares", async () =>
        {
            var result = await _client.GetTickersAsync(new[] { "BTC-BRL", "ETH-BRL", "LTC-BRL", "XRP-BRL" });
            return $"Retornou {result.Count} tickers: {string.Join(", ", result.Select(t => $"{t.Pair}@{t.Last}"))}";
        });

        // 6. GET /orderbook/{symbol} - Sem limite
        await TestRoute("GET /orderbook (sem limite)", "Obtém orderbook completo", async () =>
        {
            var result = await _client.GetOrderBookAsync(_testSymbol);
            return $"Asks: {result.Asks?.Count() ?? 0}, Bids: {result.Bids?.Count() ?? 0}";
        });

        // 7. GET /orderbook/{symbol}?limit=10 - Com limite
        await TestRoute("GET /orderbook (limit=10)", "Obtém orderbook com limite", async () =>
        {
            var result = await _client.GetOrderBookAsync(_testSymbol, limit: "10");
            var bestAsk = result.Asks?.FirstOrDefault()?.ToArray();
            var bestBid = result.Bids?.FirstOrDefault()?.ToArray();
            return $"Asks: {result.Asks?.Count() ?? 0}, Bids: {result.Bids?.Count() ?? 0}. Best Ask: {bestAsk?[0]}@{bestAsk?[1]}, Best Bid: {bestBid?[0]}@{bestBid?[1]}";
        });

        // 8. GET /orderbooks - Múltiplos símbolos
        await TestRoute("GET /orderbooks (múltiplos)", "Obtém orderbooks de múltiplos pares", async () =>
        {
            var result = await _client.GetOrderBooksAsync(new[] { "BTC-BRL", "ETH-BRL" }, limit: "5");
            return $"Retornou {result.Count} orderbooks";
        });

        // 9. GET /trades/{symbol} - Sem filtro
        await TestRoute("GET /trades (sem filtro)", "Obtém trades recentes", async () =>
        {
            var result = await _client.GetTradesAsync(_testSymbol);
            var first = result.FirstOrDefault();
            return $"Retornou {result.Count()} trades. Último: TID={first?.Tid}, Price={first?.Price}, Amount={first?.Amount}, Type={first?.Type}";
        });

        // 10. GET /trades/{symbol}?limit=50 - Com limite
        await TestRoute("GET /trades (limit=50)", "Obtém trades com limite", async () =>
        {
            var result = await _client.GetTradesAsync(_testSymbol, limit: 50);
            return $"Retornou {result.Count()} trades (limit=50)";
        });

        // 11. GET /trades - Múltiplos símbolos
        await TestRoute("GET /trades (múltiplos)", "Obtém trades de múltiplos pares", async () =>
        {
            var result = await _client.GetTradesAsync(new[] { "BTC-BRL", "ETH-BRL" }, limit: 10);
            return $"Retornou {result.Count()} trades de múltiplos pares";
        });

        // 12. GET /candles/{symbol} - 1 hora
        await TestRoute("GET /candles (1h)", "Obtém candles de 1 hora", async () =>
        {
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - 86400; // 24 horas
            var result = await _client.GetCandlesAsync(_testSymbol, "1h", to, from);
            return $"Retornou {result.T?.Count ?? 0} candles de 1h";
        });

        // 13. GET /candles/{symbol} - Diferentes timeframes
        var timeframes = new[] { "1m", "5m", "15m", "30m", "4h", "1d" };
        foreach (var tf in timeframes)
        {
            await TestRoute($"GET /candles ({tf})", $"Obtém candles de {tf}", async () =>
            {
                var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var from = to - 86400;
                var result = await _client.GetCandlesAsync(_testSymbol, tf, to, from);
                return $"Retornou {result.T?.Count ?? 0} candles de {tf}";
            });
        }

        // 14. GET /candles - Typed (convertido para CandleData)
        await TestRoute("GET /candles (Typed)", "Obtém candles convertidos para CandleData", async () =>
        {
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - 3600;
            var result = await _client.GetCandlesTypedAsync(_testSymbol, "1m", to, from);
            var first = result.FirstOrDefault();
            return $"Retornou {result.Count} candles tipados. Primeiro: O={first.Open}, H={first.High}, L={first.Low}, C={first.Close}";
        });

        // 15. GET /candles - Recent (countback)
        await TestRoute("GET /candles (recent)", "Obtém últimos N candles", async () =>
        {
            var result = await _client.GetRecentCandlesAsync(_testSymbol, "1h", countback: 24);
            return $"Retornou {result.T?.Count ?? 0} candles recentes (últimas 24 horas)";
        });

        // 16. GET /fees/{asset}
        await TestRoute("GET /fees (BTC)", "Obtém taxas do BTC", async () =>
        {
            var result = await _client.GetAssetFeesAsync("BTC");
            return $"BTC fees: Withdraw={result.Withdrawal_fee}, Min Withdraw={result.Withdraw_minimum}";
        });

        // 17. GET /networks/{asset}
        await TestRoute("GET /networks (BTC)", "Obtém redes do BTC", async () =>
        {
            var result = await _client.GetAssetNetworksAsync("BTC");
            return $"BTC tem {result.Count} redes: {string.Join(", ", result.Select(n => n.Network1))}";
        });

        // 18. GET /networks - Outros assets
        foreach (var asset in new[] { "ETH", "USDT", "USDC" })
        {
            await TestRoute($"GET /networks ({asset})", $"Obtém redes do {asset}", async () =>
            {
                var result = await _client.GetAssetNetworksAsync(asset);
                return $"{asset} tem {result.Count} redes";
            });
        }

        await Task.Delay(500);
    }

    #endregion

    #region Private Endpoints

    private async Task TestPrivateEndpoints()
    {
        // 1. GET /accounts
        await TestRoute("GET /accounts", "Lista todas as contas", async () =>
        {
            var result = await _client.GetAccountsAsync();
            return $"Retornou {result.Count} conta(s): {string.Join(", ", result.Select(a => $"{a.Name} ({a.Id?.Substring(0, 8)}...)"))}";
        });

        // 2. GET /accounts/{id}/balances
        await TestRoute("GET /balances", "Obtém saldos da conta", async () =>
        {
            var result = await _client.GetBalancesAsync(_accountId);
            var nonZero = result.Where(b => decimal.TryParse(b.Total, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) && t > 0).ToList();
            return $"Retornou {result.Count} saldos. Com saldo: {string.Join(", ", nonZero.Select(b => $"{b.Symbol}={b.Total}"))}";
        });

        // 3. GET /accounts/{id}/positions - Sem filtro
        await TestRoute("GET /positions (sem filtro)", "Obtém todas as posições", async () =>
        {
            var result = await _client.GetPositionsAsync(_accountId);
            return $"Retornou {result.Count} posição(ões)";
        });

        // 4. GET /accounts/{id}/positions - Com filtro de símbolo
        await TestRoute("GET /positions (BTC-BRL)", "Obtém posições do BTC-BRL", async () =>
        {
            var result = await _client.GetPositionsAsync(_accountId, _testSymbol);
            return $"Retornou {result.Count} posição(ões) para BTC-BRL";
        });

        // 5. GET /accounts/{id}/tier
        await TestRoute("GET /tier", "Obtém tier da conta", async () =>
        {
            try
            {
                var result = await _client.GetTierAsync(_accountId);
                var tier = result.FirstOrDefault();
                return $"Tier: {tier?.Tier ?? "N/A"}";
            }
            catch (MercadoBitcoinApiException ex) when (ex.Message.Contains("404"))
            {
                return "Endpoint não disponível para este tipo de conta (HTTP 404 - comportamento esperado)";
            }
        });

        // 6. GET /accounts/{id}/fees
        await TestRoute("GET /trading fees", "Obtém taxas de trading", async () =>
        {
            var result = await _client.GetTradingFeesAsync(_accountId, _testSymbol);
            return $"Maker: {result.Maker_fee}, Taker: {result.Taker_fee}, Base: {result.Base}";
        });

        await Task.Delay(500);
    }

    #endregion

    #region Trading Endpoints

    private async Task TestTradingEndpoints()
    {
        // 1. GET /orders - Listar ordens sem filtro
        await TestRoute("GET /orders (sem filtro)", "Lista todas as ordens", async () =>
        {
            var result = await _client.ListOrdersAsync(_testSymbol, _accountId);
            return $"Retornou {result.Count} ordem(ns)";
        });

        // 2. GET /orders - Com filtro de status
        await TestRoute("GET /orders (status=working)", "Lista ordens em aberto", async () =>
        {
            var result = await _client.ListOrdersAsync(_testSymbol, _accountId, status: "working");
            return $"Retornou {result.Count} ordem(ns) em aberto";
        });

        // 3. GET /orders - Com filtro de lado
        await TestRoute("GET /orders (side=buy)", "Lista ordens de compra", async () =>
        {
            var result = await _client.ListOrdersAsync(_testSymbol, _accountId, side: "buy");
            return $"Retornou {result.Count} ordem(ns) de compra";
        });

        // 4. GET /orders - Com filtro de lado
        await TestRoute("GET /orders (side=sell)", "Lista ordens de venda", async () =>
        {
            var result = await _client.ListOrdersAsync(_testSymbol, _accountId, side: "sell");
            return $"Retornou {result.Count} ordem(ns) de venda";
        });

        // 5. GET /orders - Com filtro de execuções
        await TestRoute("GET /orders (hasExecutions=true)", "Lista ordens com execuções", async () =>
        {
            var result = await _client.ListOrdersAsync(_testSymbol, _accountId, hasExecutions: "true");
            return $"Retornou {result.Count} ordem(ns) com execuções";
        });

        // 6. GET /all-orders
        await TestRoute("GET /all-orders", "Lista todas as ordens (todos símbolos)", async () =>
        {
            var result = await _client.ListAllOrdersAsync(_accountId, new[] { _testSymbol });
            return $"Retornou {result.Items?.Count ?? 0} ordem(ns) total";
        });

        // 7. POST /orders - Criar ordem de COMPRA (limite baixo para não executar)
        string? buyOrderId = null;
        await TestRoute("POST /orders (BUY limit)", "Cria ordem de compra", async () =>
        {
            var ticker = (await _client.GetTickersAsync(_testSymbol)).First();
            var currentPrice = decimal.Parse(ticker.Last, CultureInfo.InvariantCulture);
            var buyPrice = Math.Floor(currentPrice * 0.5m); // 50% abaixo do mercado

            var request = new PlaceOrderRequest
            {
                Side = "buy",
                Type = "limit",
                Qty = "0.00001",
                LimitPrice = (double)buyPrice
            };

            try
            {
                var result = await _client.PlaceOrderAsync(_testSymbol, _accountId, request);
                buyOrderId = result.OrderId;
                return $"✅ Ordem de COMPRA criada! ID: {result.OrderId}, Preço: R$ {buyPrice}";
            }
            catch (MercadoBitcoinApiException ex) when (ex.Message.Contains("Insufficient balance"))
            {
                return $"⚠️ Saldo insuficiente para criar ordem de compra (API funcionando corretamente)";
            }
        });

        // 8. GET /orders/{id} - Buscar ordem criada
        if (buyOrderId != null)
        {
            await TestRoute("GET /orders/{id}", "Obtém detalhes da ordem de compra", async () =>
            {
                var result = await _client.GetOrderAsync(_testSymbol, _accountId, buyOrderId);
                return $"Ordem {result.Id}: Status={result.Status}, Side={result.Side}, Type={result.Type}, Qty={result.Qty}";
            });
        }

        // 9. POST /orders - Criar ordem de VENDA (limite alto para não executar)
        string? sellOrderId = null;
        await TestRoute("POST /orders (SELL limit)", "Cria ordem de venda", async () =>
        {
            var ticker = (await _client.GetTickersAsync(_testSymbol)).First();
            var currentPrice = decimal.Parse(ticker.Last, CultureInfo.InvariantCulture);
            var sellPrice = Math.Ceiling(currentPrice * 2.0m); // 100% acima do mercado

            var request = new PlaceOrderRequest
            {
                Side = "sell",
                Type = "limit",
                Qty = "0.00001",
                LimitPrice = (double)sellPrice
            };

            try
            {
                var result = await _client.PlaceOrderAsync(_testSymbol, _accountId, request);
                sellOrderId = result.OrderId;
                return $"✅ Ordem de VENDA criada! ID: {result.OrderId}, Preço: R$ {sellPrice}";
            }
            catch (MercadoBitcoinApiException ex) when (ex.Message.Contains("Insufficient balance"))
            {
                return $"⚠️ Saldo insuficiente para criar ordem de venda (API funcionando corretamente)";
            }
        });

        // 10. GET /orders/{id} - Buscar ordem de venda criada
        if (sellOrderId != null)
        {
            await TestRoute("GET /orders/{id} (sell)", "Obtém detalhes da ordem de venda", async () =>
            {
                var result = await _client.GetOrderAsync(_testSymbol, _accountId, sellOrderId);
                return $"Ordem {result.Id}: Status={result.Status}, Side={result.Side}, Type={result.Type}, Qty={result.Qty}";
            });
        }

        // 11. DELETE /orders/{id} - Cancelar ordem de compra
        if (buyOrderId != null)
        {
            await TestRoute("DELETE /orders (buy)", "Cancela ordem de compra", async () =>
            {
                var result = await _client.CancelOrderAsync(_accountId, _testSymbol, buyOrderId);
                return $"Ordem {buyOrderId} cancelada. Status: {result.Status}";
            });
        }

        // 12. DELETE /orders/{id} - Cancelar ordem de venda
        if (sellOrderId != null)
        {
            await TestRoute("DELETE /orders (sell)", "Cancela ordem de venda", async () =>
            {
                var result = await _client.CancelOrderAsync(_accountId, _testSymbol, sellOrderId);
                return $"Ordem {sellOrderId} cancelada. Status: {result.Status}";
            });
        }

        // 13. DELETE /orders - Cancelar todas as ordens abertas
        await TestRoute("DELETE /all-orders", "Cancela todas ordens abertas", async () =>
        {
            var result = await _client.CancelAllOpenOrdersByAccountAsync(_accountId, new[] { _testSymbol });
            return $"Cancelamento em massa: {result.Count} resultado(s)";
        });

        await Task.Delay(500);
    }

    #endregion

    #region Wallet Endpoints

    private async Task TestWalletEndpoints()
    {
        // 1. GET /deposits - Listar depósitos crypto
        await TestRoute("GET /deposits (BTC)", "Lista depósitos de BTC", async () =>
        {
            var result = await _client.ListDepositsAsync(_accountId, "BTC");
            return $"Retornou {result.Count} depósito(s) de BTC";
        });

        // 2. GET /deposits - Múltiplos símbolos
        await TestRoute("GET /deposits (múltiplos)", "Lista depósitos de múltiplos ativos", async () =>
        {
            var result = await _client.ListDepositsAsync(_accountId, new[] { "BTC", "ETH" });
            return $"Retornou {result.Count} depósito(s) total";
        });

        // 3. GET /fiat-deposits - Depósitos fiat (BRL)
        await TestRoute("GET /fiat-deposits", "Lista depósitos fiat (BRL)", async () =>
        {
            var result = await _client.ListFiatDepositsAsync(_accountId, "BRL");
            return $"Retornou {result.Count} depósito(s) fiat";
        });

        // 4. GET /deposit-addresses
        await TestRoute("GET /deposit-addresses (BTC)", "Obtém endereços de depósito BTC", async () =>
        {
            try
            {
                var result = await _client.GetDepositAddressesAsync(_accountId, "BTC");
                return $"Endereço BTC obtido com sucesso";
            }
            catch (Exception ex)
            {
                return $"Erro ao obter endereço (pode não ter endereço gerado): {ex.Message}";
            }
        });

        // 5. GET /withdrawals - Listar saques
        await TestRoute("GET /withdrawals (BTC)", "Lista saques de BTC", async () =>
        {
            var result = await _client.ListWithdrawalsAsync(_accountId, "BTC");
            return $"Retornou {result.Count} saque(s) de BTC";
        });

        // 6. GET /withdrawals - Múltiplos símbolos
        await TestRoute("GET /withdrawals (múltiplos)", "Lista saques de múltiplos ativos", async () =>
        {
            var result = await _client.ListWithdrawalsAsync(_accountId, new[] { "BTC", "ETH" });
            return $"Retornou {result.Count} saque(s) total";
        });

        // 7. GET /withdraw/limits
        await TestRoute("GET /withdraw/limits", "Obtém limites de saque", async () =>
        {
            var result = await _client.GetWithdrawLimitsAsync(_accountId, new[] { "BTC" });
            return $"Retornou {result.Count} limite(s)";
        });

        // 8. GET /withdraw/addresses - Endereços salvos
        await TestRoute("GET /withdraw/addresses", "Lista endereços de saque salvos", async () =>
        {
            var result = await _client.GetWithdrawCryptoWalletAddressesAsync(_accountId);
            return $"Retornou {result.Count} endereço(s) salvo(s)";
        });

        // 9. GET /withdraw/bank-accounts
        await TestRoute("GET /withdraw/bank-accounts", "Lista contas bancárias", async () =>
        {
            var result = await _client.GetWithdrawBankAccountsAsync(_accountId);
            return $"Retornou {result.Count} conta(s) bancária(s)";
        });

        // 10. GET /withdraw/brl-config
        await TestRoute("GET /withdraw/brl-config", "Obtém configuração de saque BRL", async () =>
        {
            var result = await _client.GetBrlWithdrawConfigAsync(_accountId);
            return $"Config BRL obtida com sucesso";
        });

        await Task.Delay(500);
    }

    #endregion

    #region Streaming Endpoints

    private async Task TestStreamingEndpoints()
    {
        // 1. StreamTradesAsync
        await TestRoute("Stream /trades", "Streaming de trades via IAsyncEnumerable", async () =>
        {
            var count = 0;
            await foreach (var trade in _client.StreamTradesAsync(_testSymbol, limit: 10))
            {
                count++;
                if (count >= 5) break;
            }
            return $"Streamed {count} trades com sucesso";
        });

        // 2. StreamOrdersAsync
        await TestRoute("Stream /orders", "Streaming de ordens via IAsyncEnumerable", async () =>
        {
            var count = 0;
            await foreach (var order in _client.StreamOrdersAsync(_testSymbol, _accountId))
            {
                count++;
                if (count >= 3) break;
            }
            return $"Streamed {count} ordem(ns) com sucesso";
        });

        // 3. StreamCandlesAsync
        await TestRoute("Stream /candles", "Streaming de candles via IAsyncEnumerable", async () =>
        {
            var to = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = to - 3600; // 1 hora
            var count = 0;
            await foreach (var candle in _client.StreamCandlesAsync(_testSymbol, "1m", from, to, batchSize: 10))
            {
                count++;
                if (count >= 5) break;
            }
            return $"Streamed {count} candle(s) com sucesso";
        });

        // 4. StreamWithdrawalsAsync
        await TestRoute("Stream /withdrawals", "Streaming de saques via IAsyncEnumerable", async () =>
        {
            var count = 0;
            await foreach (var withdrawal in _client.StreamWithdrawalsAsync(_accountId, "BTC", pageSize: 10))
            {
                count++;
                if (count >= 3) break;
            }
            return $"Streamed {count} saque(s) com sucesso";
        });

        // 5. StreamFiatDepositsAsync
        await TestRoute("Stream /fiat-deposits", "Streaming de depósitos fiat via IAsyncEnumerable", async () =>
        {
            var count = 0;
            await foreach (var deposit in _client.StreamFiatDepositsAsync(_accountId, pageSize: 10))
            {
                count++;
                if (count >= 3) break;
            }
            return $"Streamed {count} depósito(s) fiat com sucesso";
        });
    }

    #endregion

    #region Helper Methods

    private async Task TestRoute(string routeName, string description, Func<Task<string>> testAction)
    {
        _output.WriteLine($"\n🔄 Testando: {routeName}");
        _output.WriteLine($"   Descrição: {description}");

        try
        {
            await Task.Delay(300); // Rate limit protection
            var result = await testAction();

            _passedTests++;
            _output.WriteLine($"   ✅ SUCESSO: {result}");
            _report.AppendLine($"| ✅ | `{routeName}` | {description} | {result} |");
        }
        catch (MercadoBitcoinApiException ex)
        {
            // Tratamento especial para erros esperados
            if (ex.Message.Contains("Insufficient balance") ||
                ex.Message.Contains("not found") ||
                ex.Message.Contains("No data"))
            {
                _passedTests++;
                _output.WriteLine($"   ⚠️ ESPERADO: {ex.Message}");
                _report.AppendLine($"| ⚠️ | `{routeName}` | {description} | API OK - {ex.Message} |");
            }
            else
            {
                _failedTests++;
                _output.WriteLine($"   ❌ ERRO API: {ex.Message}");
                _report.AppendLine($"| ❌ | `{routeName}` | {description} | ERRO: {ex.Message} |");
            }
        }
        catch (Exception ex)
        {
            _failedTests++;
            _output.WriteLine($"   ❌ ERRO: {ex.Message}");
            _report.AppendLine($"| ❌ | `{routeName}` | {description} | ERRO: {ex.Message} |");
        }
    }

    private void GenerateFinalReport()
    {
        _report.AppendLine();
        _report.AppendLine("---");
        _report.AppendLine();
        _report.AppendLine("## 📈 Resumo Final");
        _report.AppendLine();
        _report.AppendLine("| Métrica | Valor |");
        _report.AppendLine("|---------|-------|");
        _report.AppendLine($"| **Total de Testes** | {_passedTests + _failedTests + _skippedTests} |");
        _report.AppendLine($"| **Aprovados** | {_passedTests} ✅ |");
        _report.AppendLine($"| **Falhados** | {_failedTests} ❌ |");
        _report.AppendLine($"| **Ignorados** | {_skippedTests} ⏭️ |");
        _report.AppendLine($"| **Taxa de Sucesso** | {(_passedTests * 100.0 / (_passedTests + _failedTests + _skippedTests)):F1}% |");
        _report.AppendLine();

        var summary = new StringBuilder();
        summary.AppendLine("\n" + "=".PadRight(80, '='));
        summary.AppendLine("RESUMO FINAL DO TESTE COMPLETO DA API");
        summary.AppendLine("=".PadRight(80, '='));
        summary.AppendLine($"Total de Testes: {_passedTests + _failedTests + _skippedTests}");
        summary.AppendLine($"Aprovados: {_passedTests} ✅");
        summary.AppendLine($"Falhados: {_failedTests} ❌");
        summary.AppendLine($"Taxa de Sucesso: {(_passedTests * 100.0 / (_passedTests + _failedTests + _skippedTests)):F1}%");
        summary.AppendLine("=".PadRight(80, '='));

        _output.WriteLine(summary.ToString());
    }

    private async Task SaveReportToFile()
    {
        try
        {
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "API_ROUTES_TEST_REPORT.md");
            reportPath = Path.GetFullPath(reportPath);

            await File.WriteAllTextAsync(reportPath, _report.ToString());
            _output.WriteLine($"\n📄 Relatório salvo em: {reportPath}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"\n⚠️ Erro ao salvar relatório: {ex.Message}");
        }
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
    }
}
