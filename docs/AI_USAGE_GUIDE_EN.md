# AI-Oriented Usage Guide (English)

This document is self-contained and crafted so that **autonomous AI agents / LLM tools** can safely and efficiently operate the `MercadoBitcoin.Client` .NET library. It includes method maps, reasoning patterns, prompt templates, error handling strategies, decision tables, and output contracts.

> Primary Goal: Enable an AI agent to plan and execute public data retrieval, trading, account management and wallet operations through this client with minimal ambiguity.

---
## 1. Quick Overview
- Root namespace: `MercadoBitcoin.Client`
- Main client class: `MercadoBitcoinClient`
- Protocol: HTTPS over HTTP/2 (default)
- Serialization: `System.Text.Json` (Source Generators), context `MercadoBitcoinJsonSerializerContext`
- Auth: `AuthenticateAsync(login, password)` issues Bearer token
- Public endpoints require no token; private endpoints require Bearer
- Resilience: Retry via `RetryHandler` + Polly controlled by `RetryPolicyConfig`
 - Circuit Breaker: Manual lightweight breaker (consecutive failure threshold + half-open probe)
 - Jitter: Enabled by default to de-sync concurrent clients
 - Metrics: Native `System.Diagnostics.Metrics` counters + latency histogram (opt-out via `EnableMetrics`)
 - Cancellation: Every public and private endpoint exposes `CancellationToken`
 - User-Agent Override: Env var `MB_USER_AGENT` (observability / traffic segregation)
 - Test Suite: 64 scenarios (public, private, serialization, performance, resilience)
 - Version: 2.1.0 (Resilience & Observability expansion)

---
## 2. Logical Structure
```
 │           GetWithdrawCryptoWalletAddresses, GetWithdrawBankAccounts
 └── Candle Helpers: GetCandlesTypedAsync / GetRecentCandlesTypedAsync
```

---
## 3. AI Usage Principles
1. Determine if target operation is public vs private before calling.
2. Authenticate once before any private operations; reuse client.
4. Maintain a local cache of `/symbols` for validation and normalization.
5. Rely on built-in window swap (from > to) in candle methods—do not duplicate.
6. For trading, optionally store `ExternalId` for logical idempotency.
7. Catch `MercadoBitcoinApiException` and branch by `ErrorResponse.Code` for recovery decisions.
8. Respect soft rate limit pacing (≥1s between public bursts if uncertain).
9. Prefer graceful cancellation (propagate upstream token) instead of force abort if adjusting strategy mid-flight.
10. Observe metrics (retry rate spikes, circuit transitions, latency p95 growth) to adapt behavior (dynamic backoff tuning).

---
## 3.1 Resilience Components (Deep Dive)
| Component | Purpose | Key Tunables | AI Considerations |
|-----------|---------|--------------|-------------------|
| Retry (Polly WaitAndRetryAsync) | Recover transient/network/server faults | `MaxRetryAttempts`, `BaseDelaySeconds`, `BackoffMultiplier`, `MaxDelaySeconds`, `EnableJitter`, `JitterMillisecondsMax`, flags for `RetryOnTimeout` / `RetryOnRateLimit` / `RetryOnServerErrors` | Back off earlier if latency baseline worsens; disable server-error retry if performing idempotent analysis only. |
| Manual Circuit Breaker | Fast-fail after N consecutive retry-eligible failures | `EnableCircuitBreaker`, `CircuitBreakerFailuresBeforeBreaking`, `CircuitBreakerDurationSeconds` | When breaker opens, treat system as degraded; reduce request frequency; schedule health probe after open duration. |
| Retry-After Honor | Align with server throttling | `RespectRetryAfterHeader` | If header delay > computed backoff, override to avoid double delay. |
| Metrics | Feedback loop & observability | `EnableMetrics` | Use histogram outcome distribution to decide tuning. |

### RetryPolicyConfig Field Reference
| Property | Type | Default | Description | AI Adaptive Guidance |
|----------|------|---------|-------------|----------------------|
| MaxRetryAttempts | int | 3 | Attempts after initial call (total tries = attempts) | Increase to 5 only if success probability improves and latency SLO not violated |
| BaseDelaySeconds | double | 1.0 | First backoff slice | Lower (0.3–0.5) for ultra low-lat public price fetch bursts |
| BackoffMultiplier | double | 2.0 | Exponential growth factor | Consider 1.5 if traffic constant & jitter adequate |
| MaxDelaySeconds | double | 30 | Ceiling per attempt | Cap ≤ 10 for latency sensitive UX flows |
| RetryOnTimeout | bool | true | Includes 408/TaskCanceled (timeout) | Disable if upstream already aggressively times out |
| RetryOnRateLimit | bool | true | Retries 429 with optional Retry-After | Keep true; rely on header respect |
| RetryOnServerErrors | bool | true | Retries 5xx (recoverable) | For non-idempotent operations ensure server semantics safe |
| RespectRetryAfterHeader | bool | true | Honor server pacing hint | ALWAYS for fairness |
| EnableCircuitBreaker | bool | true | Enables consecutive-failure breaker | Disable only in test harness fuzzing |
| CircuitBreakerFailuresBeforeBreaking | int | 8 | Threshold to open | Lower (5) under severe systemic outage detection |
| CircuitBreakerDurationSeconds | int | 30 | Open window before half-open probe | Extend (60) if repeated flapping observed |
| EnableJitter | bool | true | Randomize delays (0..JitterMax) | Keep true unless deterministic benchmarking |
| JitterMillisecondsMax | int | 250 | Max jitter added | Increase (500+) in high concurrency clusters |
| EnableMetrics | bool | true | Expose instruments | Essential for autonomous tuning |
| OnCircuitBreakerEvent | Action<CircuitBreakerEvent>? | null | Breaker state change hook | Trigger adaptive throttling tasks |
| Closed | Normal ops | Count failures; success resets | Normal pacing |
| HalfOpen | Open duration elapsed & single probe in-flight | First success closes, failure re-opens | Send ONE probe; gate parallel calls |

### Metrics Instruments
Meter Name: `MercadoBitcoin.Client` VersionTag: `2.1.0`

| Name | Type | Unit | Tags | Description |
|------|------|------|------|-------------|
| mb_client_http_retries | Counter<long> | retries | status_code | Increment per retry attempt executed |
| mb_client_circuit_opened | Counter<long> | - | - | Circuit opened transitions |
| mb_client_circuit_half_open | Counter<long> | - | - | Half-open probes initiated |
| mb_client_circuit_closed | Counter<long> | - | - | Circuit closed after success |
| mb_client_http_request_duration | Histogram<double> | ms | method, outcome, status_code | End-to-end duration including retries |

Outcome Classification (histogram tag `outcome`): success, client_error, server_error, transient_exhausted, circuit_open_fast_fail, timeout_or_canceled, canceled, exception, other, unknown.

Latency Budgeting: Monitor p95 & p99; if p95 > (baseline * 1.5) adjust `BackoffMultiplier` downward or reduce `MaxRetryAttempts`.
### Environment Variables
| Variable | Purpose | Example |
|----------|---------|---------|
| MB_USER_AGENT | Override User-Agent value (suffix or replace) | `MyBot/1.0 (+team)` |
| MB_API_KEY / MB_API_SECRET (if used) | Credential provisioning (if alternative auth flows) | (secret) |
| ENABLE_PERFORMANCE_TESTS | Enable perf tests in test suite | true |
| ENABLE_TRADING_TESTS | Allow trading mutations | false |

### Cancellation Strategy
- Always pass a single root `CancellationToken` through stacked operations.
- For time-bounded workflows, compose: `using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));`
- Avoid arbitrary `Thread.Abort` equivalents—rely on cooperative cancellation to keep metrics consistent.

### Adaptive Heuristics (AI Control Loop)
| Signal | Condition | Adjustment |
|--------|-----------|------------|
| Retry Rate Spike | retries/sec > historical p95 * 1.3 | Lower MaxRetryAttempts or widen base delay |
| Circuit Opens | > 2 opens in 5 min | Enter degraded mode: widen delays, reduce request fan-out |
| Latency Degradation | p95 > 1.5 × baseline & outcome=success stable | Reduce concurrency or enable symbol batching |
| Transient Exhausted Rise | outcome=transient_exhausted proportion > 10% | Investigate upstream; temporarily raise delay/backoff |
| Timeout Or Canceled > Normal | >10% of histogram samples | Shorten client timeout or detect network saturation |

### AOT Considerations for Agents
- JSON Source Generation reduces reflection, but generated REST client still emits some dynamic patterns; prefer using provided DTOs only.
- If trimming, keep reference to `MercadoBitcoinJsonSerializerContext` alive (static field or direct usage) so linker does not remove metadata.

### Safe Trading Guidelines (Expanded)
| Check | Rationale | Enforcement |
|-------|-----------|------------|
| Spread Threshold | Avoid unfavorable fills | Abort or adjust limit price |
| Recent Volatility | Detect spike regime | Shrink order size |
| Balance Sufficiency | Prevent rejects | Pre-calc cost vs available |
| Retry Bound | Avoid cascaded duplication | Ensure idempotent `ExternalId` where applicable |
| Circuit State | System instability | Pause non-essential trading when open |
### Telemetry-Driven Dynamic Config Pseudocode
```csharp
void Adapt(RetryPolicyConfig cfg, MetricsSnapshot snap)
{
  if (snap.RetryRatePerSecond > snap.BaselineRetryRate * 1.3)
    cfg.BackoffMultiplier = Math.Min(3.0, cfg.BackoffMultiplier + 0.2);
  if (snap.CircuitOpensLast5Min > 2)
    cfg.CircuitBreakerDurationSeconds = Math.Min(120, cfg.CircuitBreakerDurationSeconds + 15);
  if (snap.P95LatencyMs > snap.BaselineP95LatencyMs * 1.5)
    cfg.MaxRetryAttempts = Math.Max(2, cfg.MaxRetryAttempts - 1);
}
```

---

---
## 4. Method Contract Table
| Method | Scope | Key Inputs | Output | AI Note |
|--------|-------|-----------|--------|---------|
| AuthenticateAsync(login, password) | Private | string, string | void | Issues Bearer token. |
| GetSymbolsAsync(symbols?) | Public | optional CSV | `ListSymbolInfoResponse` | Cache for validation. |
| GetTickersAsync(symbols) | Public | CSV | List<TickerResponse> | Requires correct `BASE-QUOTE`. |
| GetOrderBookAsync(symbol, limit?) | Public | string, string? | OrderBookResponse | Limit ≤ 1000 (string). |
| GetTradesAsync(symbol, tid?, since?, from?, to?, limit?) | Public | filters | ICollection<TradeResponse> | Combine filters sparingly. |
| GetCandlesAsync(symbol, resolution, to, from?, countback?) | Public | timing ints | ListCandlesResponse | `countback` overrides `from`. |
| GetCandlesTypedAsync(...) | Public | same | IReadOnlyList<CandleData> | Normalized typed result. |
| GetAccountsAsync() | Private | - | ICollection<AccountResponse> | First after auth. |
| GetBalancesAsync(accountId) | Private | string | ICollection<CryptoBalanceResponse> | Audit funds. |
| GetTradingFeesAsync(accountId, symbol) | Private | string,string | GetMarketFeesResponse | Maker/Taker fees. |
| PlaceOrderAsync(symbol, accountId, payload) | Private | PlaceOrderRequest | PlaceOrderResponse | Provide qty OR cost. |
| CancelOrderAsync(accountId, symbol, orderId, async?) | Private | ids + bool? | CancelOrderResponse | async=false waits internal poll. |
| ListOrdersAsync(symbol, accountId, filters...) | Private | strings | ICollection<OrderResponse> | Symbol-specific. |
| ListAllOrdersAsync(accountId, filters...) | Private | strings | ListAllOrdersResponse | All symbols. |
| CancelAllOpenOrdersByAccountAsync(accountId, hasExec?, symbol?) | Private | string+filters | ICollection<CancelOpenOrdersResponse> | Mass action. |
| WithdrawCoinAsync(accountId, symbol, payload) | Private | WithdrawCoinRequest | Withdraw | Address vs bank selection. |
| ListWithdrawalsAsync(accountId, symbol, page?, size?, from?) | Private | paging ints | ICollection<Withdraw> | Historical review. |
| GetWithdrawLimitsAsync(accountId, symbols?) | Private | filters | Response (dynamic) | Treat as symbol→quantity map. |

---
## 5. Parameter Notes
- `symbol`: Must be `BASE-QUOTE` uppercase; library attempts normalization but AI should supply normalized value.
- `resolution` (document spec): `1m, 15m, 1h, 3h, 1d, 1w, 1M`. Library accepts additional values (`5m, 30m, 4h, 6h, 12h`) which may 400—prefer documented first.
- `PlaceOrderRequest`: Provide `Qty` OR (for market buy) `Cost` (quote currency). Avoid conflicting dual specification.
- `WithdrawCoinRequest`: For fiat use `Account_ref`; for crypto use `Address`, `Tx_fee`, maybe `Network`.

---
## 6. Recommended Flows (Playbooks)
### 6.1 Aggregated Price Snapshot
1. Cache symbols. 2. Validate target symbol. 3. Get tickers. 4. Get order book. 5. Compute spread & metrics.

### 6.2 Prepare Limit Buy Order
1. Authenticate. 2. GetAccounts → accountId. 3. Balances → ensure BRL coverage. 4. Build PlaceOrderRequest. 5. PlaceOrderAsync. 6. Persist OrderId.

### 6.3 Mass Cancel for One Symbol
1. Authenticate. 2. ListOrders (status=working). 3. Confirm >0. 4. CancelAllOpenOrders. 5. Re-list.

### 6.4 Rolling Candle Update
Use `GetRecentCandlesTypedAsync(symbol, timeframe, countback)` every timeframe boundary.

### 6.5 Withdraw Audit
List withdrawals, refine by page/time; detail specific with GetWithdrawal.

---
## 7. Error Handling Strategy (Decision Table)
| Code | Action |
|------|--------|
| INVALID_SYMBOL | Refresh symbols + retry once. |
| INSUFFICIENT_BALANCE | Reduce qty or abort. |
| ORDER_NOT_FOUND | Re-check order list (may be completed). |
| REQUEST_RATE_EXCEEDED / 429 | Backoff (exp + jitter) then retry. |
| API_UNAVAILABLE | Progressive backoff (5s, 15s, 30s). |
| INVALID_PARAMETER | Sanitize & correct input. |
| FORBIDDEN / INVALID_ACCESS | Re-authenticate. |
| MISSING_FIELD | Rebuild payload with required fields. |

Catch pattern:
```csharp
try { /* call */ }
catch (MercadoBitcoinApiException ex) {
  switch (ex.ErrorResponse?.Code) {
    case "REQUEST_RATE_EXCEEDED": await Task.Delay(1200); /* retry */ break;
    case "INVALID_SYMBOL": /* refresh symbol set */ break;
    default: /* structured log */ break;
  }
}
```

---
## 8. Prompt Templates
### 8.1 Safe Candles Retrieval
"Fetch last N candles for symbol S at timeframe T (fallback to 1h if unsupported). Validate symbol first. Return JSON {symbol,resolution,candles[]} with numeric fields."

### 8.2 Balance-Aware Limit Order
"Authenticate if needed; ensure available BRL >= qty*price else downscale qty. Submit limit buy; respond with {requestedQty, placedQty, orderId}."

### 8.3 Resilient Cancel
"Attempt cancel; poll order until status terminal or max 5 polls spaced 500ms. Return final status." 

### 8.4 Spread Classification
"Compute (ask-bid)/bid. If > threshold label 'WIDE_SPREAD', else 'NORMAL'."

### 8.5 Partial Fill Analysis
"For order retrieve fill ratio=FilledQty/Qty. Classify LOW(<25%), MID(25-75%), HIGH(>75%)."

### 8.6 Latency Monitor
"Record round-trip times; if moving average of last 5 > baseline*1.5 emit 'LATENCY_DEGRADATION'."

### 8.7 Symbol Cache Refresh
"Refresh symbols only if last fetch > 10 minutes or INVALID_SYMBOL encountered."

### 8.8 Rate Limit Backoff
"On 429 apply exponential delay base 750ms *2^(n-1) ±100ms jitter; abort after 5 attempts with 'RATE_LIMIT_ABORT'."

### 8.9 Symbol Normalization
"Convert free-form symbol (e.g. btcbrl) to BTC-BRL; validate exists before proceeding." 

### 8.10 Candle Metrics
"Augment candles with avgVolume, volatility range (maxH-minL), body ratio |C-O|/(H-L)."

---
## 9. Operational Guardrails
- Do not place market orders without evaluating spread & volatility.
- Limit mass cancel to once per 60 seconds.
- Avoid `countback > 500` for candles (chunk if needed).
- Prefer filtered time windows when scanning historical orders.
- Always log structured payloads for trading actions.

---
## 10. Special Client Notes
- Extra candle resolutions accepted—but treat as experimental: probe once, then cache success flag.
- `GetWithdrawLimitsAsync` returns weak model (`Response`); parse raw JSON if enhanced method not available.
- `CancelAllOpenOrdersByAccountAsync` = high-risk: enumerate active first.

---
## 11. Example Pseudocode (Safe Limit Buy)
```csharp
async Task<string> SafeLimitBuyAsync(MercadoBitcoinClient c, string symbol, decimal qty, decimal limitPrice){
    var acct = (await c.GetAccountsAsync()).First();
    var bals = await c.GetBalancesAsync(acct.Id);
    var fiat = bals.First(b => b.Symbol == "BRL");
    var avail = decimal.Parse(fiat.Available, CultureInfo.InvariantCulture);
    var needed = qty * limitPrice;
    if (needed > avail) {
        qty = Math.Floor((avail / limitPrice) * 100000000m)/100000000m;
    }
    if (qty <= 0) throw new InvalidOperationException("Insufficient balance");
    var req = new PlaceOrderRequest { Side="buy", Type="limit", Qty=qty.ToString(CultureInfo.InvariantCulture), LimitPrice=(double)limitPrice };
    var r = await c.PlaceOrderAsync(symbol, acct.Id, req);
    return r.OrderId;
}
```

---
## 12. Best Practice Checklist
| Item | Goal | Example Status |
|------|------|----------------|
| Symbol cache loaded | >0 symbols | OK |
| Auth before private | Token set | OK |
| Rate pacing | <= guideline | OK |
| Param validation | symbol/resolution | OK |
| Idempotency (externalId) | Avoid duplicates | PENDING |
| Logging | Structured order payload | OK |

---
## 13. Glossary
- **Symbol**: Trading pair `BASE-QUOTE`.
- **Resolution**: Candle timeframe.
- **OrderId**: Unique alphanumeric order identifier.
- **AccountId**: Identifier from GetAccounts.
- **Working**: Order open/partially filled.
- **Filled**: Fully executed order.

---
## 14. Evolution Suggestions
1. Strongly typed withdraw limits wrapper (Dictionary adapter or generated model).
2. Automated adaptive retry tuner (close loop using metrics histogram + counters).
3. Replace dynamic JSON calls in generated client with explicit SourceGen context to eliminate AOT warnings.
4. Pluggable rate-limit strategy (token bucket / leaky bucket abstraction) on top of current passive respect.
5. Smart batching for high-frequency symbol queries.
6. Optional WebSocket streaming (future) to reduce polling load.
7. ML-based anomaly detection on latency & outcome classification trends.
8. Exponential moving average volatility gating for order size scaling.

---
## 15. Reference Output Schema
```json
{
  "symbol": "BTC-BRL",
  "ticker": { "last": 275000.15, "high": 278500.00, "low": 270100.00, "volume": 84.12 },
  "orderbook": { "bestBid": 274900.00, "bestAsk": 275100.00, "spreadPct": 0.00073 },
  "recentCandles": { "resolution": "1h", "count": 24, "avgVolume": 1.72, "volatility": 0.0312 },
  "riskFlags": ["SPREAD_NORMAL"],
  "timestampUtc": "2025-08-27T12:34:56Z"
}
```

---
## 16. Conclusion
This guide supplies operational context, prompt patterns, method contracts, and safety heuristics enabling an autonomous AI agent to leverage the MercadoBitcoin API via this library. Extend or refine as new endpoints or policies emerge.

> END – Self-contained document for intelligent agent consumption (v2.1.0 augmented: resilience + observability + adaptive heuristics).
