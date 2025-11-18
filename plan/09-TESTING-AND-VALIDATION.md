```markdown
# Testing and Validation - Mercado Bitcoin API

## 🧪 Testing Strategy

### Test Pyramid

Unit tests: 80% | Integration: 15% | E2E: 5%

## 🔬 Unit Tests

Example: Order validation tests

```csharp
public class OrderValidatorTests
{
    [Fact]
    public void Validate_WithValidOrder_ReturnsTrue()
    {
        var validator = new OrderValidator();
        var order = new PlaceOrderRequest { Side = "buy", Type = "limit", Qty = "0.001", LimitPrice = 345000 };
        var result = validator.Validate(order);
        Assert.True(result.IsValid);
    }
}
```

Mocking HttpClient for unit tests is recommended (Mock<HttpMessageHandler>). See examples.

## 🔗 Integration Tests

Use fixtures to initialize authenticated clients when credentials are present. Skip tests if env vars are missing.

## 🎯 End-to-End Tests

E2E flows should cover a complete trading scenario (authenticate, check balance, place order, cancel, verify).

## 🚀 Performance Tests

Use BenchmarkDotNet for microbenchmarks and create load tests (e.g., 100 concurrent requests) to assert latency and throughput targets.

## 🔍 Validation Tests

Serialization round-trip tests ensure System.Text.Json context preserves model data.

## 🛡 Resilience Tests

Retry behavior tests and circuit breaker tests should simulate failures and assert expected behavior (retries count, fast-fail when circuit open).

## 📊 Coverage and Quality

Set coverage thresholds and collect coverage reports. Use `dotnet test --collect:"XPlat Code Coverage"` and `reportgenerator`.

## ✅ Test Checklist

- [ ] Unit tests for core logic
- [ ] Integration tests for public/private endpoints
- [ ] E2E tests for critical flows
- [ ] Performance and load tests
- [ ] Serialization validation
- [ ] Resilience and retry tests

**Next**: [10-IMPLEMENTATION-ROADMAP.md](10-IMPLEMENTATION-ROADMAP.md)

```
