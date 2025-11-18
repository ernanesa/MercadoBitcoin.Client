```markdown
# 22. Release Notes — v4.0.0

## Summary
Version: 4.0.0 — Target: .NET 10 / C# 14. Focus: performance, memory efficiency, AOT readiness.

## Highlights
- Optimized HTTP pipeline with `SocketsHttpHandler`.
- Native rate limiting via `System.Threading.RateLimiting`.
- Extensive use of pooling and source generators for JSON.

## Performance Improvements

- 30–50% higher throughput on main endpoints
- 40–70% fewer allocations on hot paths

## Potentially Breaking Changes

- `CandleData` moved from class → `readonly struct`.
- Rate limiting/timeouts may be stricter by default.

## Migration Recommendations

See `21-MIGRATION-GUIDE.md` for detailed steps.

```
