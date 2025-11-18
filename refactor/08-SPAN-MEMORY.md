# 08. SPAN<T> AND MEMORY<T> (ZERO-COPY)

## 📋 Index

1. [Goal and Context](#goal-and-context)
2. [Overview of Span<T> and Memory<T>](#overview-of-spant-and-memoryt)
3. [General Usage Guidelines](#general-usage-guidelines)
4. [Project Usage Scenarios](#project-usage-scenarios)
5. [String Parsing Without Allocation](#string-parsing-without-allocation)
6. [Integration with JSON and Buffers](#integration-with-json-and-buffers)
7. [Stream Reading and Writing](#stream-reading-and-writing)
8. [Span Helpers (SpanHelpers)](#span-helpers-spanhelpers)
9. [Lifetime and Safety Concerns](#lifetime-and-safety-concerns)
10. [Metrics, Tests, and Validation](#metrics-tests-and-validation)
11. [Implementation Checklist](#implementation-checklist)

---

## 1. Goal and Context

This document defines how to use `Span<T>` and `Memory<T>` systematically in the library to achieve:

- 🚫 **Zero-copy** parsing and transformation operations  
- 💾 Fewer intermediate array and string allocations  
- ⚡ Better CPU cache usage by reducing heap objects

It is directly aligned with:

- Document `12-ADVANCED-OPTIMIZATIONS-NET10-CSHARP14.md`  
- Documents `06-MEMORY-POOLING.md` and `07-ARRAYPOOL-IMPLEMENTATION.md`

---

## 2. Overview of Span<T> and Memory<T>

### 2.1. Span<T>

- `ref struct` type that represents a "window" over:
  - Arrays (`T[]`)  
  - `stackalloc`  
  - String segments (`ReadOnlySpan<char>`)  
- Used **only on the stack** (cannot be a class field, cannot be captured by closures)

### 2.2. ReadOnlySpan<T>

- Read‑only version, ideal for parsing  
- Implicit conversions from `string`, `T[]`, and `ReadOnlyMemory<T>`

### 2.3. Memory<T>

- Similar to `Span<T>`, but can be used in:
  - Class fields  
  - `async` methods  
  - Closures  

- Always paired with `Span<T>` via the `.Span` property

---

## 3. General Usage Guidelines

1. Prefer `Span<T>` in synchronous, local code.  
2. Use `ReadOnlySpan<char>` for string parsing (e.g., symbols, query parameters).  
3. Use `Memory<byte>` for async stream operations.  
4. Avoid `string.Split`, `Substring`, `ToArray` allocations on hot paths.  
5. Encapsulate common patterns in helpers (`SpanHelpers`).

---

## 4. Project Usage Scenarios

### 4.1. Symbol String Parsing

- Examples: `"BTC-USD"`, `"BTC,50000.50,1.5"`  
- Currently likely implemented with `Split` and `Substring`  
- Target: replace with parsing using `ReadOnlySpan<char>`

### 4.2. Query String Processing

- Example: multiple symbols in a public endpoint  
- Target: use spans to avoid allocating arrays of strings

### 4.3. JSON Serialization/Deserialization with Buffers

- Receive `ReadOnlySpan<byte>` from `ArrayPoolManager`  
- Deserialize directly without allocating an intermediate JSON string

---

## 5. String Parsing Without Allocation

### 5.1. Simple CSV Parsing Example

```csharp
public static void ParseTickerLine(
    ReadOnlySpan<char> line,
    out ReadOnlySpan<char> symbol,
    out ReadOnlySpan<char> price,
    out ReadOnlySpan<char> quantity)
{
    int firstComma = line.IndexOf(',');
    if (firstComma < 0)
    {
        throw new FormatException("Invalid line: expected at least 2 commas.");
    }

    int secondComma = line.Slice(firstComma + 1).IndexOf(',');
    if (secondComma < 0)
    {
        throw new FormatException("Invalid line: expected 3 fields.");
    }

    secondComma += firstComma + 1;

    symbol = line[..firstComma].Trim();
    price = line[(firstComma + 1)..secondComma].Trim();
    quantity = line[(secondComma + 1)..].Trim();
}
```

### 5.2. Usage

```csharp
ReadOnlySpan<char> line = "BTC,50000.50,1.5";
ParseTickerLine(line, out var symbolSpan, out var priceSpan, out var qtySpan);

string symbol = symbolSpan.ToString(); // allocate only when truly needed
if (!decimal.TryParse(priceSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
{
    // handle error
}
```

---

## 6. Integration with JSON and Buffers

### 6.1. Input: ReadOnlySpan<byte>

Comes from `ArrayPoolManager` as specified in `07-ARRAYPOOL-IMPLEMENTATION.md`:

```csharp
ReadOnlySpan<byte> payload = buffer.AsSpan(0, totalRead);
var ticker = JsonSerializer.Deserialize(
    payload,
    MercadoBitcoinJsonSerializerContext.Default.Ticker);
```

### 6.2. Avoiding Conversion to string

- Do not do:

```csharp
string json = Encoding.UTF8.GetString(buffer, 0, totalRead);
var ticker = JsonSerializer.Deserialize(json, options);
```

- Always use `ReadOnlySpan<byte>`:

```csharp
ReadOnlySpan<byte> span = buffer.AsSpan(0, totalRead);
var ticker = JsonSerializer.Deserialize(span, typeInfo);
```

---

## 7. Stream Reading and Writing

### 7.1. Async Read with Memory<byte>

```csharp
public static async Task<int> ReadFullyAsync(Stream stream, Memory<byte> destination, CancellationToken cancellationToken)
{
    int totalRead = 0;

    while (totalRead < destination.Length)
    {
        int bytesRead = await stream.ReadAsync(destination.Slice(totalRead), cancellationToken).ConfigureAwait(false);
        if (bytesRead == 0)
        {
            break;
        }

        totalRead += bytesRead;
    }

    return totalRead;
}
```

### 7.2. Write using ReadOnlyMemory<byte>

```csharp
public static Task WriteAsync(Stream stream, ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
{
    return stream.WriteAsync(source, cancellationToken).AsTask();
}
```

---

## 8. Span Helpers (SpanHelpers)

### 8.1. Location

File: `Internal/Optimization/SpanHelpers.cs`

### 8.2. Examples of Useful Methods

```csharp
namespace MercadoBitcoin.Client.Internal.Optimization;

internal static class SpanHelpers
{
    public static bool TrySplitOnce(
        ReadOnlySpan<char> source,
        char separator,
        out ReadOnlySpan<char> left,
        out ReadOnlySpan<char> right)
    {
        int idx = source.IndexOf(separator);
        if (idx < 0)
        {
            left = source;
            right = ReadOnlySpan<char>.Empty;
            return false;
        }

        left = source[..idx];
        right = source[(idx + 1)..];
        return true;
    }

    public static ReadOnlySpan<char> Trim(ReadOnlySpan<char> span)
    {
        return span.Trim();
    }

    public static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }
}
```

### 8.3. Usage

```csharp
ReadOnlySpan<char> input = "BTC-USD";
if (SpanHelpers.TrySplitOnce(input, '-', out var left, out var right))
{
    // left = "BTC", right = "USD"
}
```

---

## 9. Lifetime and Safety Concerns

### 9.1. Do Not Capture Span in Closures

Wrong:

```csharp
ReadOnlySpan<char> span = symbol.AsSpan();
Func<bool> predicate = () => span.StartsWith("BTC"); // ❌ not allowed
```

Correct:

```csharp
string symbol = symbolInput; // regular string
Func<bool> predicate = () => symbol.StartsWith("BTC", StringComparison.Ordinal);
```

### 9.2. Do Not Store Span in Class Fields

- `Span<T>` and `ReadOnlySpan<T>` are `ref struct` and can only live on the stack.  
- When a buffer needs to live longer, use `Memory<T>` or `ReadOnlyMemory<T>`.

### 9.3. Interoperability with Existing APIs

- Many APIs still expect `string`, `byte[]`, etc.  
- Strategy: work with `Span<T>` internally and convert only at the boundary, as late as possible.

---

## 10. Metrics, Tests, and Validation

### 10.1. Metrics

- Number of `string` and `string[]` allocations (before/after)  
- Total bytes allocated for text parsing

### 10.2. Unit Tests

- Test `SpanHelpers` with varied inputs (empty strings, no separator, multiple separators).  
- Test symbol/line parsing with real‑world Mercado Bitcoin inputs.

### 10.3. Performance Tests

- Benchmarks comparing:
  - `string.Split` + `Substring`  
  - vs. parsing with `ReadOnlySpan<char>`

Pseudo benchmark example:

```csharp
[Benchmark]
public void ParseWithSplit()
{
    var parts = _line.Split(',');
}

[Benchmark]
public void ParseWithSpan()
{
    ReadOnlySpan<char> span = _line;
    SpanHelpers.TrySplitOnce(span, ',', out _, out _);
}
```

---

## 11. Implementation Checklist

- [ ] Create `Internal/Optimization/SpanHelpers.cs` with basic helpers.  
- [ ] Refactor symbol/query parsing to use `ReadOnlySpan<char>`.  
- [ ] Integrate with `ArrayPoolManager` to operate over `ReadOnlySpan<byte>` in JSON deserialization.  
- [ ] Add unit tests for `SpanHelpers`.  
- [ ] Add benchmarks comparing current approach vs. spans.

---

**Document**: 08-SPAN-MEMORY.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned / Awaiting Implementation  
**Next**: [09-HTTP-OPTIMIZATION.md](09-HTTP-OPTIMIZATION.md)
