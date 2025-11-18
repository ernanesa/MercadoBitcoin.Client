# 03. REFACTORING ROADMAP

## 📋 Index

1. [Overview](#overview)
2. [Executive Timeline](#executive-timeline)
3. [Phase 1: Analysis and Preparation](#phase-1-analysis-and-preparation)
4. [Phase 2: Infrastructure Migration](#phase-2-infrastructure-migration)
5. [Phase 3: Memory Optimizations](#phase-3-memory-optimizations)
6. [Phase 4: Performance Optimizations](#phase-4-performance-optimizations)
7. [Phase 5: Models and Structures](#phase-5-models-and-structures)
8. [Phase 6: Testing and Validation](#phase-6-testing-and-validation)
9. [Phase 7: Documentation and Release](#phase-7-documentation-and-release)
10. [Checkpoints and Rollback](#checkpoints-and-rollback)
11. [Risk Management](#risk-management)
12. [Communication and Stakeholders](#communication-and-stakeholders)

---

## 1. Overview

### Refactoring Objective

Migrate **MercadoBitcoin.Client v3.0.0 (.NET 9)** to **v4.0.0 (.NET 10)** implementing all advanced optimizations identified in documents 01 and 02.

### Quantitative Goals

| Metric | v3.0.0 (Current) | v4.0.0 (Target) | Gain |
|---------|-----------------|-----------------|------|
| **Startup Time** | ~800ms | ~400ms | -50% |
| **Memory (Working Set)** | ~150MB | ~80MB | -47% |
| **Throughput** | ~10k req/s | ~15k req/s | +50% |
| **P99 Latency** | ~100ms | ~30ms | -70% |
| **Heap Allocations** | ~100MB/s | ~30MB/s | -70% |
| **GC Gen0/min** | ~100 | ~30 | -70% |
| **GC Pause** | ~50ms | ~10ms | -80% |

### Refactoring Principles

1. ✅ **100% API Compatibility**: No breaking changes in public API
2. ✅ **Incremental**: Each phase delivers isolated value
3. ✅ **Testable**: Each change is validated with automated tests
4. ✅ **Reversible**: Each phase has rollback plan
5. ✅ **Measurable**: Benchmarks before/after each phase
6. ✅ **Documented**: Each decision is documented

---

## 2. Executive Timeline

### 9-Week Overview

```
Week 1: Analysis and Preparation
├─ Detailed code analysis
├─ Benchmark configuration
└─ .NET 10 environment setup

Week 2: Infrastructure Migration
├─ Upgrade to .NET 10 / C# 14
├─ PGO and GC configuration
└─ Dependencies update

Weeks 3-4: Memory Optimizations
├─ ArrayPool implementation
├─ MemoryPool implementation
└─ ObjectPool implementation

Weeks 5-6: Performance Optimizations
├─ AsyncRateLimiter replacement
├─ HTTP/3 and SocketsHttpHandler
├─ Span<T> and stackalloc
└─ Polly V4

Week 7: Models and Structures
├─ CandleData class → struct
├─ String optimizations (ReadOnlyMemory)
└─ Inline arrays

Week 8: Testing and Validation
├─ Regression tests
├─ Final benchmarks
└─ Load testing

Week 9: Documentation and Release
├─ Migration guide
├─ Release notes
├─ Documentation updates
└─ Release v4.0.0
```

### Effort Distribution

| Phase | Duration | Complexity | Risk | Priority |
|------|----------|-----------|------|----------|
| **Phase 1** | 1 week | ⭐⭐ | Low | 🔴 P0 |
| **Phase 2** | 1 week | ⭐⭐ | Medium | 🔴 P0 |
| **Phase 3** | 2 weeks | ⭐⭐⭐ | Medium | 🔴 P0 |
| **Phase 4** | 2 weeks | ⭐⭐⭐⭐ | High | 🔴 P0 |
| **Phase 5** | 1 week | ⭐⭐⭐⭐ | High | 🔴 P0 |
| **Phase 6** | 1 week | ⭐⭐ | Low | 🟠 P1 |
| **Phase 7** | 1 week | ⭐ | Low | 🟠 P1 |

---

## 3. Phase 1: Analysis and Preparation

**Duration**: Week 1 (5 business days)  
**Objective**: Establish baseline and prepare environment

### Day 1: Environment Setup

#### Tasks
- [ ] Install .NET 10 SDK
- [ ] Configure Visual Studio 2025 / Rider
- [ ] Install profiling tools (dotTrace, dotMemory)
- [ ] Configure BenchmarkDotNet
- [ ] Setup CI/CD for .NET 10

#### Deliverables
- ✅ Functional .NET 10 environment
- ✅ Installed profilers
- ✅ Configured BenchmarkDotNet

#### Commands
```bash
# Install .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

# Verify installation
dotnet --version  # Should show 10.0.x

# Create working branch
git checkout -b refactor/net10-migration
```

---

### Day 2-3: Performance Baseline

#### Tasks
- [ ] Create benchmarks for hot paths
  - [ ] HTTP request/response cycle
  - [ ] JSON serialization/deserialization
  - [ ] Rate limiting
  - [ ] Error handling
  - [ ] Bulk operations (1000 candles)
- [ ] Run benchmarks on .NET 9 (baseline)
- [ ] Analyze allocations with dotMemory
- [ ] Profile CPU with dotTrace
- [ ] Document baseline metrics

#### Deliverables
- ✅ Complete benchmark suite
- ✅ Documented baseline metrics
- ✅ Allocations report
- ✅ Hot paths identification

#### Benchmark Example
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net100)]
public class HttpClientBenchmarks
{
    private MercadoBitcoinClient _client;
    
    [GlobalSetup]
    public void Setup()
    {
        // Setup client
    }
    
    [Benchmark]
    public async Task<Ticker> GetTicker()
    {
        return await _client.GetTickerAsync("BRLBTC");
    }
    
    [Benchmark]
    public async Task<List<CandleData>> GetCandles()
    {
        return await _client.GetCandlesAsync("BRLBTC", "1d", 1000);
    }
}
```

---

### Day 4: Dependency Analysis

#### Tasks
- [ ] List all NuGet dependencies
- [ ] Verify .NET 10 compatibility
- [ ] Identify target versions
- [ ] Check breaking changes
- [ ] Document update plan

#### Dependencies to Update

| Package | v3.0.0 | v4.0.0 | Breaking Changes |
|---------|--------|--------|------------------|
| **Microsoft.Extensions.DependencyInjection.Abstractions** | 9.0.0 | 10.0.0 | None |
| **Microsoft.Extensions.Http** | 9.0.0 | 10.0.0 | None |
| **Microsoft.Extensions.Options** | 9.0.0 | 10.0.0 | None |
| **Polly** | 8.2.0 | 8.5.0+ | None |
| **Polly.Extensions.Http** | 3.0.0 | 3.0.0 | None |
| **NSwag.MSBuild** | 14.5.0 | 14.5.0+ | Verify |

---

### Day 5: Documentation and Planning

#### Tasks
- [ ] Review documents 01-ANALISE and 02-GAPS
- [ ] Create detailed prioritization matrix
- [ ] Define success criteria per phase
- [ ] Develop rollback plan
- [ ] Present roadmap to stakeholders

#### Deliverables
- ✅ Document 03-ROADMAP (this document)
- ✅ Defined success criteria
- ✅ Documented rollback plan
- ✅ Stakeholder approval

---

## 4. Phase 2: Infrastructure Migration

**Duration**: Week 2 (5 business days)  
**Objective**: Migrate to .NET 10 and configure base optimizations

### Day 1: Framework Upgrade

#### Tasks
- [ ] Update TargetFramework to net10.0
- [ ] Update LangVersion to 14.0
- [ ] Compile project
- [ ] Run tests
- [ ] Check warnings

#### .csproj Changes
```xml
<!-- Before -->
<TargetFramework>net9.0</TargetFramework>
<LangVersion>13.0</LangVersion>

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
<LangVersion>14.0</LangVersion>
```

#### Validation
```bash
dotnet build
dotnet test
dotnet list package --vulnerable
```

---

### Day 2: Dependencies Update

#### Tasks
- [ ] Update Microsoft.Extensions.* to 10.0.0
- [ ] Update Polly to 8.5.0+
- [ ] Verify NSwag.MSBuild compatibility
- [ ] Resolve version conflicts
- [ ] Run regression tests

#### Commands
```bash
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.0
dotnet add package Microsoft.Extensions.Http --version 10.0.0
dotnet add package Microsoft.Extensions.Options --version 10.0.0
dotnet add package Polly --version 8.5.0
```

---

### Day 3: PGO Configuration

#### Tasks
- [ ] Add TieredCompilation flags
- [ ] Enable Dynamic PGO
- [ ] Configure runtimeconfig.json
- [ ] Test with benchmarks
- [ ] Measure performance gain

#### Configuration
```xml
<PropertyGroup>
  <TieredCompilation>true</TieredCompilation>
  <TieredCompilationQuickJit>true</TieredCompilationQuickJit>
  <TieredCompilationQuickJitForLoops>false</TieredCompilationQuickJitForLoops>
  <DynamicPGO>true</DynamicPGO>
</PropertyGroup>
```

#### Validation
- Run benchmarks
- Compare with baseline
- Expect +10-15% throughput

---

### Day 4: GC Configuration

#### Tasks
- [ ] Configure Server GC
- [ ] Configure Concurrent GC
- [ ] Configure RetainVM
- [ ] Adjust HeapCount
- [ ] Test with load tests

#### Configuration
```xml
<PropertyGroup>
  <ServerGarbageCollection>true</ServerGarbageCollection>
  <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
  <RetainVMGarbageCollection>true</RetainVMGarbageCollection>
</PropertyGroup>
```

```json
{
  "runtimeOptions": {
    "configProperties": {
      "System.GC.Server": true,
      "System.GC.Concurrent": true,
      "System.GC.RetainVM": true,
      "System.GC.HeapCount": 8
    }
  }
}
```

---

### Day 5: Validation and Checkpoint

#### Tasks
- [ ] Run complete test suite
- [ ] Run benchmarks
- [ ] Compare metrics with baseline
- [ ] Document gains
- [ ] Commit and tag checkpoint-phase2

#### Success Criteria
- ✅ All tests pass
- ✅ +10-15% throughput (PGO)
- ✅ -5-10% memory usage (GC config)
- ✅ No breaking changes

#### Checkpoint
```bash
git add .
git commit -m "chore: migrate to .NET 10 with PGO and Server GC"
git tag checkpoint-phase2
git push origin refactor/net10-migration
```

---

## 5. Phase 3: Memory Optimizations

**Duration**: Weeks 3-4 (10 business days)  
**Objective**: Implement object and buffer pooling

### Week 3, Day 1-2: ArrayPool Implementation

#### Tasks
- [ ] Create HttpBufferPool wrapper
- [ ] Refactor AuthHttpClient.SendAsync
- [ ] Replace ReadAsStringAsync with ReadAsStreamAsync + ArrayPool
- [ ] Refactor RetryHandler
- [ ] Add unit tests

#### Implementation (AuthHttpClient.cs)
```csharp
public class AuthHttpClient : DelegatingHandler
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // ... auth logic
        
        var response = await base.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            byte[] buffer = BytePool.Rent(4096);
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(
                    buffer.AsSpan(0, bytesRead),
                    MercadoBitcoinJsonSerializerContext.Default.ErrorResponse);
                
                throw new MercadoBitcoinApiException(errorResponse);
            }
            finally
            {
                BytePool.Return(buffer);
            }
        }
        
        return response;
    }
}
```

#### Validation
- Run tests
- Measure allocations (expect -60-80%)
- Benchmark HTTP requests

---

### Week 3, Day 3-4: MemoryPool Implementation

#### Tasks
- [ ] Identify large allocations (>85KB)
- [ ] Create MemoryPoolManager
- [ ] Refactor bulk operations
- [ ] Add tests

#### Implementation
```csharp
public class MemoryPoolManager
{
    public static async Task<List<T>> ReadLargeListAsync<T>(
        Stream stream,
        JsonTypeInfo<List<T>> typeInfo,
        CancellationToken ct = default)
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(65536);
        Memory<byte> buffer = memoryOwner.Memory;
        
        int totalRead = 0;
        int read;
        
        while ((read = await stream.ReadAsync(buffer.Slice(totalRead), ct)) > 0)
        {
            totalRead += read;
        }
        
        return JsonSerializer.Deserialize(buffer.Span.Slice(0, totalRead), typeInfo);
    }
}
```

---

### Week 3, Day 5 + Week 4, Day 1: ObjectPool Implementation

#### Tasks
- [ ] Create ObjectPool<ErrorResponse>
- [ ] Create ObjectPool<StringBuilder>
- [ ] Create ObjectPool<List<T>>
- [ ] Refactor code to use pools
- [ ] Add tests

#### Implementation
```csharp
public class ErrorResponsePool
{
    private static readonly ObjectPool<ErrorResponse> Pool = 
        ObjectPool.Create<ErrorResponse>();
    
    public static ErrorResponse Rent()
    {
        var response = Pool.Get();
        response.Reset();
        return response;
    }
    
    public static void Return(ErrorResponse response)
    {
        Pool.Return(response);
    }
}
```

---

### Week 4, Day 2-3: Validation and Benchmarks

#### Tasks
- [ ] Run test suite
- [ ] Run memory benchmarks
- [ ] Analyze allocations with dotMemory
- [ ] Compare with baseline
- [ ] Document gains

#### Success Criteria
- ✅ -60-80% heap allocations
- ✅ -50-70% Gen0 collections
- ✅ All tests pass

---

### Week 4, Day 4-5: Documentation and Checkpoint

#### Tasks
- [ ] Document pooling patterns
- [ ] Update README
- [ ] Commit and tag checkpoint-phase3

#### Checkpoint
```bash
git commit -m "feat: implement memory pooling (ArrayPool, MemoryPool, ObjectPool)"
git tag checkpoint-phase3
```

---

## 6. Phase 4: Performance Optimizations

**Duration**: Weeks 5-6 (10 business days)  
**Objective**: Implement CPU and I/O optimizations

### Week 5, Day 1-2: Rate Limiter Migration

#### Tasks
- [ ] Replace AsyncRateLimiter with TokenBucketRateLimiter
- [ ] Refactor MercadoBitcoinClient
- [ ] Add rate limiting metrics
- [ ] Unit tests
- [ ] Benchmarks

#### Implementation
```csharp
public class MercadoBitcoinClient
{
    private readonly TokenBucketRateLimiter _rateLimiter;
    
    public MercadoBitcoinClient(IOptions<MercadoBitcoinClientOptions> options)
    {
        var opts = options.Value;
        
        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 10,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = opts.RequestsPerSecond,
            AutoReplenishment = true
        });
    }
    
    private async Task<T> ExecuteWithRateLimitAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        using var lease = await _rateLimiter.AcquireAsync(1, ct);
        
        if (!lease.IsAcquired)
            throw new RateLimitException("Rate limit exceeded");
        
        return await action();
    }
}
```

---

### Week 5, Day 3-4: HTTP/3 and SocketsHttpHandler

#### Tasks
- [ ] Configure HTTP/3 support
- [ ] Optimize SocketsHttpHandler
- [ ] Configure connection pooling
- [ ] Tests
- [ ] Benchmarks

---

### Week 5, Day 5 + Week 6, Day 1-3: Span<T> and stackalloc

#### Tasks
- [ ] Identify hot paths with string operations
- [ ] Refactor to use Span<T>
- [ ] Implement stackalloc for small buffers
- [ ] ValueStringBuilder for ToString()
- [ ] Tests and benchmarks

---

### Week 6, Day 4-5: Polly V4 Migration

#### Tasks
- [ ] Migrate to Polly V4 ResiliencePipeline
- [ ] Replace manual circuit breaker
- [ ] Configure retry strategies
- [ ] Tests
- [ ] Validation

---

## 7. Phase 5: Models and Structures

**Duration**: Week 7 (5 business days)  
**Objective**: Optimize data types

### Day 1-3: CandleData class → struct

**Breaking Change Risk**: HIGH

#### Tasks
- [ ] Convert CandleData to readonly struct
- [ ] ReadOnlyMemory<char> for strings
- [ ] Inline computed properties
- [ ] Update JsonSerializer
- [ ] Extensive tests

---

### Day 4: String Optimizations

#### Tasks
- [ ] Implement ReadOnlyMemory<char> patterns
- [ ] String interning for symbols
- [ ] Refactor parsing

---

### Day 5: Inline Arrays

#### Tasks
- [ ] Identify fixed-size buffers
- [ ] Implement inline arrays (C# 14)
- [ ] Tests

---

## 8. Phase 6: Testing and Validation

**Duration**: Week 8 (5 business days)

### Day 1-2: Regression Tests

- [ ] Run complete suite
- [ ] Integration tests
- [ ] Smoke tests

### Day 3-4: Final Benchmarks

- [ ] Run all benchmarks
- [ ] Compare with baseline
- [ ] Document gains

### Day 5: Load Testing

- [ ] k6 load tests
- [ ] Stress testing
- [ ] Soak testing

---

## 9. Phase 7: Documentation and Release

**Duration**: Week 9 (5 business days)

### Day 1-2: Migration Guide

- [ ] Write migration guide v3 → v4
- [ ] Document breaking changes (if any)
- [ ] Code examples

### Day 3: Release Notes

- [ ] Complete changelog
- [ ] Performance metrics
- [ ] Known issues

### Day 4: Documentation Updates

- [ ] Update README
- [ ] Update samples
- [ ] API documentation

### Day 5: Release v4.0.0

- [ ] Create release on GitHub
- [ ] Publish to NuGet
- [ ] Announce release

---

## 10. Checkpoints and Rollback

### Mandatory Checkpoints

| Checkpoint | When | Git Tag | Rollback Trigger |
|------------|------|---------|------------------|
| **CP1** | End Phase 1 | `checkpoint-phase1` | N/A (analysis only) |
| **CP2** | End Phase 2 | `checkpoint-phase2` | Build failure, test failure |
| **CP3** | End Phase 3 | `checkpoint-phase3` | Performance regression >10% |
| **CP4** | End Phase 4 | `checkpoint-phase4` | Stability issues |
| **CP5** | End Phase 5 | `checkpoint-phase5` | Unacceptable breaking changes |
| **CP6** | End Phase 6 | `checkpoint-phase6` | Failed load tests |
| **CP7** | Release | `v4.0.0` | Critical bugs |

### Rollback Plan

#### Trigger Conditions
1. **Build Failure**: Code doesn't compile
2. **Test Failure**: >5% tests failing
3. **Performance Regression**: >10% worse vs baseline
4. **Stability Issues**: Crashes, deadlocks, memory leaks
5. **Breaking Changes**: API incompatibilities

#### Rollback Procedure
```bash
# 1. Identify last stable checkpoint
git tag --list "checkpoint-*"

# 2. Revert to checkpoint
git reset --hard checkpoint-phaseX

# 3. Create investigation branch
git checkout -b investigate/phase-failure

# 4. Analyze root cause
# ...

# 5. Fix and retry
```

---

## 11. Risk Management

### Risk Matrix

| Risk | Probability | Impact | Mitigation |
|-------|-------------|---------|------------|
| **Breaking Changes in API** | Medium | High | Design reviews, API analyzer |
| **Performance Regression** | Low | High | Continuous benchmarks |
| **Production Bugs** | Medium | Critical | Extensive testing, canary deployment |
| **Incompatible Dependencies** | Low | Medium | Prior verification |
| **Development Overhead** | High | Medium | 20% buffer in timeline |

### Mitigation Plan

#### Breaking Changes
- ✅ Use API Analyzer (Microsoft.CodeAnalysis.PublicApiAnalyzers)
- ✅ Review each public API change
- ✅ Binary compatibility tests

#### Performance Regression
- ✅ Benchmarks after each change
- ✅ Threshold: -10% is blocker
- ✅ Continuous profiling

#### Production Bugs
- ✅ Canary deployment (5% → 25% → 100%)
- ✅ Feature flags
- ✅ 5-minute rollback plan

---

## 12. Communication and Stakeholders

### Stakeholders

| Group | Interest | Communication |
|-------|----------|---------------|
| **Library Users** | API stability, performance | Release notes, migration guide |
| **Contributors** | Code quality, architecture | Architecture docs, code reviews |
| **Internal Team** | Timeline, risks | Weekly status reports |

### Weekly Communication

**Format**: Status report via GitHub discussion

**Template**:
```markdown
## Weekly Status Report - Week X

### ✅ Completed
- [ ] Task 1
- [ ] Task 2

### 🚧 In Progress
- [ ] Task 3

### ⚠️ Blocked
- [ ] Issue with dependency X

### 📊 Metrics
- Throughput: +X%
- Memory: -Y%
- Tests: N passing

### 🎯 Next Week
- [ ] Task 4
```

---

## ✅ Conclusion

This roadmap details **9 weeks of structured refactoring** to migrate MercadoBitcoin.Client v3.0.0 to v4.0.0 with all .NET 10 optimizations.

### Next Documents

- **04-MIGRACAO-NET10.md**: Technical details of .NET 10 migration
- **05-ESTRUTURA-PASTAS.md**: New folder organization
- **06-MEMORY-POOLING.md**: Detailed pooling implementation
- **... (until 22)**

---

**Document**: 03-REFACTORING-ROADMAP.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: ✅ Complete  
**Next**: [04-MIGRACAO-NET10.md](04-MIGRACAO-NET10.md)
