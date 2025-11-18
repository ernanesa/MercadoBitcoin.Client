# 06. MEMORY POOLING (ArrayPool, MemoryPool, ObjectPool)

## 📋 Index

1. [Objectives and Goals](#objectives-and-goals)
2. [Current Scenario and Problems](#current-scenario-and-problems)
3. [Pooling Patterns to Adopt](#pooling-patterns-to-adopt)
4. [ArrayPool<T> - Network Buffers](#arraypoolt---network-buffers)
5. [MemoryPool<T> - Large Streams](#memorypoolt---large-streams)
6. [ObjectPool<T> - Reusable Objects](#objectpoolt---reusable-objects)
7. [Pool Manager Design](#pool-manager-design)
8. [Integration with Current Code](#integration-with-current-code)
9. [Best Practices and Pitfalls](#best-practices-and-pitfalls)
10. [Metrics and Validation](#metrics-and-validation)
11. [Implementation Checklist](#implementation-checklist)

---

## 1. Objectives and Goals

### General Objective

Introduce **systematic memory pooling** in the library using `ArrayPool<T>`, `MemoryPool<T>`, and `ObjectPool<T>` to:

- 💾 Reduce **70–90%** of array allocations in hot paths
- 🔥 Significantly decrease **GC pressure** (Gen0/Gen1)
- ⚡ Increase throughput of most-used endpoints (public and private)
- 🧱 Create reusable and centralized pooling infrastructure

### Quantitative Goals

- Reduce byte array (`byte[]`) allocations per request by **>= 80%**
- Reduce `StringBuilder` and temporary list allocations by **>= 60%**
- Decrease GC Gen0/min by **>= 50%** under 10k req/min load

---

## 2. Current Scenario and Problems

### 2.1. Current Code (v3.0.0)

Based on analysis of `AuthHttpClient`, `RetryHandler`, `JsonHelper`:

- Frequent use of `new byte[...]` when reading HTTP responses
- Use of `JsonSerializer.Serialize/Deserialize` with strings/arrays allocated for each operation
- Error and log strings built with concatenation or `StringBuilder` without pooling
- Absence of central pooling layer

### 2.2. Identified Problems

- ❌ **Repetitive allocations** of `byte[]` in response reading
- ❌ Lack of **buffer reuse** between requests
- ❌ `StringBuilder` created/destroyed in hot paths of logging/manual serialization
- ❌ No abstraction for pooling domain objects (e.g., error payloads)

---

## 3. Pooling Patterns to Adopt

### 3.1. ArrayPool<T>

- `ArrayPool<T>.Shared` for buffers of:
  - HTTP response reading (`byte[]`)
  - Serialization/deserialization with `JsonSerializer` when working with `Span<byte>`
- Encapsulated in `ArrayPoolManager` (see document 05-FOLDER-STRUCTURE)

### 3.2. MemoryPool<T>

- `MemoryPool<byte>.Shared` for:
  - Large streams (file downloads, endpoints with large payloads)
  - Chunked data processing (e.g., future async pagination)

### 3.3. ObjectPool<T>

- `ObjectPool<StringBuilder>` for:
  - Building JSON manually in critical paths
  - Building complex error messages

- `ObjectPool<ErrorResponse>` (internal) for:
  - Reusing error response instances in internal flow

---

## 4. ArrayPool<T> - Network Buffers

### 4.1. Design

Create `ArrayPoolManager` in `Internal/Pooling/ArrayPoolManager.cs` with:

- Minimal API:
  - `byte[] RentBytes(int minimumLength)`
  - `void ReturnBytes(byte[] array, bool clearArray = true)`
- Exclusive use via this manager (don't use `ArrayPool<byte>.Shared` directly elsewhere)

### 4.2. Manager Implementation (summary)

```csharp
namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class ArrayPoolManager
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;
    
    /// <summary>
    /// Rents a byte array from the pool.
    /// </summary>
    /// <param name="minimumLength">Minimum length needed</param>
    /// <returns>Byte array (may be larger than requested)</returns>
    public static byte[] RentBytes(int minimumLength)
    {
        return BytePool.Rent(minimumLength);
    }
    
    /// <summary>
    /// Returns a rented byte array to the pool.
    /// </summary>
    /// <param name="array">Array to return</param>
    /// <param name="clearArray">Whether to clear before returning</param>
    public static void ReturnBytes(byte[] array, bool clearArray = true)
    {
        BytePool.Return(array, clearArray);
    }
}
```

### 4.3. Usage Pattern

```csharp
// In AuthHttpClient
byte[] buffer = ArrayPoolManager.RentBytes(4096);
try
{
    await using var stream = await response.Content.ReadAsStreamAsync();
    int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
    
    // Process buffer
    var json = JsonSerializer.Deserialize(buffer.AsSpan(0, bytesRead), typeInfo);
}
finally
{
    ArrayPoolManager.ReturnBytes(buffer);
}
```

---

## 5. MemoryPool<T> - Large Streams

### 5.1. Design

Create `MemoryPoolManager` in `Internal/Pooling/MemoryPoolManager.cs`:

```csharp
namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class MemoryPoolManager
{
    /// <summary>
    /// Rents memory from the pool.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="minimumLength">Minimum length needed</param>
    /// <returns>IMemoryOwner auto-disposing</returns>
    public static IMemoryOwner<T> Rent<T>(int minimumLength)
    {
        return MemoryPool<T>.Shared.Rent(minimumLength);
    }
}
```

### 5.2. Usage

```csharp
// For large candle downloads
using IMemoryOwner<CandleData> memoryOwner = MemoryPoolManager.Rent<CandleData>(10000);
Memory<CandleData> buffer = memoryOwner.Memory;

// Process candles...
// Auto-disposed when leaving using block
```

---

## 6. ObjectPool<T> - Reusable Objects

### 6.1. Design

Create `ErrorResponsePool` in `Internal/Pooling/ErrorResponsePool.cs`:

```csharp
namespace MercadoBitcoin.Client.Internal.Pooling;

internal static class ErrorResponsePool
{
    private static readonly ObjectPool<ErrorResponse> Pool = 
        ObjectPool.Create<ErrorResponse>();
    
    public static ErrorResponse Rent()
    {
        var response = Pool.Get();
        response.Reset();
        return response;
    }
    
    public static void Return(ErrorResponse response)
    {
        Pool.Return(response);
    }
}
```

---

## 7. Pool Manager Design

### 7.1. Principles

1. **Single Responsibility**: Each manager handles one pool type
2. **Centralization**: All pooling through managers
3. **Exception Safety**: Use `try/finally` for return
4. **Reset Protocol**: Objects reset before reuse

### 7.2. Exception Safety Pattern

```csharp
byte[] buffer = ArrayPoolManager.RentBytes(4096);
try
{
    // Use buffer
}
catch (Exception ex)
{
    // Always return buffer even on error
    ArrayPoolManager.ReturnBytes(buffer);
    throw;
}
finally
{
    // Or guarantee return in finally
    ArrayPoolManager.ReturnBytes(buffer);
}
```

---

## 8. Integration with Current Code

### 8.1. AuthHttpClient

**Before**:
```csharp
var responseContent = await response.Content.ReadAsStringAsync();
var json = JsonSerializer.Deserialize(responseContent, /* ... */);
```

**After**:
```csharp
byte[] buffer = ArrayPoolManager.RentBytes(4096);
try
{
    await using var stream = await response.Content.ReadAsStreamAsync();
    int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
    
    var json = JsonSerializer.Deserialize(buffer.AsSpan(0, bytesRead), /* ... */);
}
finally
{
    ArrayPoolManager.ReturnBytes(buffer);
}
```

### 8.2. Bulk Operations

**Before**:
```csharp
var candles = new List<CandleData>(1000);
```

**After**:
```csharp
using var memory = MemoryPoolManager.Rent<CandleData>(1000);
Span<CandleData> candles = memory.Memory.Span;
```

---

## 9. Best Practices and Pitfalls

### ✅ Do's

- ✅ Always return arrays in `finally` block
- ✅ Never use array after returning to pool
- ✅ Clear sensitive data before returning
- ✅ Use `using` for `IMemoryOwner<T>`
- ✅ Document pool usage in comments

### ❌ Don'ts

- ❌ Return array twice (throws exception)
- ❌ Reuse array after return
- ❌ Ignore exceptions (use `try/finally`)
- ❌ Pool huge arrays (>= 1MB) unnecessarily
- ❌ Create private pools (use shared)

---

## 10. Metrics and Validation

### 10.1. Before/After Comparison

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| **Allocations/req** | ~5-10 | ~0-1 | -80-90% |
| **GC Gen0/min** | 100-150 | 30-50 | -70% |
| **Memory working set** | 150MB | 80MB | -47% |

### 10.2. Profiling

Use dotMemory to verify:
- Allocation count decreased
- ArrayPool allocations dominant (not new arrays)
- No memory leaks (arrays properly returned)

---

## 11. Implementation Checklist

- [ ] Create `ArrayPoolManager.cs`
- [ ] Create `MemoryPoolManager.cs`
- [ ] Create `ErrorResponsePool.cs`
- [ ] Create `StringBuilderPool.cs`
- [ ] Update `AuthHttpClient` to use `ArrayPoolManager`
- [ ] Update `RetryHandler` to use pooling
- [ ] Add unit tests for pool managers
- [ ] Add integration tests for pooling
- [ ] Profile and verify metrics
- [ ] Document pool usage patterns
- [ ] Run load tests (10k req/min)

---

**Document**: 06-MEMORY-POOLING.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: ✅ Complete  
**Next**: [07-ARRAYPOOL-IMPLEMENTATION.md](07-ARRAYPOOL-IMPLEMENTATION.md)

