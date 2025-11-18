# 15. INLINE ARRAYS (C# 12+) AND FIXED BUFFERS

## 📋 Index

1. [Objective](#objective)
2. [InlineArray Overview](#inlinearray-overview)
3. [Library Usage Scenarios](#library-usage-scenarios)
4. [InlineArray Type Design](#inlinearray-type-design)
5. [Integration with Span<T>](#integration-with-spant)
6. [Caveats and Limitations](#caveats-and-limitations)
7. [Implementation Plan](#implementation-plan)

---

## 1. Objective

Leverage **Inline Arrays** (C# 12+) to create small/medium fixed‑size buffers, focusing on:

- 💾 Reducing allocations of small arrays
- ⚡ Improving data locality in high‑performance scenarios

---

## 2. InlineArray Overview

### 2.1. InlineArray Attribute

Example:

```csharp
[InlineArray(32)]
public struct Buffer32<T>
{
    private T _element0;
}
```

- Creates a struct that behaves like an array of 32 elements
- Implicitly converts to `Span<T>` / `ReadOnlySpan<T>`

---

## 3. Library Usage Scenarios

### 3.1. Small Temporary Buffers

- E.g., small temporary lists of candles, tickers, or errors

### 3.2. Internal Optimization Structures

- E.g., fixed‑size caches used in indicator calculations

---

## 4. InlineArray Type Design

### 4.1. Examples

```csharp
namespace MercadoBitcoin.Client.Internal.Optimization;

[InlineArray(32)]
internal struct CandleBuffer32
{
    private CandleData _element0;
}

[InlineArray(16)]
internal struct DecimalBuffer16
{
    private decimal _element0;
}
```

---

## 5. Integration with Span<T>

### 5.1. Basic Usage

```csharp
CandleBuffer32 buffer = default;
Span<CandleData> span = buffer;

for (int i = 0; i < span.Length; i++)
{
    // preencher span[i]
}
```

### 5.2. Benefits

- Operate with Span‑based APIs without allocating arrays

---

## 6. Caveats and Limitations

- Size is fixed; do not use for collections with unknown/variable size
- Large structs are heavier to copy; use only where it makes sense

---

## 7. Implementation Plan

1. Create `CandleBuffer32`, `DecimalBuffer16` types in `Internal/Optimization`
2. Use them in scenarios where small temporary buffers are needed
3. Measure impact in specific benchmarks

---

**Document**: 15-INLINE-ARRAYS.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned
