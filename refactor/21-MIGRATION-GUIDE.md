```markdown
# 21. Migration Guide v3.x → v4.0.0

## Overview

This guide helps users migrate from v3.x (.NET 9) to v4.0.0 (target: .NET 10 + C# 14).

## Key Changes

- Target framework updated to .NET 10
- Performance and memory improvements
- Some models converted to structs (e.g., `CandleData`)
- Native rate limiting via `System.Threading.RateLimiting`

## Steps

1. Update `TargetFramework` to `net10.0`.
2. Update package references to the recommended 10.0 versions.
3. Rebuild and run tests; address any binary compatibility issues.
4. Run integration tests and validate critical flows.

## Post-migration checks

- Validate public endpoints
- Monitor metrics and adjust rate-limiting/timeout settings

```
