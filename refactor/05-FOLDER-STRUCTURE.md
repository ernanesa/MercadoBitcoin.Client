# 05. FOLDER STRUCTURE

## 📋 Index

1. [Overview](#overview)
2. [Current Structure (v3.0.0)](#current-structure-v300)
3. [Target Structure (v4.0.0)](#target-structure-v400)
4. [Justification of Changes](#justification-of-changes)
5. [Migration Plan](#migration-plan)
6. [Naming Conventions](#naming-conventions)

---

## 1. Overview

### Objective

Reorganize the folder structure to:
- ✅ Improve organization of optimized code
- ✅ Separate concerns (pooling, rate limiting, optimization)
- ✅ Facilitate navigation and maintenance
- ✅ Prepare for future extensions

### Principles

1. **High Cohesion**: Related files in same folder
2. **Low Coupling**: Clear dependencies between folders
3. **Discoverability**: Easy to find related code
4. **Scalability**: Structure supports growth

---

## 2. Current Structure (v3.0.0)

```
src/MercadoBitcoin.Client/
├── Client/
│   ├── MercadoBitcoinClient.cs
│   ├── MercadoBitcoinClient.Account.cs
│   ├── MercadoBitcoinClient.Public.cs
│   ├── MercadoBitcoinClient.Trading.cs
│   └── MercadoBitcoinClient.Wallet.cs
├── Configuration/
│   └── MercadoBitcoinClientOptions.cs
├── Errors/
│   ├── ErrorResponse.cs
│   └── MercadoBitcoinApiException.cs
├── Extensions/
│   ├── CandleExtensions.cs
│   ├── MercadoBitcoinClientExtensions.cs
│   └── WithdrawLimitsExtensions.cs
├── Generated/
│   ├── GeneratedClient.cs
│   └── GeneratedClient.Partial.Aot.cs
├── Http/
│   ├── AuthHttpClient.cs
│   ├── HttpConfiguration.cs
│   ├── RetryHandler.cs
│   └── RetryPolicyConfig.cs
├── Internal/
│   ├── AsyncPaginationHelper.cs
│   ├── AsyncRateLimiter.cs
│   └── JsonHelper.cs
├── Models/
│   └── CandleData.cs
└── MercadoBitcoinJsonSerializerContext.cs
```

### Analysis of Current Structure

| Folder | Purpose | Assessment |
|--------|---------|------------|
| **Client/** | Main facade | ✅ Well organized |
| **Configuration/** | Options | ✅ Adequate |
| **Errors/** | Error handling | ✅ Adequate |
| **Extensions/** | Extension methods | ✅ Adequate |
| **Generated/** | Generated code | ✅ Adequate |
| **Http/** | HTTP handlers | ✅ Adequate |
| **Internal/** | Internal helpers | ⚠️ Too generic, mixes pooling + rate limiting + helpers |
| **Models/** | DTOs | ⚠️ Underutilized |

**Problems Identified**:
1. **Internal/** too generic
2. **Models/** underutilized
3. No folder for pooling
4. No folder for optimizations

---

## 3. Target Structure (v4.0.0)

```
src/MercadoBitcoin.Client/
├── Client/                                    # ✅ Keep
│   ├── MercadoBitcoinClient.cs
│   ├── MercadoBitcoinClient.Account.cs
│   ├── MercadoBitcoinClient.Public.cs
│   ├── MercadoBitcoinClient.Trading.cs
│   └── MercadoBitcoinClient.Wallet.cs
├── Configuration/                             # ✅ Keep
│   └── MercadoBitcoinClientOptions.cs
├── Errors/                                    # ✅ Keep
│   ├── ErrorResponse.cs
│   ├── MercadoBitcoinApiException.cs
│   └── MercadoBitcoinUnauthorizedException.cs
├── Extensions/                                # ✅ Keep
│   ├── CandleExtensions.cs
│   ├── MercadoBitcoinClientExtensions.cs
│   └── WithdrawLimitsExtensions.cs
├── Generated/                                 # ✅ Keep
│   ├── GeneratedClient.cs
│   └── GeneratedClient.Partial.Aot.cs
├── Http/                                      # ✅ Keep
│   ├── AuthHttpClient.cs
│   ├── HttpConfiguration.cs
│   ├── RetryHandler.cs
│   └── RetryPolicyConfig.cs
├── Internal/                                  # ✅ Reorganized
│   ├── Helpers/                               # ➕ NEW
│   │   ├── AsyncPaginationHelper.cs
│   │   └── JsonHelper.cs
│   ├── Pooling/                               # ➕ NEW
│   │   ├── ArrayPoolManager.cs
│   │   ├── MemoryPoolManager.cs
│   │   ├── ErrorResponsePool.cs
│   │   └── StringBuilderPool.cs
│   ├── RateLimiting/                          # ➕ NEW
│   │   ├── RateLimiterFactory.cs
│   │   └── RateLimiterMetrics.cs
│   └── Optimization/                          # ➕ NEW
│       ├── ValueStringBuilder.cs
│       └── SpanHelpers.cs
├── Models/                                    # ✅ Expanded
│   ├── CandleData.cs
│   ├── ValueTypes/                            # ➕ NEW
│   │   └── SymbolSpan.cs
│   └── Enums/                                 # ➕ NEW
│       └── OutcomeType.cs
└── MercadoBitcoinJsonSerializerContext.cs     # ✅ Keep
```

### New Total

- **Folders**: 13 (before: 7)
- **Subfolders**: 6 new
- **Files**: ~30 (before: ~20)

---

## 4. Justification of Changes

### 4.1. Internal/Helpers/

**Before**: AsyncPaginationHelper.cs and JsonHelper.cs mixed with AsyncRateLimiter

**After**: Separated into `Internal/Helpers/`

**Justification**:
- ✅ Cohesion: Helpers unrelated to pooling/rate limiting
- ✅ Clarity: Explicit folder purpose

---

### 4.2. Internal/Pooling/

**New files**:
- `ArrayPoolManager.cs`: Manages ArrayPool<byte> for HTTP responses
- `MemoryPoolManager.cs`: Manages MemoryPool<T> for large buffers
- `ErrorResponsePool.cs`: ObjectPool<ErrorResponse>
- `StringBuilderPool.cs`: ObjectPool<StringBuilder>

**Justification**:
- ✅ Centralizes pooling logic
- ✅ Facilitates reuse
- ✅ Isolated maintenance

---

### 4.3. Internal/RateLimiting/

**New files**:
- `RateLimiterFactory.cs`: Factory for creating TokenBucketRateLimiter
- `RateLimiterMetrics.cs`: Rate limiting metrics

**Justification**:
- ✅ Encapsulates System.Threading.RateLimiting
- ✅ Facilitates configuration
- ✅ Centralized metrics

**Migration**:
- ❌ `AsyncRateLimiter.cs` will be **deleted**
- ✅ Replaced by native `System.Threading.RateLimiting.TokenBucketRateLimiter`

---

### 4.4. Internal/Optimization/

**New files**:
- `ValueStringBuilder.cs`: Stack-based string builder
- `SpanHelpers.cs`: Helpers for Span<T> operations

**Justification**:
- ✅ Centralizes optimization code
- ✅ Reusable throughout project
- ✅ Clear documentation of advanced techniques

---

### 4.5. Models/ValueTypes/

**New files**:
- `SymbolSpan.cs`: Symbol representation with ReadOnlyMemory<char>

**Justification**:
- ✅ Separate value types from reference types
- ✅ Facilitate class-to-struct conversion

---

### 4.6. Models/Enums/

**New files**:
- `OutcomeType.cs`: Enum for request outcomes (success, error, timeout, etc)

**Justification**:
- ✅ Replace strings with enums (zero allocation)
- ✅ Type-safety

---

## 5. Migration Plan

### Phase 1: Create New Folders (Day 1)

```bash
cd src/MercadoBitcoin.Client

# Create subfolders
mkdir Internal/Helpers
mkdir Internal/Pooling
mkdir Internal/RateLimiting
mkdir Internal/Optimization
mkdir Models/ValueTypes
mkdir Models/Enums
```

---

### Phase 2: Move Existing Files (Day 1)

```bash
# Move helpers
git mv Internal/AsyncPaginationHelper.cs Internal/Helpers/
git mv Internal/JsonHelper.cs Internal/Helpers/

# AsyncRateLimiter will be deleted in Phase 4
```

---

### Phase 3: Create New Files (Days 2-5)

#### Internal/Pooling/ArrayPoolManager.cs

```csharp
namespace MercadoBitcoin.Client.Internal.Pooling;

public static class ArrayPoolManager
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;
    
    public static byte[] RentBytes(int minimumLength)
    {
        return BytePool.Rent(minimumLength);
    }
    
    public static void ReturnBytes(byte[] array, bool clearArray = true)
    {
        BytePool.Return(array, clearArray);
    }
}
```

#### Internal/Pooling/ErrorResponsePool.cs

```csharp
namespace MercadoBitcoin.Client.Internal.Pooling;

public static class ErrorResponsePool
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

#### Internal/RateLimiting/RateLimiterFactory.cs

```csharp
namespace MercadoBitcoin.Client.Internal.RateLimiting;

public static class RateLimiterFactory
{
    public static TokenBucketRateLimiter CreateTokenBucket(int requestsPerSecond)
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = Math.Max(10, requestsPerSecond / 10),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = requestsPerSecond,
            AutoReplenishment = true
        });
    }
}
```

#### Internal/Optimization/ValueStringBuilder.cs

```csharp
namespace MercadoBitcoin.Client.Internal.Optimization;

public ref struct ValueStringBuilder
{
    private Span<char> _buffer;
    private int _position;
    
    public ValueStringBuilder(Span<char> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }
    
    public void Append(ReadOnlySpan<char> value)
    {
        value.CopyTo(_buffer.Slice(_position));
        _position += value.Length;
    }
    
    public void Append(char value)
    {
        _buffer[_position++] = value;
    }
    
    public int Length => _position;
    
    public override string ToString()
    {
        return _buffer.Slice(0, _position).ToString();
    }
    
    public void Dispose()
    {
        // No-op for stack-allocated buffer
    }
}
```

#### Models/Enums/OutcomeType.cs

```csharp
namespace MercadoBitcoin.Client.Models.Enums;

public enum OutcomeType
{
    Success,
    HttpError,
    Timeout,
    Cancellation,
    NetworkError,
    CircuitBreakerOpen,
    RateLimitExceeded,
    AuthenticationError,
    UnknownError
}
```

---

### Phase 4: Update Imports (Day 6)

#### Files to Update

```csharp
// MercadoBitcoinClient.cs
using MercadoBitcoin.Client.Internal.Helpers;      // AsyncPaginationHelper
using MercadoBitcoin.Client.Internal.RateLimiting; // RateLimiterFactory

// AuthHttpClient.cs
using MercadoBitcoin.Client.Internal.Pooling;      // ArrayPoolManager

// RetryHandler.cs
using MercadoBitcoin.Client.Models.Enums;          // OutcomeType
```

---

### Phase 5: Delete AsyncRateLimiter (Day 7)

```bash
git rm Internal/AsyncRateLimiter.cs
```

**Justification**: Replaced by `System.Threading.RateLimiting.TokenBucketRateLimiter`

---

## 6. Naming Conventions

### 6.1. Namespaces

| Folder | Namespace |
|--------|-----------|
| `Client/` | `MercadoBitcoin.Client` |
| `Configuration/` | `MercadoBitcoin.Client.Configuration` |
| `Errors/` | `MercadoBitcoin.Client.Errors` |
| `Extensions/` | `MercadoBitcoin.Client.Extensions` |
| `Generated/` | `MercadoBitcoin.Client.Generated` |
| `Http/` | `MercadoBitcoin.Client.Http` |
| `Internal/Helpers/` | `MercadoBitcoin.Client.Internal.Helpers` |
| `Internal/Pooling/` | `MercadoBitcoin.Client.Internal.Pooling` |
| `Internal/RateLimiting/` | `MercadoBitcoin.Client.Internal.RateLimiting` |
| `Internal/Optimization/` | `MercadoBitcoin.Client.Internal.Optimization` |
| `Models/` | `MercadoBitcoin.Client.Models` |
| `Models/ValueTypes/` | `MercadoBitcoin.Client.Models.ValueTypes` |
| `Models/Enums/` | `MercadoBitcoin.Client.Models.Enums` |

### 6.2. File Suffixes

| Suffix | Purpose | Example |
|--------|---------|---------|
| `*Manager.cs` | Resource managers | `ArrayPoolManager.cs` |
| `*Pool.cs` | Object pools | `ErrorResponsePool.cs` |
| `*Factory.cs` | Factories | `RateLimiterFactory.cs` |
| `*Helper.cs` | Static helpers | `JsonHelper.cs` |
| `*Extensions.cs` | Extension methods | `CandleExtensions.cs` |
| `*Handler.cs` | HTTP handlers | `RetryHandler.cs` |
| `*Options.cs` | Configuration options | `MercadoBitcoinClientOptions.cs` |

### 6.3. Access Modifiers

| Folder | Default Access | Justification |
|--------|----------------|---------------|
| `Client/` | `public` | Public API |
| `Configuration/` | `public` | Public API |
| `Errors/` | `public` | Public API |
| `Extensions/` | `public` | Public API |
| `Generated/` | `public` | Generated by tool |
| `Http/` | `public` | Can be injected |
| `Internal/**` | `internal` | Internal implementation |
| `Models/` | `public` | Public DTOs |

---

## 7. Migration Checklist

### ✅ Phase 1: Structure (Day 1)

- [ ] Create `Internal/Helpers/`
- [ ] Create `Internal/Pooling/`
- [ ] Create `Internal/RateLimiting/`
- [ ] Create `Internal/Optimization/`
- [ ] Create `Models/ValueTypes/`
- [ ] Create `Models/Enums/`

### ✅ Phase 2: Reorganization (Day 1)

- [ ] Move `AsyncPaginationHelper.cs` to `Internal/Helpers/`
- [ ] Move `JsonHelper.cs` to `Internal/Helpers/`
- [ ] Update namespaces in moved files
- [ ] Update imports in dependent files

### ✅ Phase 3: New Files (Days 2-5)

- [ ] Create `ArrayPoolManager.cs`
- [ ] Create `MemoryPoolManager.cs`
- [ ] Create `ErrorResponsePool.cs`
- [ ] Create `StringBuilderPool.cs`
- [ ] Create `RateLimiterFactory.cs`
- [ ] Create `RateLimiterMetrics.cs`
- [ ] Create `ValueStringBuilder.cs`
- [ ] Create `SpanHelpers.cs`
- [ ] Create `OutcomeType.cs`

### ✅ Phase 4: Integration (Day 6)

- [ ] Update `MercadoBitcoinClient.cs` to use new pools/rate limiters
- [ ] Update `AuthHttpClient.cs` to use `ArrayPoolManager`
- [ ] Update `RetryHandler.cs` to use `OutcomeType`
- [ ] Run tests

### ✅ Phase 5: Cleanup (Day 7)

- [ ] Delete `AsyncRateLimiter.cs`
- [ ] Update documentation
- [ ] Commit and push

---

## 8. Conclusion

### Benefits of New Structure

1. ✅ **Organization**: Code separated by concern
2. ✅ **Discoverability**: Easy to find related code
3. ✅ **Maintenance**: Isolation facilitates changes
4. ✅ **Scalability**: Structure supports growth
5. ✅ **Clarity**: Each folder's purpose is evident

### Next Steps

1. ➡️ **Implement pools**: [06-MEMORY-POOLING.md](06-MEMORY-POOLING.md)
2. ➡️ **Implement rate limiter**: [11-RATE-LIMITING.md](11-RATE-LIMITING.md)
3. ➡️ **Implement optimizations**: [08-SPAN-MEMORY.md](08-SPAN-MEMORY.md)

---

**Document**: 05-FOLDER-STRUCTURE.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: ✅ Complete  
**Next**: [06-MEMORY-POOLING.md](06-MEMORY-POOLING.md)

