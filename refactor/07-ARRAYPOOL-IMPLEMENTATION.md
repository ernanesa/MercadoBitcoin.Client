# 07. ARRAYPOOL IMPLEMENTATION (DETAILED)

## 📋 Index

1. [Specific Objective](#specific-objective)  
2. [Scenarios Where ArrayPool Will Be Used](#scenarios-where-arraypool-will-be-used)  
3. [Design Requirements](#design-requirements)  
4. [ArrayPoolManager API](#arraypoolmanager-api)  
5. [HTTP Usage Patterns](#http-usage-patterns)  
6. [Integration with System.Text.Json](#integration-with-systemtextjson)  
7. [Integration with Retry/Logging](#integration-with-retrylogging)  
8. [ArrayPool‑Specific Tests](#arraypool-specific-tests)  
9. [Common Errors and How to Avoid Them](#common-errors-and-how-to-avoid-them)  
10. [Implementation Checklist](#implementation-checklist)

---

## 1. Specific Objective

This document details the **practical implementation** of `ArrayPool<T>` in the library, reinforcing what was defined in `06-MEMORY-POOLING.md`, focusing on:

- Standardizing **a single way** to allocate `byte[]` buffers for I/O  
- Minimizing misuse (e.g., not returning arrays or using them after returning to the pool)  
- Integrating with the following layers:
  - HTTP (response reading)  
  - JSON serialization (System.Text.Json)

---

## 2. Scenarios Where ArrayPool Will Be Used

### 2.1. Reading HTTP Responses

- `AuthHttpClient` and generated classes (via `GeneratedClient`) must:
  - Read response content using `Stream.ReadAsync` into buffers provided by `ArrayPoolManager`  
  - Avoid `ReadAsStringAsync` in hot paths

### 2.2. JSON Serialization/Deserialization

- In scenarios where we work with `ReadOnlySpan<byte>` and `JsonSerializer.Deserialize`:
  - Use pooled buffers to store the received payload

### 2.3. Internal Data Transformation Operations

- Example: converting raw responses into intermediate structures, binary logs, etc.

---

## 3. Design Requirements

1. **Centralization**: only `ArrayPoolManager` accesses `ArrayPool<byte>.Shared` directly  
2. **Safety of Use**:
   - Buffers must not escape the method scope  
   - Storing references to pooled buffers in instance fields is forbidden  
3. **Efficiency**:
   - Avoid unnecessary copies, but **never** at the expense of safety  
4. **Observability** (future):
   - Ability to add counters for how many arrays are rented/returned

---

## 4. ArrayPoolManager API

### 4.1. Signatures

```csharp
namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class ArrayPoolManager
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

    public static byte[] RentBytes(int minimumLength)
        => BytePool.Rent(minimumLength);

    public static void ReturnBytes(byte[] array, bool clearArray = true)
    {
        if (array is null)
        {
            return;
        }

        BytePool.Return(array, clearArray);
    }
}
```

### 4.2. Guidelines

- `minimumLength` must always be calculated based on:
  - `Content-Length` (when available)  
  - Default size (4 KB, 8 KB, etc.) when content length is unknown  
- Use `clearArray = true` for sensitive data (e.g., headers, tokens), and optionally `false` for public data when we want maximum performance

---

## 5. HTTP Usage Patterns

### 5.1. Reading the Entire Response

When the content is not extremely large and we want to load it fully into memory:

```csharp
private static async Task<byte[]> ReadResponseToBufferAsync(HttpContent content, CancellationToken cancellationToken)
{
    long? contentLength = content.Headers.ContentLength;
    int initialSize = contentLength.HasValue && contentLength.Value > 0
        ? (int)Math.Min(contentLength.Value, int.MaxValue)
        : 4096;

    byte[] buffer = ArrayPoolManager.RentBytes(initialSize);

    try
    {
        using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        int totalRead = 0;
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;

            if (totalRead == buffer.Length)
            {
                byte[] newBuffer = ArrayPoolManager.RentBytes(buffer.Length * 2);
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, totalRead);
                ArrayPoolManager.ReturnBytes(buffer, clearArray: false);
                buffer = newBuffer;
            }
        }

        byte[] result = new byte[totalRead];
        Buffer.BlockCopy(buffer, 0, result, 0, totalRead);
        return result;
    }
    finally
    {
        ArrayPoolManager.ReturnBytes(buffer);
    }
}
```

### 5.2. Read + Direct Deserialization

If we do not need to return the bytes, only deserialize them:

```csharp
private static async Task<T?> DeserializeJsonAsync<T>(HttpContent content, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
{
    long? contentLength = content.Headers.ContentLength;
    int initialSize = contentLength.HasValue && contentLength.Value > 0
        ? (int)Math.Min(contentLength.Value, int.MaxValue)
        : 4096;

    byte[] buffer = ArrayPoolManager.RentBytes(initialSize);

    try
    {
        using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        int totalRead = 0;
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;

            if (totalRead == buffer.Length)
            {
                byte[] newBuffer = ArrayPoolManager.RentBytes(buffer.Length * 2);
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, totalRead);
                ArrayPoolManager.ReturnBytes(buffer, clearArray: false);
                buffer = newBuffer;
            }
        }

        ReadOnlySpan<byte> span = buffer.AsSpan(0, totalRead);
        return JsonSerializer.Deserialize(span, typeInfo);
    }
    finally
    {
        ArrayPoolManager.ReturnBytes(buffer);
    }
}
```

---

## 6. Integration with System.Text.Json

### 6.1. Advantages

- Avoids creating intermediate `string` instances  
- Operates directly on `ReadOnlySpan<byte>`  
- Allocates only the objects needed for the deserialized JSON graph

### 6.2. Usage with Source Generators

Example using `MercadoBitcoinJsonSerializerContext`:

```csharp
Ticker? ticker = JsonSerializer.Deserialize(
    span,
    MercadoBitcoinJsonSerializerContext.Default.Ticker);

List<Ticker>? tickers = JsonSerializer.Deserialize(
    span,
    MercadoBitcoinJsonSerializerContext.Default.ListTicker);
```

### 6.3. Deserialization Errors

- Catch `JsonException` and, if needed, log the payload (being careful not to log sensitive data)  
- Even in case of exceptions, **always** return the buffer to the pool inside the `finally` block

---

## 7. Integration with Retry/Logging

### 7.1. Logging Small Payloads

For debug logs that involve the raw payload:

- Avoid converting the entire array to a string  
- Limit the number of logged bytes (e.g., 512 bytes)

```csharp
ReadOnlySpan<byte> toLog = span.Length > 512 ? span[..512] : span;
string preview = Encoding.UTF8.GetString(toLog); // Acceptable in debug mode
logger.LogDebug("Response preview: {Preview}", preview);
```

### 7.2. RetryHandler

- When reprocessing the same content across multiple attempts, consider keeping the deserialized payload (objects) instead of reusing the raw buffer

---

## 8. ArrayPool‑Specific Tests

### 8.1. Unit Tests

- Create tests that exercise:
  - Responses with known `Content-Length` (small, medium, large)  
  - Responses without `Content-Length`  
  - Scenarios where the buffer must grow (2x, 4x, …)

### 8.2. Safety Tests

- Ensure there are no exceptions from using a `null` buffer or returning the same buffer twice  
- (Optional) In debug builds, add internal invariants (asserts) to validate that buffers are not used after being returned (hard to guarantee completely, but we can reduce risk)

### 8.3. Performance Tests

- Create benchmarks (see `18-BENCHMARKS.md`) comparing:
  - `ReadAsStringAsync` + `JsonSerializer.Deserialize(string, ...)`  
  - vs. `Stream` + `ArrayPool` + `JsonSerializer.Deserialize(ReadOnlySpan<byte>, ...)`

---

## 9. Common Errors and How to Avoid Them

1. **Forgetting the finally block**  
   - Always use a `try/finally` structure when working with pooled buffers  
2. **Returning the buffer to the pool before deserialization**  
   - Deserialization must happen **before** `ReturnBytes`  
3. **Storing buffers in static or instance fields**  
   - Forbidden. Buffers must remain local to the method  
4. **Assuming the buffer is zeroed**  
   - Never assume this; use `clearArray: true` when needed  
5. **Silent buffer overflow**  
   - The "grow buffer" logic must be tested with inputs larger than expected

---

## 10. Implementation Checklist

- [ ] Implement `ArrayPoolManager` in `Internal/Pooling/ArrayPoolManager.cs`  
- [ ] Replace `ReadAsStringAsync` with `Stream` + `ArrayPoolManager` in critical `AuthHttpClient` paths  
- [ ] Implement a private helper `ReadResponseToBufferAsync` (or similar) to reuse logic  
- [ ] Implement a private helper `DeserializeJsonAsync<T>` using `ArrayPoolManager`  
- [ ] Adapt `GeneratedClient` (or wrappers) to use the new helpers where feasible  
- [ ] Create unit tests for scenarios with/without `Content-Length` and growing buffers  
- [ ] Create benchmarks comparing the old vs. new approach

---

**Document**: 07-ARRAYPOOL-IMPLEMENTATION.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned / Awaiting Implementation  
**Next**: [08-SPAN-MEMORY.md](08-SPAN-MEMORY.md)

