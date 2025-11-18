# 16. REF STRUCTS, SPAN-LIKE TYPES, AND MEMORY SAFETY

## 📋 Index

1. [Objective](#objective)
2. [ref struct Overview](#ref-struct-overview)
3. [Library Scenarios](#library-scenarios)
4. [ref struct Design Patterns](#ref-struct-design-patterns)
5. [Lifetime, scoped, and Safety](#lifetime-scoped-and-safety)
6. [Integration with Span<T> and Inline Arrays](#integration-with-spant-and-inline-arrays)
7. [Caveats and Limitations](#caveats-and-limitations)
8. [Implementation Plan](#implementation-plan)

---

## 1. Objective

Define how and when to use **ref struct** in the library, mainly for types that:

- Involve Span/Memory
- Must ensure data remains on the stack

---

## 2. ref struct Overview

- `ref struct` enables stack-only types
- They cannot be:
  - Boxed
  - Captured in closures
  - Stored in class fields

Example:

```csharp
public ref struct TokenParser
{
    private Span<char> _buffer;

    public TokenParser(Span<char> buffer)
    {
        _buffer = buffer;
    }
}
```

---

## 3. Library Scenarios

### 3.1. Token Parsing

- Parsing of symbols, query parameters, etc.

### 3.2. OrderBook Snapshots

- `ref struct` structure for fast best bid/ask access (see advanced optimizations doc)

---

## 4. ref struct Design Patterns

### 4.1. Lightweight Types

- Should be small and not perform I/O directly

### 4.2. Span Aggregates

- Store `Span<T>` / `ReadOnlySpan<T>` internally to simplify APIs

---

## 5. Lifetime, scoped, and Safety

### 5.1. scoped

Use `scoped` to ensure spans do not escape:

```csharp
public ReadOnlySpan<char> NextToken(scoped ReadOnlySpan<char> delimiter)
{
    // implementação
}
```

### 5.2. Rules

- `ref struct` should only be used in synchronous methods

---

## 6. Integration with Span<T> and Inline Arrays

- `ref struct` can operate over `Span<T>` from inline arrays, pooled buffers, etc.

---

## 7. Caveats and Limitations

- Do not expose `ref struct` in public APIs unless strictly necessary
- Prefer internal usage (`internal`) to keep future flexibility

---

## 8. Implementation Plan

1. Create `TokenParser` as an internal ref struct for complex string parsing
2. Evaluate `OrderBookSnapshot` as a ref struct for intensive scenarios
3. Ensure all usages respect lifetime rules

---

**Document**: 16-REF-STRUCTS.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned
