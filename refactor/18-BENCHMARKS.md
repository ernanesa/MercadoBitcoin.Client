```markdown
# 18. Benchmarks (BenchmarkDotNet)

## Index
1. Objective
2. Tooling: BenchmarkDotNet
3. Benchmark scenarios
4. Project structure
5. Examples
6. Metrics
7. How to run
8. Acceptance criteria

---

## 1. Objective

Measure the impact of optimizations reliably using BenchmarkDotNet focusing on latency, allocations and throughput.

## 2. Tooling

Add `BenchmarkDotNet` in a separate benchmarks project.

## 3. Scenarios

- HTTP + JSON (GetTicker)
- Serialization/deserialization
- Pooling vs. non-pooling

## 4. Project Structure

Create `MercadoBitcoin.Client.Benchmarks` referencing the main project.

## 5. Examples

Deserialization benchmark using the source-generated Json context (Memory-based).

## 6. Metrics

- Mean time
- StdDev
- Allocated bytes

## 7. How to run

```bash
dotnet run -c Release -f net10.0 --project tests/MercadoBitcoin.Client.Benchmarks
```

## 8. Acceptance

Target meaningful improvements (20–30% or more) in allocations and latency for hot paths.

```
