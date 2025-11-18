# 📋 REFACTORING INDEX - MercadoBitcoin.Client

## 🎯 Refactoring Objective

Migrate the project from **.NET 9 to .NET 10**, applying **all advanced optimizations** documented in the planning, maintaining **100% compatibility** with the current API and significantly improving performance and memory efficiency.

---

## 📚 Planning Documents

### Reference Documents
- [00-QUICK-REFERENCE-GUIDE.md](../plan/00-QUICK-REFERENCE-GUIDE.md)
- [01-ARCHITECTURE-OVERVIEW.md](../plan/01-ARCHITECTURE-OVERVIEW.md)
- [06-PERFORMANCE-AND-OPTIMIZATION.md](../plan/06-PERFORMANCE-AND-OPTIMIZATION.md)
- [11-FOLDER-STRUCTURE-AND-NET10.md](../plan/11-FOLDER-STRUCTURE-AND-NET10.md)
- [12-ADVANCED-OPTIMIZATIONS-NET10-CSHARP14.md](../plan/12-ADVANCED-OPTIMIZATIONS-NET10-CSHARP14.md)

---

## 📂 Refactoring Documents (in this folder)

### Phase 1: Analysis and Planning
- **[01-CODE-ANALYSIS.md](01-CODE-ANALYSIS.md)** - Complete analysis of existing code
- **[02-GAPS-AND-IMPROVEMENTS.md](02-GAPS-AND-IMPROVEMENTS.md)** - Identification of gaps between current and planned
- **[03-REFACTORING-ROADMAP.md](03-REFACTORING-ROADMAP.md)** - Detailed refactoring roadmap

### Phase 2: Configuration and Infrastructure
- **[04-NET10-MIGRATION.md](04-NET10-MIGRATION.md)** - .NET 9 → .NET 10 migration
- **[05-FOLDER-STRUCTURE.md](05-FOLDER-STRUCTURE.md)** - Folder structure reorganization
- **[06-PROJECT-CONFIGURATION.md](06-PROJECT-CONFIGURATION.md)** - .csproj, runtimeconfig.json configuration

### Phase 3: Memory Optimizations
- **[06-MEMORY-POOLING.md](06-MEMORY-POOLING.md)** - ArrayPool, MemoryPool, ObjectPool implementation
- **[08-SPAN-MEMORY.md](08-SPAN-MEMORY.md)** - Refactoring to Span<T> and Memory<T>
- **[09-STACK-ALLOCATION.md](09-STACK-ALLOCATION.md)** - Stack allocation leverage

### Phase 4: Performance Optimizations
- **[10-HTTP-OPTIMIZATION.md](10-HTTP-OPTIMIZATION.md)** - SocketsHttpHandler optimization, HTTP/3
- **[10-RATE-LIMITING.md](10-RATE-LIMITING.md)** - Migration to System.Threading.RateLimiting
- **[11-SOURCE-GENERATORS.md](11-SOURCE-GENERATORS.md)** - Source Generators expansion
- **[12-PGO-CONFIGURATION.md](12-PGO-CONFIGURATION.md)** - Dynamic PGO configuration

### Phase 5: Models and Structures
- **[13-CANDLEDATA-STRUCT.md](13-CANDLEDATA-STRUCT.md)** - CandleData struct optimization
- **[14-VALUE-TYPES.md](14-VALUE-TYPES.md)** - Class to struct conversion (where appropriate)
- **[15-INLINE-ARRAYS.md](15-INLINE-ARRAYS.md)** - Inline Arrays implementation
- **[16-REF-STRUCTS.md](16-REF-STRUCTS.md)** - Custom ref structs creation

### Phase 6: Testing and Validation
- **[17-TESTING-STRATEGY.md](17-TESTING-STRATEGY.md)** - Performance and functional testing strategy
- **[18-BENCHMARKS.md](18-BENCHMARKS.md)** - BenchmarkDotNet setup
- **[19-VALIDATION.md](19-VALIDATION.md)** - Validation and acceptance criteria

### Phase 7: Documentation and Deployment
- **[20-BREAKING-CHANGES.md](20-BREAKING-CHANGES.md)** - Breaking changes documentation
- **[21-MIGRATION-GUIDE.md](21-MIGRATION-GUIDE.md)** - Migration guide for users
- **[22-RELEASE-NOTES.md](22-RELEASE-NOTES.md)** - Release notes v4.0.0

---

## 🎯 Success Metrics

### Performance Targets

| Metric | Baseline (v3.0.0) | Target (v4.0.0) | Improvement |
|---------|-------------------|-----------------|-------------|
| **Startup Time** | ~800ms | ~400ms | **50%** ⬇️ |
| **Memory Usage** | ~150 MB | ~80 MB | **47%** ⬇️ |
| **Throughput** | ~10k req/s | ~15k req/s | **50%** ⬆️ |
| **P99 Latency** | ~100ms | ~30ms | **70%** ⬇️ |
| **Heap Allocations** | ~100 MB/s | ~30 MB/s | **70%** ⬇️ |
| **GC Gen0** | ~100/min | ~30/min | **70%** ⬇️ |
| **GC Pause** | ~50ms | ~10ms | **80%** ⬇️ |

### Quality Targets

| Criterion | Target |
|----------|--------|
| **Test Coverage** | ≥ 90% |
| **API Compatibility** | 100% |
| **AOT Warnings** | 0 |
| **Trim Warnings** | 0 |
| **Code Smells** | 0 |
| **Technical Debt** | < 5% |

---

## 📅 Estimated Timeline

### Overview

| Phase | Duration | Deliverables |
|------|---------|--------------|
| **Phase 1** - Analysis | 1 week | Documents 01-03 |
| **Phase 2** - Infrastructure | 1 week | .NET 10 migration, structure |
| **Phase 3** - Memory | 2 weeks | Pooling, Span/Memory |
| **Phase 4** - Performance | 2 weeks | HTTP, Rate Limiting, PGO |
| **Phase 5** - Models | 1 week | Value types, ref structs |
| **Phase 6** - Testing | 1 week | Benchmarks, validation |
| **Phase 7** - Release | 1 week | Docs, deployment |
| **TOTAL** | **9 weeks** | v4.0.0 Release |

### Detailed Schedule

#### Week 1: Analysis and Planning
- [ ] Complete code analysis
- [ ] Gap identification
- [ ] Detailed roadmap definition
- [ ] .NET 10 environment setup

#### Week 2: Base Migration
- [ ] .NET 9 → .NET 10 migration
- [ ] Dependencies update
- [ ] Folder structure reorganization
- [ ] .csproj and runtimeconfig configuration

#### Week 3-4: Memory Optimizations
- [ ] ArrayPool implementation for buffers
- [ ] MemoryPool implementation
- [ ] Custom ObjectPool implementation
- [ ] Span<T> and Memory<T> refactoring
- [ ] Stack allocation leverage

#### Week 5-6: Performance Optimizations
- [ ] SocketsHttpHandler optimization
- [ ] HTTP/3 implementation
- [ ] Migration to System.Threading.RateLimiting
- [ ] Source Generators expansion
- [ ] Dynamic PGO configuration
- [ ] [MethodImpl] attributes application

#### Week 7: Models and Structures
- [ ] Class to struct conversion
- [ ] Inline Arrays implementation
- [ ] Custom ref structs creation
- [ ] CandleData optimization
- [ ] All models review

#### Week 8: Testing and Validation
- [ ] BenchmarkDotNet setup
- [ ] Baseline benchmarks creation
- [ ] Performance tests execution
- [ ] Allocations analysis (dotMemory)
- [ ] CPU profile (dotTrace/PerfView)
- [ ] Metrics validation

#### Week 9: Documentation and Release
- [ ] Breaking changes documentation
- [ ] Migration guide v3 → v4
- [ ] Complete release notes
- [ ] README and CHANGELOG update
- [ ] NuGet package preparation
- [ ] Deployment and publication

---

## ⚠️ Risks and Mitigations

### Identified Risks

| Risk | Probability | Impact | Mitigation |
|-------|--------------|---------|------------|
| **Breaking Changes** | High | High | Keep public API compatible, create extension methods |
| **Performance Regression** | Medium | High | Continuous benchmarks, rollback plan |
| **Bugs in .NET 10** | Low | Medium | Extensive testing, feedback to Microsoft |
| **Development Overhead** | High | Medium | Incremental implementation, prioritization |
| **AOT Incompatibility** | Medium | Medium | Continuous validation, use of Source Generators |

### Mitigation Strategies

1. **Compatibility**: Keep public API 100% compatible (no public breaking changes)
2. **Testing**: Automated tests running in CI/CD
3. **Benchmarking**: Benchmarks on each PR, baseline comparison
4. **Incremental**: Implementation in separate branches, gradual merge
5. **Rollback**: Maintain v3.x in support branch for 6 months

---

## 🔧 Tools and Technologies

### Development
- **IDE**: Visual Studio 2025 / Rider 2025
- **.NET SDK**: 10.0.0 (GA November 2025)
- **Language**: C# 14

### Testing
- **Unit Tests**: xUnit
- **Benchmarking**: BenchmarkDotNet
- **Load Testing**: k6 / JMeter
- **Profiling**: dotTrace, dotMemory, PerfView

### CI/CD
- **Build**: GitHub Actions
- **Analysis**: SonarQube / CodeQL
- **Package**: NuGet.org

---

## 📞 Contacts and Responsibilities

### Roles

| Role | Responsible | Responsibilities |
|------|-------------|------------------|
| **Tech Lead** | Ernane Sa | Architectural decisions, code review |
| **Developer** | Ernane Sa | Implementation, testing |
| **QA** | Ernane Sa | Validation, performance testing |
| **DevOps** | Ernane Sa | CI/CD, deployment |

---

## 📖 How to Use This Index

1. **Read documents in order** (01 → 22)
2. **Implement phase by phase** (don't skip phases)
3. **Validate each phase** before proceeding
4. **Document deviations** from the plan in the corresponding document
5. **Update metrics** as progress is made

---

## 🔄 Progress Status

### Legend
- ⏳ **Pending**
- 🔄 **In Progress**
- ✅ **Complete**
- ⚠️ **Blocked**

### Phases

| Phase | Status | Progress | Start Date | End Date |
|------|--------|----------|------------|----------|
| **Phase 1** - Analysis | ⏳ | 0% | - | - |
| **Phase 2** - Infrastructure | ⏳ | 0% | - | - |
| **Phase 3** - Memory | ⏳ | 0% | - | - |
| **Phase 4** - Performance | ⏳ | 0% | - | - |
| **Phase 5** - Models | ⏳ | 0% | - | - |
| **Phase 6** - Testing | ⏳ | 0% | - | - |
| **Phase 7** - Release | ⏳ | 0% | - | - |

---

## 📝 Notes

- This index will be updated as the refactoring progresses
- Each document can be read independently, but they follow a logical sequence
- Documentation based on plans in `/plan` and .NET 10 best practices
- Goal: v4.0.0 with maximum performance and efficiency

---

**Last updated**: 2025-11-18  
**Version**: 1.0  
**Status**: In Planning
