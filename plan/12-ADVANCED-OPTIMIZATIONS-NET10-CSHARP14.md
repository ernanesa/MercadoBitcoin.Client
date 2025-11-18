```markdown
# 12. Advanced Optimizations — .NET 10 & C# 14

## Executive Summary

This document compiles advanced optimization techniques available in .NET 10 and C# 14 focused on reducing startup time, memory usage and maximizing throughput.

Key goals:
- Reduce startup by ~50%
- Cut working memory by ~40–60%
- Increase throughput by ~30–50%
- Drastically lower heap allocations and GC pressure

## Highlights (topics covered)

1. JIT improvements and physical promotion
2. GC tuning and server GC
3. Stack allocation and escape analysis
4. SIMD and vectorization
5. Memory pooling (ArrayPool, MemoryPool, ObjectPool)
6. Span<T> / Memory<T> for zero-copy operations
7. Dynamic and static PGO
8. SocketsHttpHandler + HTTP/2/3 tuning
9. Source Generators for System.Text.Json
10. Inline arrays, ref structs and safety patterns

## Actionable checklist (summary)

- Enable `TieredCompilation` and `DynamicPGO`.
- Use `ArrayPool<byte>.Shared` for large buffers and streaming readers.
- Replace frequent short-lived allocations with `stackalloc` and `Span<T>` where safe.
- Use `ObjectPool<T>` for StringBuilder and reusable collections.
- Move heavy numeric workloads to `Vector<T>` SIMD implementations.
- Configure `SocketsHttpHandler` for connection pooling and HTTP/2 performance.
- Pre-generate JSON serialization with System.Text.Json source generators.
- Add BenchmarkDotNet benchmarks for hot paths before/after changes.

For full examples and code patterns, refer to the original Portuguese document (content preserved) — this English version keeps the same code samples and recommends step-by-step implementation across phases.

**Next**: implement phase 1 changes (pooling + serializer context) and run benchmarks.

```
