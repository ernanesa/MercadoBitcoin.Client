# 13. CANDLEDATA AS STRUCT (VALUE TYPE)

## 📋 Index

1. [Objective](#objective)
2. [Current CandleData State](#current-candledata-state)
3. [Motivations for Struct](#motivations-for-struct)
4. [New Structure Design](#new-structure-design)
5. [Impact on JSON Serialization](#impact-on-json-serialization)
6. [Integration with Span/Memory](#integration-with-spanmemory)
7. [API Compatibility](#api-compatibility)
8. [Tests and Benchmarks](#tests-and-benchmarks)
9. [Migration Plan](#migration-plan)

---

## 1. Objective

Convert `CandleData` from `class` to `readonly struct`, reducing:

- Heap allocations
- GC pressure

And enabling better integration with:

- `Span<T>`
- Inline arrays

---

## 2. Current CandleData State

File: `src/MercadoBitcoin.Client/Models/CandleData.cs`.

### 2.1. Current Situation (summary)

- `CandleData` is a `class`
- Typical properties: `Timestamp`, `Open`, `High`, `Low`, `Close`, `Volume`
- Instances are created frequently by candle endpoints

### 2.2. Issues

- Each candle is a heap allocation
- Large candle collections generate many allocations

---

## 3. Motivations for Struct

### 3.1. Performance

- `readonly struct` avoids heap allocations when used in arrays/Spans
- Value copies are cheap if the size is moderate

### 3.2. Integration with Spans

- Enables `Span<CandleData>` and `InlineArray<CandleData>`
- Ideal for vectorized operations in technical indicators

---

## 4. New Structure Design

### 4.1. Assinatura

```csharp
namespace MercadoBitcoin.Client.Models;

public readonly struct CandleData
{
    public long Timestamp { get; }
    public decimal Open { get; }
    public decimal High { get; }
    public decimal Low { get; }
    public decimal Close { get; }
    public decimal Volume { get; }

    [JsonConstructor]
    public CandleData(
        long timestamp,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal volume)
    {
        Timestamp = timestamp;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
    }
}
```

### 4.2. Immutability

- Marked as `readonly struct` to prevent changes after creation

---

## 5. Impact on JSON Serialization

### 5.1. System.Text.Json

- `readonly struct` is fully supported
- Ensure `CandleData` is annotated in `MercadoBitcoinJsonSerializerContext`

```csharp
[JsonSerializable(typeof(CandleData))]
[JsonSerializable(typeof(List<CandleData>))]
```

### 5.2. Backwards Compatibility

- JSON fields/properties remain the same
- Change is transparent to JSON consumers

---

## 6. Integration with Span/Memory

### 6.1. Usage with Span

```csharp
Span<CandleData> candlesSpan = candlesArray;

for (int i = 0; i < candlesSpan.Length; i++)
{
    ref readonly CandleData candle = ref candlesSpan[i];
    // process
}
```

### 6.2. Inline Arrays

- Future: `InlineArray` for small candle buffers (see 15-INLINE-ARRAYS.md)

---

## 7. API Compatibility

### 7.1. Public Signatures

- Methods returning `IEnumerable<CandleData>` or `List<CandleData>` continue to work
- Serialized/deserialized data keeps the same JSON format

### 7.2. Binary Compatibility

- Binary breaking change (class → struct)
- Treat as part of v4.0.0 (major) release

---

## 8. Tests and Benchmarks

### 8.1. Functional Tests

- Validate all endpoints returning candles still work and produce correct JSON

### 8.2. Benchmarks

- Compare allocations and processing time for large candle lists (`10k+`)

---

## 9. Migration Plan

1. Update `CandleData` to `readonly struct`
2. Update `MercadoBitcoinJsonSerializerContext` to include `CandleData`/`List<CandleData>`
3. Run tests and adjust any places assuming reference semantics (class)
4. Update documentation (release notes) to indicate the change

---

**Document**: 13-CANDLEDATA-STRUCT.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned / Breaking change in v4.0.0
