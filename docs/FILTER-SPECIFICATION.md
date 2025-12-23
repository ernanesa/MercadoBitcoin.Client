# Universal Filter Specification

## Overview
The library implements a universal filtering and batching mechanism to handle multiple assets efficiently across all supported endpoints.

## 1. Filter Logic

### Case: No Parameters
- **Behavior**: Return all available items.
- **Implementation**: 
  1. Fetch the complete list of symbols via `/symbols`.
  2. Filter for active/tradable symbols.
  3. Execute batch requests for the target data.

### Case: Specific Asset List
- **Behavior**: Filter for the requested assets.
- **Implementation**:
  1. Normalize input (Trim, ToUpper, Distinct).
  2. Validate against a known list of symbols (cached).
  3. Execute batch requests.

---

## 2. Batching Mechanisms

### Native Batching (Chunking)
Used for endpoints that support comma-separated symbols (e.g., `/tickers`, `/symbols`, `/positions`).
- **Chunk Size**: 50-100 symbols per URL to avoid length limits.
- **Parallelism**: Execute chunks in parallel using `Task.WhenAll`.

### Client-Side Fan-Out
Used for endpoints that only support one symbol at a time (e.g., `/orderbook`, `/trades`, `/candles`).
- **Parallelism**: Use `Parallel.ForEachAsync` with `MaxDegreeOfParallelism`.
- **Rate Limit Awareness**: Respects the global `TokenBucketRateLimiter`.

---

## 3. Validation and Error Handling

### Parameter Validation
- **Symbol Format**: Must match `BASE-QUOTE` (e.g., `BTC-BRL`).
- **Resolution**: Validated against a fixed set (1m, 1h, 1d, etc.).
- **Timestamps**: `to` must be greater than `from`.

### Error Handling
- **Invalid Symbol**: If a symbol is invalid, the API returns `INVALID_SYMBOL`. The library maps this to `MercadoBitcoinApiException`.
- **Partial Success**: In batch requests, if one chunk fails, the library can either fail fast or aggregate successful results and report errors (configurable).
- **Negative Caching**: Invalid symbols are cached for 1 hour to prevent redundant failed API calls.

---

## 4. Implementation Example (Universal Batcher)

```csharp
public async Task<IEnumerable<TResult>> GetUniversalDataAsync<TResult>(
    IEnumerable<string>? symbols,
    Func<string, CancellationToken, Task<IEnumerable<TResult>>> apiCall,
    CancellationToken ct)
{
    // 1. Handle "No Parameters"
    if (symbols == null || !symbols.Any())
    {
        symbols = await GetAllActiveSymbolsAsync(ct);
    }

    // 2. Normalize and Validate
    var validSymbols = symbols.NormalizeAndValidate();

    // 3. Execute Batching
    return await BatchHelper.ExecuteNativeBatchAsync(
        validSymbols, 
        batchSize: 50, 
        apiCall, 
        ct);
}
```

## 5. Recommendations for Resource Reduction
1. **Use `IAsyncEnumerable`**: For large datasets (Trades, Deposits), stream the data instead of buffering in a `List<T>`.
2. **Enable L1 Cache**: Set a TTL of at least 1 second for Tickers to deduplicate high-frequency UI updates.
3. **Prefer `GetTickersAsync` over `GetOrderBookAsync`**: Tickers are much smaller and cheaper to process if you only need the last price.
