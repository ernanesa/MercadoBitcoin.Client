# Release Notes v4.0.0-alpha.1 (BREAKING CHANGES)

> Esta versão é marcada como **alpha** e destinada a testes e validação
> antes da liberação da versão estável 4.0.0. Use em produção apenas se
> você estiver confortável com possíveis ajustes adicionais.

## 1. Resumo

- Migração da biblioteca para **.NET 10** e **C# 14**.
- Pipeline HTTP modernizado com suporte a **HTTP/2** (padrão) e **HTTP/3** (opcional).
- Remoção de construtores públicos de conveniência de `MercadoBitcoinClient`.
- Configuração padronizada via `MercadoBitcoinClientOptions`, `HttpConfiguration` e `RetryPolicyConfig`.
- Novas extensões para análise de candles com **SIMD** (alta performance).
- Métricas e instrumentação expandidas para observabilidade e rate limiting.

## 2. Breaking Changes

### 2.1 Construtores de `MercadoBitcoinClient`

- Todos os construtores públicos de conveniência (sem parâmetros ou com parâmetros simples)
  foram removidos.
- O fluxo suportado passa a ser:
  - Métodos de fábrica/extension methods:
    - `MercadoBitcoinClientExtensions.CreateWithRetryPolicies(...)`
    - `MercadoBitcoinClientExtensions.CreateWithHttp2(...)`
    - `MercadoBitcoinClientExtensions.CreateForTrading(...)`
    - `MercadoBitcoinClientExtensions.CreateForDevelopment(...)`
  - Ou via DI:
    - `services.AddMercadoBitcoinClient(options => { ... })`

**Antes (obsoleto):**

```csharp
var client = new MercadoBitcoinClient();
```

**Depois (recomendado – factory):**

```csharp
using MercadoBitcoin.Client.Extensions;

var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
```

**Depois (recomendado – DI):**

```csharp
using MercadoBitcoin.Client.Extensions;

builder.Services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = "https://api.mercadobitcoin.net/api/v4";
    options.HttpConfiguration = HttpConfiguration.CreateHttp2Default();
    options.RetryPolicyConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();
});
```

### 2.2 Target Framework e Linguagem

- A biblioteca agora é compilada para **`net10.0`**.
- Código consumidor deve estar em um runtime compatível com .NET 10.
- C# 14 é utilizado no código-fonte da biblioteca.

### 2.3 HTTP/2 e HTTP/3

- **HTTP/2** passa a ser a configuração padrão em todos os factories.
- **HTTP/3** é suportado via `HttpConfiguration.CreateHttp3Default()` para cenários que
  precisem de QUIC (baixa latência, melhores links instáveis, etc).
- Alguns ambientes ainda não oferecem suporte pleno a HTTP/3; por isso, o padrão
  permanece HTTP/2.

### 2.4 Configuração padronizada

- Todas as configurações de HTTP e retry agora são centralizadas em:
  - `MercadoBitcoinClientOptions`
  - `HttpConfiguration`
  - `RetryPolicyConfig`
- Recomenda-se fortemente parar de instanciar `HttpClient` manualmente e
  utilizar:
  - Extension methods (`CreateWithHttp2`, `CreateForTrading`, etc.)
  - Ou `AddMercadoBitcoinClient` com `IHttpClientFactory` e `IOptions<T>`.

## 3. Como Migrar da 3.x para 4.0.0-alpha.1

### 3.1 Código que instancia o client

Substitua qualquer uso de construtores diretos por factories ou DI.

**Antes:**

```csharp
var client = new MercadoBitcoinClient();
```

**Depois (console app / worker):**

```csharp
using MercadoBitcoin.Client.Extensions;

var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
```

**Depois (web API com DI):**

```csharp
builder.Services.AddMercadoBitcoinClient(options =>
{
    options.BaseUrl = builder.Configuration["MercadoBitcoin:BaseUrl"]
                      ?? "https://api.mercadobitcoin.net/api/v4";
    options.HttpConfiguration = HttpConfiguration.CreateHttp2Default();
    options.RetryPolicyConfig = MercadoBitcoinClientExtensions.CreatePublicDataRetryConfig();
});
```

### 3.2 Configuração via `appsettings.json` / `.env`

- Opcionalmente, você pode usar um `appsettings.json` no **seu projeto de aplicação** para centralizar configurações.
- Use variáveis de ambiente semelhantes a `.env.example`:
  - `MB_API_KEY`
  - `MB_API_SECRET`
  - `MB_USER_AGENT`

### 3.3 HTTP/3 (opcional)

Se quiser testar HTTP/3:

```csharp
using MercadoBitcoin.Client.Extensions;
using MercadoBitcoin.Client.Http;

var http3Config = HttpConfiguration.CreateHttp3Default();
var retryConfig = MercadoBitcoinClientExtensions.CreateTradingRetryConfig();

var client = MercadoBitcoinClientExtensions.CreateWithHttp2(retryConfig, http3Config);
```

Lembre-se de validar suporte a HTTP/3 no ambiente (sistema operacional e runtime).

## 4. Novas Funcionalidades

### 4.1 Extensões SIMD para análise de candles

- Novas extensões em `CandleMathExtensions`:
  - `CalculateAverageClose(this ReadOnlySpan<CandleData> candles)`
  - `CalculateMaxHigh(this ReadOnlySpan<CandleData> candles)`
  - `CalculateMinLow(this ReadOnlySpan<CandleData> candles)`
- Usam **AVX2** quando disponível, com fallback seguro para loop escalar.

### 4.2 Métricas e Rate Limiting

- Métricas de rate limiting em `RateLimiterMetrics`.
- Integração com `System.Diagnostics.Metrics` para:
  - Contagem de retries.
  - Abertura/fechamento de circuit breaker.
  - Duração de requisições HTTP.

### 4.3 Documentação

- `README.md` revisado com foco em:
  - Uso via factories e DI.
  - Separação entre dados públicos e privados.
- Novos guias em `docs/`:
  - `docs/USER_GUIDE.md` (guia para humanos).
  - `docs/AI_USAGE_GUIDE.md` (guia para agentes/LLMs).

## 5. Compatibilidade e Regressões Conhecidas

- Esta versão é **alpha**:
  - API pública está estável, mas nomes e detalhes de configuração ainda podem
    sofrer ajustes até a 4.0.0 estável.
  - Se você precisa de máxima estabilidade, mantenha-se na linha 3.x até a
    liberação da 4.0.0 final.

## 6. Próximos Passos

- Validar a biblioteca em cenários reais de:
  - Coleta de dados de mercado (tickers, candles, orderbook).
  - Operações de trading de baixa latência.
  - Ambientes com HTTP/2 vs HTTP/3.
- Abrir issues no repositório em caso de:
  - Erros de migração.
  - Ajustes necessários na API pública.
  - Melhorias sugeridas para ergonomia de uso.
