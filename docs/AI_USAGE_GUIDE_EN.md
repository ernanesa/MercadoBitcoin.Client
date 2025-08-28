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

---
## 2. Logical Structure
```
MercadoBitcoinClient
 ├── Authentication: AuthenticateAsync
 ├── Public Data: GetSymbols, GetTickers, GetOrderBook, GetTrades, GetCandles*, GetAssetFees, GetAssetNetworks
 ├── Accounts: GetAccounts, GetBalances, GetTier, GetTradingFees, GetPositions
 ├── Trading: PlaceOrder, ListOrders, GetOrder, CancelOrder, ListAllOrders, CancelAllOpenOrders
 ├── Wallet: ListDeposits, GetDepositAddresses, ListFiatDeposits, WithdrawCoin, ListWithdrawals,
 │           GetWithdrawal, GetWithdrawLimits, GetBrlWithdrawConfig,
 │           GetWithdrawCryptoWalletAddresses, GetWithdrawBankAccounts
 └── Candle Helpers: GetCandlesTypedAsync / GetRecentCandlesTypedAsync
```

---
## 3. AI Usage Principles
1. Determine if target operation is public vs private before calling.
2. Authenticate once before any private operations; reuse client.
3. Validate critical parameters (symbol format `BASE-QUOTE`; candle resolution) before invocation.
4. Maintain a local cache of `/symbols` for validation and normalization.
5. Rely on built-in window swap (from > to) in candle methods—do not duplicate.
6. For trading, optionally store `ExternalId` for logical idempotency.
7. Catch `MercadoBitcoinApiException` and branch by `ErrorResponse.Code` for recovery decisions.
8. Respect soft rate limit pacing (≥1s between public bursts if uncertain).

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
1. Add strong dictionary wrapper for withdraw limits.
2. Parse `Retry-After` for improved 429 handling.
3. Expose latency metrics for adaptive throttling.
4. Validate extra resolutions by discovery probe.

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

> END – Self-contained document for intelligent agent consumption.
