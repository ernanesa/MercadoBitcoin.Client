# MercadoBitcoin.Client

[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![C#](https://img.shields.io/badge/C%23-12.0-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![API](https://img.shields.io/badge/API-v4.0-orange)](https://api.mercadobitcoin.net/api/v4/docs)

Uma biblioteca .NET 9 completa e moderna para integração com a **API v4 do Mercado Bitcoin**. Esta biblioteca oferece acesso a todos os endpoints disponíveis da plataforma, incluindo dados públicos, trading, gestão de contas e operações de carteira.

## 🚀 Características

- ✅ **Cobertura Completa**: Todos os endpoints da API v4 do Mercado Bitcoin
- ✅ **.NET 9**: Framework mais recente com performance otimizada
- ✅ **Async/Await**: Programação assíncrona nativa
- ✅ **Strongly Typed**: Modelos de dados tipados para type safety
- ✅ **OpenAPI Integration**: Cliente gerado automaticamente via NSwag
- ✅ **Clean Architecture**: Código organizado e maintível
- ✅ **Error Handling**: Sistema robusto de tratamento de erros
- ✅ **Retry Policies**: Políticas de retry com Polly para maior robustez
- ✅ **Rate Limit Compliant**: Respeita os limites da API
- ✅ **Production Ready**: Pronto para uso em produção

## 📦 Instalação

```bash
# Via Package Manager Console
Install-Package MercadoBitcoin.Client

# Via .NET CLI
dotnet add package MercadoBitcoin.Client

# Via PackageReference
<PackageReference Include="MercadoBitcoin.Client" Version="1.0.1" />
```

## 🔄 Retry Policies

A biblioteca inclui políticas de retry robustas usando **Polly** para melhor confiabilidade e recuperação automática de falhas temporárias.

### Configuração Básica com Retry

```csharp
using MercadoBitcoin.Client;
// Criar cliente com retry policies
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();

// Autenticar
await client.AuthenticateAsync("seu_login", "sua_senha");
```

### Configurações de Retry Personalizadas

```csharp
using MercadoBitcoin.Client.Http;

// Configuração para trading (mais agressiva)
var tradingConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();
// 5 tentativas, delay inicial de 0.5s, backoff de 1.5x, máximo 10s

// Configuração para dados públicos (mais conservadora)
var publicConfig = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();
// 2 tentativas, delay inicial de 2s, backoff de 2x, máximo 30s

// Configuração customizada
var customConfig = new RetryPolicyConfig
{
    MaxRetryAttempts = 3,
    BaseDelaySeconds = 1.0,
    BackoffMultiplier = 2.0,
    MaxDelaySeconds = 30.0,
    RetryOnTimeout = true,
    RetryOnRateLimit = true,
    RetryOnServerErrors = true
};
```

### Cenários de Retry

O sistema automaticamente faz retry nos seguintes casos:
- ⏱️ **Timeouts** (RequestTimeout)
- 🚦 **Rate Limiting** (TooManyRequests - 429)
- 🔥 **Erros de Servidor** (5xx - InternalServerError, BadGateway, ServiceUnavailable, GatewayTimeout)
- 🌐 **Falhas de Rede** (HttpRequestException, TaskCanceledException)

### Recuperação Automática

O sistema automaticamente se recupera de falhas temporárias através das políticas de retry, proporcionando maior robustez às aplicações que utilizam a biblioteca.

## 🏗️ Arquitetura

```
MercadoBitcoin.Client/
├── 📁 Public Data      → Dados públicos (tickers, orderbook, trades, candles)
├── 📁 Account          → Gestão de contas (saldos, tier, posições, taxas)
├── 📁 Trading          → Operações de trading (ordens, execuções, cancelamentos)
├── 📁 Wallet           → Carteira (depósitos, saques, endereços, limites)
└── 📁 Authentication   → Sistema de autenticação Bearer Token
```

## 📊 Endpoints Suportados

### 🔓 Dados Públicos
| Endpoint | Método | Descrição |
|----------|---------|-----------|
| `/{asset}/fees` | GET | Taxas de retirada do ativo |
| `/{symbol}/orderbook` | GET | Livro de ofertas |
| `/{symbol}/trades` | GET | Histórico de negociações |
| `/candles` | GET | Dados de candlestick (OHLCV) |
| `/symbols` | GET | Informações dos instrumentos |
| `/tickers` | GET | Preços atuais |
| `/{asset}/networks` | GET | Redes disponíveis para o ativo |

### 🔐 Conta e Autenticação
| Endpoint | Método | Descrição |
|----------|---------|-----------|
| `/authorize` | POST | Autenticação via login/senha |
| `/accounts` | GET | Lista de contas do usuário |
| `/accounts/{accountId}/balances` | GET | Saldos da conta |
| `/accounts/{accountId}/tier` | GET | Tier de taxas da conta |
| `/accounts/{accountId}/{symbol}/fees` | GET | Taxas de trading |
| `/accounts/{accountId}/positions` | GET | Posições abertas |

### 📈 Trading
| Endpoint | Método | Descrição |
|----------|---------|-----------|
| `/accounts/{accountId}/{symbol}/orders` | GET/POST | Listar/Criar ordens |
| `/accounts/{accountId}/{symbol}/orders/{orderId}` | GET/DELETE | Consultar/Cancelar ordem |
| `/accounts/{accountId}/orders` | GET | Todas as ordens |
| `/accounts/{accountId}/cancel_all_open_orders` | DELETE | Cancelar todas ordens abertas |

### 💰 Carteira
| Endpoint | Método | Descrição |
|----------|---------|-----------|
| `/accounts/{accountId}/wallet/{symbol}/deposits` | GET | Histórico de depósitos |
| `/accounts/{accountId}/wallet/{symbol}/deposits/addresses` | GET | Endereços de depósito |
| `/accounts/{accountId}/wallet/fiat/{symbol}/deposits` | GET | Depósitos fiat (BRL) |
| `/accounts/{accountId}/wallet/{symbol}/withdraw` | GET/POST | Consultar/Solicitar saques |
| `/accounts/{accountId}/wallet/{symbol}/withdraw/{withdrawId}` | GET | Consultar saque específico |
| `/accounts/{accountId}/wallet/withdraw/config/limits` | GET | Limites de saque |
| `/accounts/{accountId}/wallet/withdraw/config/BRL` | GET | Configuração de saque BRL |
| `/accounts/{accountId}/wallet/withdraw/addresses` | GET | Endereços de saque |
| `/accounts/{accountId}/wallet/withdraw/bank-accounts` | GET | Contas bancárias |

## 💻 Exemplos de Uso

### Configuração Inicial

```csharp
using MercadoBitcoin.Client;

// Cliente para dados públicos (sem autenticação)
var client = new MercadoBitcoinClient();

// Cliente autenticado
var authenticatedClient = new MercadoBitcoinClient();
await authenticatedClient.AuthenticateAsync("seu_api_token_id", "seu_api_token_secret");
```

### 📊 Dados Públicos

```csharp
// Obter lista de todos os símbolos disponíveis
var symbols = await client.GetSymbolsAsync();
Console.WriteLine($"Símbolos disponíveis: {symbols.Symbol.Count}");

// Obter ticker do Bitcoin
var tickers = await client.GetTickersAsync("BTC-BRL");
var btcTicker = tickers.First();
Console.WriteLine($"BTC: R$ {btcTicker.Last}");

// Obter livro de ofertas
var orderBook = await client.GetOrderBookAsync("BTC-BRL", limit: "10");
Console.WriteLine($"Melhor oferta de compra: R$ {orderBook.Bids[0][0]}");
Console.WriteLine($"Melhor oferta de venda: R$ {orderBook.Asks[0][0]}");

// Obter histórico de negociações
var trades = await client.GetTradesAsync("BTC-BRL", limit: 100);
Console.WriteLine($"Últimas {trades.Count} negociações obtidas");

// Obter dados de candles/gráficos
var to = DateTimeOffset.Now.ToUnixTimeSeconds();
var candles = await client.GetCandlesAsync("BTC-BRL", "1h", (int)to, countback: 24);
Console.WriteLine($"OHLCV das últimas 24 horas obtidas");

// Obter taxas de retirada de um ativo
var fees = await client.GetAssetFeesAsync("BTC");
Console.WriteLine($"Taxa de retirada BTC: {fees.Withdrawal_fee}");

// Obter redes disponíveis para um ativo
var networks = await client.GetAssetNetworksAsync("USDC");
foreach (var network in networks)
{
    Console.WriteLine($"USDC disponível na rede: {network.Network}");
}
```

### 👤 Gestão de Conta

```csharp
// Obter informações das contas
var accounts = await authenticatedClient.GetAccountsAsync();
var account = accounts.First();
Console.WriteLine($"Conta: {account.Name} ({account.Currency})");

// Obter saldos
var balances = await authenticatedClient.GetBalancesAsync(account.Id);
foreach (var balance in balances)
{
    Console.WriteLine($"{balance.Symbol}: {balance.Available} (disponível) + {balance.On_hold} (reservado)");
}

// Obter tier de taxas
var tier = await authenticatedClient.GetTierAsync(account.Id);
Console.WriteLine($"Tier atual: {tier.First().Tier}");

// Obter taxas de trading para um símbolo
var tradingFees = await authenticatedClient.GetTradingFeesAsync(account.Id, "BTC-BRL");
Console.WriteLine($"Taxa maker: {tradingFees.Maker_fee}% | Taxa taker: {tradingFees.Taker_fee}%");

// Obter posições abertas
var positions = await authenticatedClient.GetPositionsAsync(account.Id);
foreach (var position in positions)
{
    Console.WriteLine($"Posição {position.Side} {position.Instrument}: {position.Qty} @ R$ {position.AvgPrice}");
}
```

### 📈 Trading

```csharp
var accountId = accounts.First().Id;

// Criar ordem de compra limitada
var buyOrder = new PlaceOrderRequest
{
    Side = "buy",
    Type = "limit",
    Qty = "0.001",              // Quantidade em BTC
    LimitPrice = 280000,        // Preço limite em R$
    ExternalId = "minha-ordem-001"
};

var placedOrder = await authenticatedClient.PlaceOrderAsync("BTC-BRL", accountId, buyOrder);
Console.WriteLine($"Ordem criada: {placedOrder.OrderId}");

// Criar ordem de venda com stop-loss
var sellOrderWithStop = new PlaceOrderRequest
{
    Side = "sell",
    Type = "stoplimit",
    Qty = "0.001",
    LimitPrice = 270000,        // Preço de venda
    StopPrice = 275000          // Preço de ativação do stop
};

var stopOrder = await authenticatedClient.PlaceOrderAsync("BTC-BRL", accountId, sellOrderWithStop);

// Listar ordens abertas
var openOrders = await authenticatedClient.ListOrdersAsync("BTC-BRL", accountId, status: "working");
Console.WriteLine($"Você tem {openOrders.Count} ordens abertas");

// Consultar uma ordem específica
var orderDetail = await authenticatedClient.GetOrderAsync("BTC-BRL", accountId, placedOrder.OrderId);
Console.WriteLine($"Status da ordem: {orderDetail.Status}");
Console.WriteLine($"Quantidade executada: {orderDetail.FilledQty}");

// Cancelar uma ordem
var cancelResult = await authenticatedClient.CancelOrderAsync(accountId, "BTC-BRL", placedOrder.OrderId);
Console.WriteLine($"Cancelamento: {cancelResult.Status}");

// Listar todas as ordens (todos os símbolos)
var allOrders = await authenticatedClient.ListAllOrdersAsync(accountId, status: "filled", size: "50");
Console.WriteLine($"Você tem {allOrders.Items.Count} ordens executadas");

// Cancelar todas as ordens abertas (cuidado!)
// var cancelAll = await authenticatedClient.CancelAllOpenOrdersByAccountAsync(accountId);
```

### 💰 Operações de Carteira

```csharp
// Obter endereço para depósito de Bitcoin
var btcAddress = await authenticatedClient.GetDepositAddressesAsync(accountId, "BTC");
Console.WriteLine($"Endereço BTC: {btcAddress.Addresses.First().Hash}");

// Obter endereço para depósito de USDC na rede Ethereum
var usdcAddress = await authenticatedClient.GetDepositAddressesAsync(accountId, "USDC", Network2.Ethereum);
Console.WriteLine($"Endereço USDC (ETH): {usdcAddress.Addresses.First().Hash}");

// Listar histórico de depósitos
var deposits = await authenticatedClient.ListDepositsAsync(accountId, "BTC", limit: "10");
foreach (var deposit in deposits)
{
    Console.WriteLine($"Depósito: {deposit.Amount} {deposit.Coin} - Status: {deposit.Status}");
}

// Listar depósitos fiat (BRL)
var fiatDeposits = await authenticatedClient.ListFiatDepositsAsync(accountId, "BRL", limit: "10");
foreach (var deposit in fiatDeposits)
{
    Console.WriteLine($"Depósito PIX: R$ {deposit.Amount} - Status: {deposit.Status}");
}

// Solicitar saque de Bitcoin
var withdrawRequest = new WithdrawCoinRequest
{
    Address = "bc1qs62xef6x0tyxsz87fya6le7htc6q5wayhqdzen",
    Quantity = "0.001",
    Tx_fee = "0.00005",
    Description = "Saque para carteira pessoal",
    Network = "bitcoin"
};

var withdraw = await authenticatedClient.WithdrawCoinAsync(accountId, "BTC", withdrawRequest);
Console.WriteLine($"Saque solicitado: ID {withdraw.Id}");

// Solicitar saque em Reais para conta bancária
var brlWithdrawRequest = new WithdrawCoinRequest
{
    Account_ref = 1,              // ID da conta bancária cadastrada
    Quantity = "1000.00",
    Description = "Saque para conta corrente"
};

var brlWithdraw = await authenticatedClient.WithdrawCoinAsync(accountId, "BRL", brlWithdrawRequest);

// Listar histórico de saques
var withdrawals = await authenticatedClient.ListWithdrawalsAsync(accountId, "BTC", page: 1, page_size: 10);
foreach (var withdrawal in withdrawals)
{
    Console.WriteLine($"Saque: {withdrawal.Net_quantity} {withdrawal.Coin} - Status: {withdrawal.Status}");
}

// Consultar saque específico
var withdrawalDetail = await authenticatedClient.GetWithdrawalAsync(accountId, "BTC", withdraw.Id.ToString());
Console.WriteLine($"Status: {withdrawalDetail.Status} | TX: {withdrawalDetail.Tx}");

// Obter limites de saque
var limits = await authenticatedClient.GetWithdrawLimitsAsync(accountId, symbols: "BTC,ETH,BRL");
Console.WriteLine("Limites de saque disponíveis obtidos");

// Obter configurações de saque BRL
var brlConfig = await authenticatedClient.GetBrlWithdrawConfigAsync(accountId);
Console.WriteLine($"Limite mínimo BRL: R$ {brlConfig.Limit_min}");
Console.WriteLine($"Limite máximo poupança: R$ {brlConfig.Saving_limit_max}");

// Listar endereços de carteira cadastrados
var walletAddresses = await authenticatedClient.GetWithdrawCryptoWalletAddressesAsync(accountId);
foreach (var address in walletAddresses)
{
    Console.WriteLine($"{address.Asset}: {address.Address}");
}

// Listar contas bancárias cadastradas
var bankAccounts = await authenticatedClient.GetWithdrawBankAccountsAsync(accountId);
foreach (var account in bankAccounts)
{
    Console.WriteLine($"{account.Bank_name}: {account.Recipient_name} - {account.Account_type}");
}
```

## ⚡ Rate Limits

A biblioteca respeita automaticamente os rate limits da API do Mercado Bitcoin:

- **Dados Públicos**: 1 request/segundo
- **Trading**: 3 requests/segundo (criação/cancelamento), 10 requests/segundo (consultas)
- **Conta**: 3 requests/segundo
- **Carteira**: Varia por endpoint
- **Cancel All Orders**: 1 request/minuto

## 🔒 Segurança

### Autenticação
- Utiliza o sistema de **Bearer Token** do Mercado Bitcoin
- Tokens são gerenciados automaticamente pela biblioteca
- Suporte a renovação automática de tokens

### Boas Práticas
- Nunca exponha suas credenciais de API em código fonte
- Use variáveis de ambiente ou Azure Key Vault para credenciais
- Implemente retry policies com backoff exponencial
- Configure timeouts apropriados

```csharp
// ✅ Bom
var apiKey = Environment.GetEnvironmentVariable("MB_API_KEY");
var apiSecret = Environment.GetEnvironmentVariable("MB_API_SECRET");
await client.AuthenticateAsync(apiKey, apiSecret);

// ❌ Ruim
await client.AuthenticateAsync("hardcoded_key", "hardcoded_secret");
```

## 🔧 Configuração Avançada

```csharp
// Cliente HTTP customizado
var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(30);

var client = new MercadoBitcoinClient();
// Configurações adicionais podem ser implementadas
```

## 🚨 Tratamento de Erros

```csharp
try
{
    var orderResult = await client.PlaceOrderAsync("BTC-BRL", accountId, orderRequest);
}
catch (MercadoBitcoinApiException ex)
{
    Console.WriteLine($"Erro da API: {ex.Code} - {ex.Message}");
    
    // Tratar erros específicos
    switch (ex.Code)
    {
        case "INSUFFICIENT_BALANCE":
            Console.WriteLine("Saldo insuficiente para a operação");
            break;
        case "INVALID_SYMBOL":
            Console.WriteLine("Símbolo inválido");
            break;
        case "ORDER_NOT_FOUND":
            Console.WriteLine("Ordem não encontrada");
            break;
        default:
            Console.WriteLine($"Erro não tratado: {ex.Code}");
            break;
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Erro de rede: {ex.Message}");
}
catch (TaskCanceledException ex)
{
    Console.WriteLine($"Timeout: {ex.Message}");
}
```

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Executar apenas testes unitários
dotnet test tests/MercadoBitcoin.Client.Tests/

# Executar testes de integração
dotnet test MercadoBitcoin.Client.IntegrationTests/
```

## 📚 Documentação Adicional

- [Documentação Oficial da API](https://api.mercadobitcoin.net/api/v4/docs)
- [OpenAPI Specification](https://api.mercadobitcoin.net/api/v4/docs/swagger.yaml)
- [Taxas e Limites](https://www.mercadobitcoin.com.br/taxas-contas-limites)
- [Central de Ajuda](https://central.ajuda.mercadobitcoin.com.br/)

## 🤝 Contribuição

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## ⚠️ Disclaimer

Esta biblioteca é um projeto independente e não é oficialmente mantida pelo Mercado Bitcoin. Use por sua conta e risco. Sempre teste em ambiente de desenvolvimento antes de usar em produção.

---

**Desenvolvido com ❤️ para a comunidade .NET Brasil**

[![GitHub stars](https://img.shields.io/github/stars/seu-usuario/MercadoBitcoin.Client?style=social)](https://github.com/seu-usuario/MercadoBitcoin.Client/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/seu-usuario/MercadoBitcoin.Client?style=social)](https://github.com/seu-usuario/MercadoBitcoin.Client/network/members)
