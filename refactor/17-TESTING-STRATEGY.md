```markdown
# 17. Testing Strategy (Regression, Performance, Contract)

## Index
1. Objective
2. Current test status
3. Testing pillars
4. Unit tests
5. Integration tests
6. API contract tests
7. Performance tests
8. Regression and smoke tests
9. Test project organization
10. Checklist

---

## 1. Objective

Ensure refactoring and optimizations do not break:
- Public API contracts
- Functional behavior
- Minimum performance targets

## 2. Current status

Project: `tests/MercadoBitcoin.Client.ComprehensiveTests`.
Files include `PublicEndpointsTests`, `PrivateEndpointsTests`, `PaginationTests`, `PerformanceTests`.

## 3. Pillars

1. Correctness — unit & integration
2. Contracts — API shape and signatures
3. Performance — benchmarks and load tests

## 4. Unit Tests

Target internal components: pooling, rate limiter, span helpers.

## 5. Integration Tests

Validate public & private endpoints, ensure pooling and span refactors preserve behavior.

## 6. API Contract Tests

Use reflection-based tests to assert presence and signatures of public methods and DTOs.

## 7. Performance Tests

Use BenchmarkDotNet; measure serialization, HTTP hot paths and pooling impact.

## 8. Regression & Smoke

Run a small set of live calls as smoke tests to detect regressions early.

## 9. Project Organization

- `PublicEndpointsTests`
- `PrivateEndpointsTests`
- `PerformanceTests`
- `SerializationValidationTests`

## 10. Checklist

- [ ] Cover new internal components with unit tests
- [ ] Ensure contract tests pass after refactor
- [ ] Validate performance targets with benchmarks

**Status**: Draft — evolve alongside refactor work.

```
