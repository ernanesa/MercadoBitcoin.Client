# Guia de Uso Orientado para IA

Este documento é autocontido e foi elaborado para que **outros agentes de IA** possam compreender, navegar e utilizar a biblioteca `MercadoBitcoin.Client` de forma segura, eficiente e contextualizada. Inclui modelos de raciocínio, prompts recomendados, mapas de métodos, contratos de entrada/saída e estratégias de recuperação de erro.

> Objetivo primário: permitir que um agente planeje e execute operações de leitura de dados públicos, negociação (trading), gestão de conta e carteira usando esta lib .NET sem ambiguidade.

---
## 1. Visão Geral Concisa
- Namespace raiz: `MercadoBitcoin.Client`
- Cliente principal: `MercadoBitcoinClient` (**instanciado apenas via métodos de extensão ou DI a partir da v3.0.0**)
- Comunicação: HTTPS sobre HTTP/2 (default), JSON, REST.
- Serialização: `System.Text.Json` com Source Generators (contexto `MercadoBitcoinJsonSerializerContext`).
- Estratégia de autenticação: `AuthenticateAsync(login, password)` gera e injeta token Bearer.
- Requisições públicas não exigem token; privadas exigem.
- Retry e resiliência: `RetryHandler` com Polly + backoff exponencial, jitter e circuit breaker manual.
- Jitter: habilitado por padrão para evitar sincronização entre clientes concorrentes.
- Circuit Breaker: abre após falhas consecutivas configuráveis, half-open com probe único.
- Métricas: counters (retries, estados de circuito) + histogram de latência (`mb_client_http_request_duration`).
- Cancelamento: todos os endpoints aceitam `CancellationToken` (propagar para operações longas).
- User-Agent customizável: variável de ambiente `MB_USER_AGENT`.
- Suite de Testes: 64 cenários cobrindo públicos, privados, performance, serialização e resiliência.
- Versão atual documentada: 3.0.0 (Remoção de construtores públicos, métodos de extensão/DI obrigatórios).

---
## 2. Estrutura Lógica (Mapa Mental)
```
MercadoBitcoinClient (instanciado via métodos de extensão ou DI)
 ├── Autenticação: AuthenticateAsync
 ├── Dados Públicos: (GetSymbols, GetTickers, GetOrderBook, GetTrades, GetCandles*, GetAssetFees, GetAssetNetworks)
 ├── Contas: GetAccounts, GetBalances, GetTier, GetTradingFees, GetPositions
 ├── Trading: PlaceOrder, ListOrders, GetOrder, CancelOrder, ListAllOrders, CancelAllOpenOrders
 ├── Carteira: ListDeposits, GetDepositAddresses, ListFiatDeposits, WithdrawCoin, ListWithdrawals,
 │             GetWithdrawal, GetWithdrawLimits, GetBrlWithdrawConfig,
 │             GetWithdrawCryptoWalletAddresses, GetWithdrawBankAccounts
 └── Extensões/Candles Tipados: GetCandlesTypedAsync / GetRecentCandlesTypedAsync
```

---
## 3. Princípios de Uso para um Agente de IA
1. Sempre identificar se a operação é pública ou privada antes de invocar.
2. Autenticar **uma vez** antes de acessar rotas privadas; reutilizar a instância do cliente.
3. Validar parâmetros críticos (ex.: símbolo `BASE-QUOTE`, resolução de candle) antes de chamar para reduzir retries desnecessários.
4. Manter um cache leve de símbolos (`/symbols`) para validações internas de coerência.
5. Implementar fallback de janelas de candles: se `from > to`, o método já faz swap — não duplicar lógica.
6. Em trading, registrar `externalId` (se fornecido) para idempotência lógica.
7. Tratar erros via captura de `MercadoBitcoinApiException` e mapear `ErrorResponse.Code` para estratégia de correção.
8. Evitar spam de chamadas sequenciais: respeitar rate limits (delay mínimo de ~1000 ms se incerteza sobre limites dinâmicos).
9. Propagar `CancellationToken` para permitir que fluxos de decisão reajam rápido a nova estratégia.
10. Usar métricas (taxa de retries, opens de circuito, p95 de latência) como sinais para tuning adaptativo.

---
### 3.1 Componentes de Resiliência
| Componente | Propósito | Principais Campos | Consideração IA |
|------------|----------|-------------------|-----------------|
| Retry (Polly) | Recuperar falhas transitórias | `MaxRetryAttempts`, `BaseDelaySeconds`, `BackoffMultiplier`, `MaxDelaySeconds`, `EnableJitter` | Reduzir tentativas se p95 aumentar muito |
| Circuit Breaker | Fail-fast após falhas consecutivas | `CircuitBreakerFailuresBeforeBreaking`, `CircuitBreakerDurationSeconds` | Quando aberto: suspender operações não críticas |
| Jitter | Desincronizar bursts | `JitterMillisecondsMax` | Aumentar em ambientes de alta concorrência |
| Respeito Retry-After | Evitar penalização | `RespectRetryAfterHeader` | Priorizar header sobre cálculo local |
| Métricas | Observabilidade & tuning | `EnableMetrics` | Necessário para laço adaptativo |

### Campos de `RetryPolicyConfig`
| Campo | Tipo | Default | Descrição |
|-------|------|---------|----------|
| MaxRetryAttempts | int | 3 | Número de tentativas extras |
| BaseDelaySeconds | double | 1.0 | Atraso inicial |
| BackoffMultiplier | double | 2.0 | Fator exponencial |
| MaxDelaySeconds | double | 30 | Teto de atraso |
| RetryOnTimeout | bool | true | Incluir timeouts |
| RetryOnRateLimit | bool | true | Incluir 429 |
| RetryOnServerErrors | bool | true | Incluir 5xx |
| RespectRetryAfterHeader | bool | true | Honra cabeçalho 429 |
| EnableCircuitBreaker | bool | true | Habilita breaker |
| CircuitBreakerFailuresBeforeBreaking | int | 8 | Falhas antes de abrir |
| CircuitBreakerDurationSeconds | int | 30 | Janela aberto |
| EnableJitter | bool | true | Ativa jitter |
| JitterMillisecondsMax | int | 250 | Jitter máximo |
| EnableMetrics | bool | true | Emite métricas |
| OnRetryEvent | Action<RetryEvent>? | null | Callback retry |
| OnCircuitBreakerEvent | Action<CircuitBreakerEvent>? | null | Callback breaker |

### Métricas Disponíveis
| Nome | Tipo | Tags | Descrição |
|------|------|------|-----------|
| mb_client_http_retries | Counter<long> | status_code | Contagem de retries |
| mb_client_circuit_opened | Counter<long> | (sem) | Aberturas de circuito |
| mb_client_circuit_half_open | Counter<long> | (sem) | Transições half-open |
| mb_client_circuit_closed | Counter<long> | (sem) | Fechamentos pós-sucesso |
| mb_client_http_request_duration | Histogram<double> | method, outcome, status_code | Latência total (incluindo retries) |

Valores de `outcome` (histogram): success, client_error, server_error, transient_exhausted, circuit_open_fast_fail, timeout_or_canceled, canceled, exception, other, unknown.

### Heurísticas Adaptativas (Sinais → Ação)
| Sinal | Condição | Ajuste Sugerido |
|-------|----------|----------------|
| Pico de retries | retries/s > baseline*1.3 | Reduzir `MaxRetryAttempts` ou aumentar base delay |
| Muitos circuit opens | >2 em 5min | Aumentar `CircuitBreakerDurationSeconds` e diminuir fan-out |
| Latência p95 alta | p95 > 1.5x baseline | Reduzir concorrência, otimizar lotes |
| Transient exhausted alto | >10% outcomes | Aumentar backoff ou investigar upstream |

### Cancelamento
Propagar tokens para: abortar buscas de candles longas, cessar intensificação de retry e liberar recursos antecipadamente.

### Variáveis de Ambiente
| Variável | Uso |
|---------|-----|
| MB_USER_AGENT | Customização de User-Agent |
| ENABLE_PERFORMANCE_TESTS | Ativa testes de performance |
| ENABLE_TRADING_TESTS | Libera testes que mutam estado (cuidado) |

### Considerações AOT
- Manter referência ao contexto `MercadoBitcoinJsonSerializerContext`.
- Evitar reflexão dinâmica customizada adicional.
- Futuro: substituir trechos gerados que ainda emitem warnings IL.

---
## 4. Tabela de Métodos Principais (Contrato Sintético)
| Método | Público/Privado | Entrada Básica | Saída | Observações IA |
|--------|-----------------|----------------|-------|----------------|
| AuthenticateAsync(login, password) | Privado | string, string | (void) | Obtém token Bearer; chamar antes de endpoints privados. |
| GetSymbolsAsync(symbols?) | Público | string? (lista CSV) | `ListSymbolInfoResponse` | Use para validar símbolos. |
| GetTickersAsync(symbols) | Público | string CSV | List<TickerResponse> | Requer nomes exatamente `BASE-QUOTE`. |
| GetOrderBookAsync(symbol, limit?) | Público | string, string? | OrderBookResponse | `limit` string numérica (até 1000). |
| GetTradesAsync(symbol, tid?, since?, from?, to?, limit?) | Público | filtros int? | ICollection<TradeResponse> | Combine filtros com parcimônia. |
| GetCandlesAsync(symbol, resolution, to, from?, countback?) | Público | symbol, timeframe, int(s) | ListCandlesResponse | `countback` prioriza sobre `from`. |
| GetCandlesTypedAsync(...) | Público | idem | IReadOnlyList<CandleData> | Já expande em objetos amigáveis. |
| GetAccountsAsync() | Privado | - | ICollection<AccountResponse> | Primeiro passo após autenticar. |
| GetBalancesAsync(accountId) | Privado | string | ICollection<CryptoBalanceResponse> | Filtra por conta. |
| GetTradingFeesAsync(accountId, symbol) | Privado | string, string | GetMarketFeesResponse | Monitorar fee maker/taker. |
| PlaceOrderAsync(symbol, accountId, payload) | Privado | PlaceOrderRequest | PlaceOrderResponse | Preencher tipo, side, qty ou cost. |
| CancelOrderAsync(accountId, symbol, orderId, async?) | Privado | ids + bool? | CancelOrderResponse | Se async=false aguarda pooling interno. |
| ListOrdersAsync(symbol, accountId, filtros...) | Privado | strings | ICollection<OrderResponse> | Retorna ordens do par. |
| ListAllOrdersAsync(accountId, filtros...) | Privado | strings | ListAllOrdersResponse | Todos os pares. |
| CancelAllOpenOrdersByAccountAsync(accountId, has_executions?, symbol?) | Privado | string + filtros | ICollection<CancelOpenOrdersResponse> | Cuidado: ação massiva. |
| WithdrawCoinAsync(accountId, symbol, payload) | Privado | WithdrawCoinRequest | Withdraw | Verificar confiabilidade de destino. |
| ListWithdrawalsAsync(accountId, symbol, page?, page_size?, from?) | Privado | paging ints | ICollection<Withdraw> | Auditar histórico. |
| GetWithdrawLimitsAsync(accountId, symbols?) | Privado | filtros | Response (dinâmico) | Interpretar como mapa símbolo→quantidade. |

---
## 5. Notas Sobre Parâmetros Sensíveis
- `symbol` deve seguir formato `BASE-QUOTE` (maiúsculo). Há normalização, mas IA deve tentar enviar já normalizado.
- `resolution` (documentado): `1m, 15m, 1h, 3h, 1d, 1w, 1M`. O cliente aceita adicionais (`5m, 30m, 4h, 6h, 12h`), porém podem gerar 400. IA: preferir apenas resoluções documentadas a menos que haja confirmação posterior.
- `PlaceOrderRequest`: ou definir `Qty` OU `Cost` (no caso de market buy). Não enviar ambos conflitantes.
- `WithdrawCoinRequest`: Para fiat usar `Account_ref`; para cripto usar `Address`, `Tx_fee`, possivelmente `Network`.

---
## 6. Fluxos Recomendados (Playbooks para IA)
### 6.1. Consulta de Preço Agregada
1. Chamar `GetSymbolsAsync()` e armazenar conjunto.
2. Verificar se `BTC-BRL` ∈ símbolos.
3. Chamar `GetTickersAsync("BTC-BRL")`.
4. (Opcional) Chamar `GetOrderBookAsync("BTC-BRL", limit:"50")` para spread.
5. Consolidar: último preço, melhor bid/ask, volume 24h.

### 6.2. Preparar Ordem de Compra Limitada
1. Autenticar.
2. `GetAccountsAsync()` → selecionar `accountId`.
3. Validar saldo com `GetBalancesAsync(accountId)` (checar BRL disponível para `qty * preco`).
4. Construir `PlaceOrderRequest { Side="buy", Type="limit", Qty="0.001", LimitPrice=VALOR }`.
5. `PlaceOrderAsync("BTC-BRL", accountId, request)`.
6. Persistir `OrderId` para monitorar execução com `GetOrderAsync`.

### 6.3. Cancelar Todas as Ordens Abertas de um Par
1. Autenticar.
2. `ListOrdersAsync(symbol, accountId, status:"working")`.
3. Se count > threshold (ex. > 0), avaliar se ação massiva é desejada.
4. Chamar `CancelAllOpenOrdersByAccountAsync(accountId, symbol:symbol)`.
5. Confirmar cancelamento individual com `ListOrdersAsync` novamente.

### 6.4. Rolling Update de Candles Tipados
1. Definir timeframe (ex. `1h`).
2. Capturar `to = now` e `countback = 24`.
3. Chamar `GetRecentCandlesTypedAsync(symbol, timeframe, countback)`.
4. Calcular métricas (EMA, volatilidade) externamente.
5. Agendar próxima chamada a cada fechamento de candle (alinhar a boundary temporal).

### 6.5. Auditoria de Saques Recentes
1. Autenticar.
2. `ListWithdrawalsAsync(accountId, "BTC", page:1, page_size:50)`.
3. Filtrar status ≠ concluído para acompanhamento.
4. Se necessário, `GetWithdrawalAsync(accountId, "BTC", withdrawId)` para detalhar.

---
## 7. Estratégia de Tratamento de Erros (IA)
| Código (ErrorResponse.Code) | Ação Recomendada |
|-----------------------------|------------------|
| INVALID_SYMBOL | Validar via `GetSymbolsAsync` e sugerir correção. |
| INSUFFICIENT_BALANCE | Reduzir `Qty` ou abortar. |
| ORDER_NOT_FOUND | Revalidar `orderId` (pode já ter sido finalizada). |
| REQUEST_RATE_EXCEEDED / 429 | Aguardar (ex. 1–2s) + aplicar backoff exponencial. |
| API_UNAVAILABLE | Retry com atraso crescente (ex. 5s, 15s, 30s). |
| INVALID_PARAMETER | Log + revisar coerência de campos. |
| FORBIDDEN / INVALID_ACCESS | Reautenticar (token expirado ou incorreto). |
| MISSING_FIELD | Revisar payload; IA deve validar obrigatórios antes. |

Padrão de captura sugerido:
```csharp
try {
    // chamada
} catch (MercadoBitcoinApiException ex) {
    switch (ex.ErrorResponse?.Code) {
        case "REQUEST_RATE_EXCEEDED": await Task.Delay(1200); /* retry */ break;
        case "INVALID_SYMBOL": /* refresh symbols e corrigir */ break;
        default: /* log analítico e possivelmente escalar */ break;
    }
}
```

---
## 8. Prompt Patterns (Templates prontos para outra IA)
### 8.1. Obter Candles Seguros
"Dado o cliente `MercadoBitcoinClient`, recupere os últimos N candles válidos no timeframe X apenas usando resoluções documentadas. Se a resolução pedida não for suportada, faça fallback para '1h'. Em caso de erro de símbolo, recarregue símbolos e tente uma vez mais. Retorne JSON com campos: symbol, resolution, candles[]."

### 8.2. Criar Ordem com Verificação de Saldo
"Autentique se necessário; valide saldo fiat suficiente para (qty * limitPrice). Se insuficiente, reduza qty até caber. Publique ordem limit e retorne objeto: {requestedQty, placedQty, orderId, limitPrice}."

### 8.3. Cancelar Ordem com Retentativa
"Dado accountId e orderId, tente cancelar. Se status final não for 'cancelled' e não houver erro terminal, repetir consulta a cada 500ms até 5 vezes antes de devolver status final." 

### 8.4. Detecção de Divergência de Spread
"Calcule spread percentual = (bestAsk - bestBid) / bestBid. Se > limiar, produza recomendação 'ALTA_VOLATILIDADE'. Caso contrário 'NORMAL'."

### 8.5. Auditoria de Execução Parcial
"Recupere ordem. Se status = working e FilledQty > 0, compute fillRatio = FilledQty / Qty. Classifique: <25% 'LOW_FILL', 25–75% 'MID_FILL', >75% 'NEAR_COMPLETE'."

### 8.6. Monitoramento de Latência (Telemetria)
"Para cada chamada de dados públicos, logue timestamp início/fim, calcule ms. Se média de 5 últimas > baseline*1.5, emitir alerta 'LATENCY_DEGRADATION'."

### 8.7. Refresh Inteligente de Símbolos
"Atualize cache local de símbolos apenas se requisição anterior a /symbols tiver > 10 minutos ou se um INVALID_SYMBOL tiver ocorrido."

### 8.8. Fallback de Rate Limit
"Ao receber REQUEST_RATE_EXCEEDED ou status 429, aplicar delay exponencial base 750ms * 2^(tentativa-1) com jitter ±100ms. Interromper após 5 tentativas e devolver 'RATE_LIMIT_ABORT'."

### 8.9. Normalização de Entrada de Usuário
"Converter entradas de símbolo (case-insensitive) para formato padronizado 'BASE-QUOTE'; validar existência e corrigir variantes (ex. 'btcbrl' -> 'BTC-BRL')."

### 8.10. Extração de Métricas de Candle
"Para candles retornados, compute: avgVolume, highRange = max(High)-min(Low), bodyRatio = |Close-Open| / (High-Low). Enriquecer resposta." 

---
## 9. Regras Operacionais para IA
- Não emitir ordens market sem antes verificar volatilidade (usar spread + último candle). 
- Limitar simultaneamente a 1 operação de cancelamento em massa cada 60s.
- Não solicitar candles com `countback > 500` para evitar latência (ajustar chunking se necessário).
- Para reconciliação de ordens, preferir `ListOrdersAsync` com filtros de janela temporal em vez de varredura completa.
- Sempre logar (internamente) parâmetros usados em PlaceOrder para auditoria futura.

---
## 10. Considerações Específicas do Cliente
- O wrapper adiciona resoluções extras — IA deve **opt-in explícito** só após teste de uma requisição de controle (ex: tentar '5m' e aceitar apenas se não houver 400).
- Método `GetWithdrawLimitsAsync` retorna tipo gerado pobre (`Response`) — sugerido pós-processar JSON cru (se o ajuste ainda não foi implementado) ou tratar propriedades desconhecidas como mapa lógico.
- `CancelAllOpenOrdersByAccountAsync` envolve risco operacional: IA deve confirmar número de ordens > 0 antes de chamar.

---
## 11. Pseudocódigo Exemplificativo (IA Trading Seguro)
```csharp
async Task<string> SafeLimitBuyAsync(MercadoBitcoinClient c, string symbol, decimal desiredQty, decimal limitPrice){
    var accounts = await c.GetAccountsAsync();
    var accountId = accounts.First().Id;
    var balances = await c.GetBalancesAsync(accountId);
    var fiat = balances.First(b => b.Symbol == "BRL");
    var costNeeded = desiredQty * limitPrice;
    var available = decimal.Parse(fiat.Available, CultureInfo.InvariantCulture);
    if (costNeeded > available){
        desiredQty = Math.Floor((available / limitPrice) * 100000000m)/100000000m; // ajustar 8 casas
    }
    if (desiredQty <= 0) throw new InvalidOperationException("Saldo insuficiente");
    var req = new PlaceOrderRequest{ Side="buy", Type="limit", Qty=desiredQty.ToString(CultureInfo.InvariantCulture), LimitPrice=(double)limitPrice };
    var r = await c.PlaceOrderAsync(symbol, accountId, req);
    return r.OrderId;
}
```

---
## 12. Checklist de Boas Práticas para Agentes
| Item | Verificar | Status (exemplo) |
|------|-----------|------------------|
| Cache de símbolos carregado | >=1 símbolo válido | OK |
| Autenticação ativa | Token definido antes de privados | OK |
| Respeito de Rate Limit | <=1 req/s (público) | OK |
| Validação de parâmetros | symbol/timeframe revisados | OK |
| Reexecução idempotente | externalId em PlaceOrder (opcional) | PENDENTE |
| Logs internos | parâmetros críticos registrados | OK |

---
## 13. Glossário Rápido
- **Symbol**: Par de negociação `BASE-QUOTE` (ex. `BTC-BRL`).
- **Resolution**: Janela temporal do candle (ex. `1h`).
- **OrderId**: Identificador único alfanumérico retornado em criação de ordem.
- **AccountId**: Id da conta obtida em `GetAccountsAsync`.
- **Working**: Ordem ativa não totalmente executada.
- **Filled**: Ordem completamente executada.

---
## 14. Estratégia de Evolução (Sugestões à IA)
1. Wrapper tipado para limites de saque.
2. Tuner automático de retry baseado em métricas (closed loop).
3. Eliminar warnings AOT substituindo chamadas genéricas de serialização.
4. Estratégia ativa de rate limiting (token bucket configurável).
5. Batching inteligente para múltiplos símbolos.
6. Streaming/WebSocket (reduzir polling).
7. Detecção de anomalia em padrões de outcome/latência.
8. Escalonamento de tamanho de ordem por volatilidade EMA.

---
## 15. Saída de Referência (Formato JSON Recomendado)
Exemplo consolidado de resposta que a IA pode produzir ao compor múltiplos dados:
```json
{
  "symbol": "BTC-BRL",
  "ticker": { "last": 275000.15, "high": 278500.00, "low": 270100.00, "volume": 84.12 },
  "orderbook": { "bestBid": 274900.00, "bestAsk": 275100.00, "spreadPct": 0.00073 },
  "recentCandles": {
    "resolution": "1h",
    "count": 24,
    "avgVolume": 1.72,
    "volatility": 0.0312
  },
  "riskFlags": ["SPREAD_NORMAL"],
  "timestampUtc": "2025-08-27T12:34:56Z"
}
```

---
## 16. Conclusão
Este guia provê contexto operacional, padrões de prompts, contratos de métodos e heurísticas de segurança para que uma outra IA possa integrar-se eficientemente ao ecossistema MercadoBitcoin via esta biblioteca. Expandir ou adaptar conforme surgirem novos endpoints ou mudanças de política.

> FIM – Documento autocontido (v2.1.0: resiliência + observabilidade + heurísticas adaptativas) para consumo automatizado por agentes inteligentes.
