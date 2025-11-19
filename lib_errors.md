# Error Report: Candles Collection (MercadoBitcoin.Client)

## Context and Symptoms
- POST /saveCandles/BTC-BRL/15m returns: "No candle returned by the API for BTC-BRL 15m".
- GET /api/candles/BTC-BRL/15m returns an empty list.
- Other functionalities (tickers, trades, orderbook, symbols) are operating normally.

## Technical Analysis of Current Code

### Dependency Injection and Flow
- IMercadoBitcoinDadosClient is implemented by MercadoBitcoinLibClient.
- MercadoBitcoinLibClient is registered as Scoped; MercadoBitcoinClient as Singleton.
- CandleService consumes IMercadoBitcoinDadosClient to fetch and persist candles.

### Current Implementation of GetCandlesAsync
- MercadoBitcoinLibClient.GetCandlesAsync uses reflection to attempt to invoke methods of the underlying client (_mercadoBitcoinClient), looking for names like "GetCandlesAsync", "history" or "OHLC".
- Arguments are constructed from: symbol, resolution, from, to and countback. There is a retry policy with exponential backoff and jitter, and debug logs.
- CandleService processes the response using reflection to extract properties from each candle before saving.

### Points of Possible Incompatibility
1. Resolution parameter:
   - The code uses a string parameter called resolution (e.g.: "15m").
   - In the public API v4 for candles, the standard nomenclature is a "type" parameter (e.g.: "1m", "15m", "1h", "1d").
   - If the library expects "resolution" or a different type and the actual method requires "type", reflection will not find a compatible method or will construct the call incorrectly, resulting in an empty return.
2. Symbol formatting:
   - The system uses symbols in the format "BTC-BRL".
   - API v4 adopts pairs in lowercase without hyphens (e.g.: "btcbrl").
   - If the library does not normalize the symbol, it may query a non-existent/unindexed endpoint, returning empty.
3. Temporal parameters and limit:
   - The flow uses from/to (epoch) and countback/limit.
   - Divergences in the combination or expected names (from, to, limit) may generate empty responses.
4. Error handling:
   - The implementation returns an empty list when reflection fails or when it does not find a method, masking the real error (should throw a specific exception and log in detail).

## Probable Cause (Focus Hypothesis)
- Incompatibility between the signature expected by the reflection layer of MercadoBitcoinLibClient and the actual method of the library/candles endpoint (differences in name and/or types of parameters: "resolution" vs "type", unnormalized symbol, from/to/limit schema). This causes the reflection call not to be resolved correctly and the fallback returns empty.

## Recomendações de Correção (na Biblioteca MercadoBitcoin.Client)
1. Expor método fortemente tipado para candles (evitar reflexão):
   - Assinatura sugerida: GetCandlesAsync(string symbol, string type, long? from = null, long? to = null, int? limit = null, CancellationToken ct = default)
   - Responsabilidades internas: normalizar símbolo ("BTC-BRL" -> "btcbrl"), mapear resoluções, montar URL/rota/params, chamar HTTP, desserializar DTOs.
2. Normalização de inputs:
   - Símbolo: remover hífens, converter para minúsculas.
   - Resolução: mapear resoluções do sistema para os tipos aceitos: 1m, 5m, 15m, 30m, 1h, 4h, 1d (e outros suportados, se houver).
3. Parâmetros temporais:
   - Garantir uso de epoch em segundos (ou milissegundos, conforme documentação) e coerência entre from/to/limit.
   - Se from não for fornecido, permitir uso de limit para pegar candles mais recentes.
4. DTOs e mapeamento:
   - Definir modelos explícitos para o retorno de candles (timestamp/open/high/low/close/volume/quote_volume, etc.).
   - Evitar reflexão na leitura de propriedades no consumidor; retornar objetos fortemente tipados.
## Correction Recommendations (in MercadoBitcoin.Client Library)
1. Expose strongly typed method for candles (avoid reflection):
   - Suggested signature: GetCandlesAsync(string symbol, string type, long? from = null, long? to = null, int? limit = null, CancellationToken ct = default)
   - Internal responsibilities: normalize symbol ("BTC-BRL" -> "btcbrl"), map resolutions, build URL/route/params, call HTTP, deserialize DTOs.
2. Input normalization:
   - Symbol: remove hyphens, convert to lowercase.
   - Resolution: map system resolutions to accepted types: 1m, 5m, 15m, 30m, 1h, 4h, 1d (and others supported, if any).
3. Temporal parameters:
   - Ensure use of epoch in seconds (or milliseconds, according to documentation) and coherence between from/to/limit.
   - If from is not provided, allow use of limit to get most recent candles.
4. DTOs and mapping:
   - Define explicit models for candle return (timestamp/open/high/low/close/volume/quote_volume, etc.).
   - Avoid reflection when reading properties in consumer; return strongly typed objects.
5. Error and logging policy:
   - In compatibility/serialization failures, throw exceptions with clear message (e.g.: CandleEndpointNotFoundException, CandleSchemaMismatchException).
   - Keep retry with backoff, but never return empty list silently after structural error.
   - Log final URL, parameters, status code, error body (sanitized).

## Suggested Adjustments in Consumer (Data)
- Replace use of reflection in CandleService with consumption of strong DTOs from the library.
- Internally map route resolution ("15m") to library "type" ("15m").
- Standardize symbol received via API ("BTC-BRL") converting to format accepted by the library.

## Testing Plan
1. Unit tests (with library mocks):
   - Given symbol "BTC-BRL" and resolution "15m", when library returns 3 candles, then CandleService persists 3 candles and GET /api/candles returns 3 entries.
   - Given empty return from library, then service throws error or returns appropriate message informing absence of data (not to be confused with integration failure).
2. Integration (against public API, if possible):
   - Call method GetCandlesAsync("btcbrl", "15m", limit: 5) and validate that payload has coherent OHLCV.
3. End-to-end:
   - POST /saveCandles/BTC-BRL/15m, then GET /api/candles/BTC-BRL/15m?limit=3 should return recent data.

## Items to Validate with Library
- Is there currently a public method for candles? What is the exact signature?
- Does the method use "type" parameter (e.g.: "15m")? Does it accept from/to/limit? What granularity is supported?
- Does the method normalize the symbol (e.g.: "BTC-BRL" -> "btcbrl") internally?
- Structure of returned DTO (names and types of properties) and examples of actual payload.

## Decision Made So Far
- The AddHttpClient() change was reverted in the Data project, as requested.
- The correction should occur primarily in the MercadoBitcoin.Client library, exposing a strong candles method, aligned with API v4, and with input normalization and well-defined DTOs.

## Proposed Next Steps
1. In MercadoBitcoin.Client library:
   - Implement/adjust strong GetCandlesAsync method as recommended.
   - Add unit and integration tests covering success and failure cases.
2. In Data project:
   - Adapt MercadoBitcoinLibClient to use the strong method (without reflection) and remove dynamic discovery logic.
   - Update CandleService to consume strong DTOs, simplifying the persistence pipeline.
3. Validate end-to-end with BTC-BRL 15m and other resolutions (1h, 1d) to ensure robustness.