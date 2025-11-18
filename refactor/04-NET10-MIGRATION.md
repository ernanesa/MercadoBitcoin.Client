# 04. NET10 MIGRATION

## 📋 Index

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Step-by-Step Migration](#step-by-step-migration)
4. [Dependencies Update](#dependencies-update)
5. [Breaking Changes](#breaking-changes)
6. [Build Configuration](#build-configuration)
7. [Validation](#validation)
8. [Troubleshooting](#troubleshooting)

---

## 1. Overview

### Objective

Migrate **MercadoBitcoin.Client** from **.NET 9** to **.NET 10** while maintaining 100% API compatibility.

### Timeline

**Duration**: 1-2 weeks  
**Effort**: Medium  
**Risk**: Medium

### Expected Gains

| Metric | .NET 9 | .NET 10 | Gain |
|--------|--------|---------|------|
| **JIT Performance** | Baseline | +15-25% | JIT improvements |
| **GC Efficiency** | Baseline | +10-20% | ARM64 write-barriers |
| **Startup Time** | Baseline | +10-15% | QuickJit improvements |
| **New APIs** | - | ✅ | System.Threading.RateLimiting |

---

## 2. Prerequisites

### 2.1. Install .NET 10 SDK

#### Windows
```powershell
# Via winget
winget install Microsoft.DotNet.SDK.10

# Or download from:
# https://dotnet.microsoft.com/download/dotnet/10.0
```

#### macOS
```bash
brew install dotnet@10
```

#### Linux (Ubuntu)
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```

### 2.2. Verify Installation

```bash
dotnet --version
# Expected output: 10.0.x

dotnet --list-sdks
# Expected output: 10.0.xxx [C:\Program Files\dotnet\sdk]

dotnet --list-runtimes
# Expected output:
# Microsoft.NETCore.App 10.0.x [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
```

### 2.3. Update Visual Studio / Rider

- **Visual Studio 2025** (17.14+) required
- **JetBrains Rider 2025.1+** required

```bash
# Check Visual Studio version
& "C:\Program Files\Microsoft Visual Studio\2025\Enterprise\Common7\IDE\devenv.exe" /?
```

---

## 3. Step-by-Step Migration

### Step 1: Create Working Branch

```bash
git checkout main
git pull origin main
git checkout -b refactor/net10-migration
```

---

### Step 2: Update TargetFramework

#### src/MercadoBitcoin.Client/MercadoBitcoin.Client.csproj

```xml
<!-- BEFORE -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Version>3.0.0</Version>
  </PropertyGroup>
</Project>

<!-- AFTER -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Version>4.0.0-alpha.1</Version>
  </PropertyGroup>
</Project>
```

---

### Step 3: Update Tests

#### tests/MercadoBitcoin.Client.ComprehensiveTests/MercadoBitcoin.Client.ComprehensiveTests.csproj

```xml
<!-- BEFORE -->
<TargetFramework>net9.0</TargetFramework>

<!-- AFTER -->
<TargetFramework>net10.0</TargetFramework>
```

---

### Step 4: Update Samples

#### samples/AuthBalanceConsole/AuthBalanceConsole.csproj

```xml
<!-- BEFORE -->
<TargetFramework>net9.0</TargetFramework>

<!-- AFTER -->
<TargetFramework>net10.0</TargetFramework>
```

---

### Step 5: Build Project

```bash
cd src/MercadoBitcoin.Client
dotnet build

# Expected output:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

#### Possible Warnings

```
warning CS8032: An instance of analyzer ... could not be created
```

**Solution**: Update analyzers to versions compatible with .NET 10.

---

### Step 6: Run Tests

```bash
cd tests/MercadoBitcoin.Client.ComprehensiveTests
dotnet test

# Expected output:
# Passed!  - Failed:     0, Passed:    64, Skipped:     0
```

---

## 4. Dependencies Update

### 4.1. Microsoft.Extensions.*

```bash
cd src/MercadoBitcoin.Client

dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.0
dotnet add package Microsoft.Extensions.Http --version 10.0.0
dotnet add package Microsoft.Extensions.Options --version 10.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 10.0.0
dotnet add package Microsoft.Extensions.Configuration.Abstractions --version 10.0.0
```

### 4.2. Polly

```bash
dotnet add package Polly --version 8.5.0
```

**Check Breaking Changes**: https://github.com/App-vNext/Polly/releases/tag/8.5.0

### 4.3. NSwag.MSBuild

```bash
# Check compatible version
dotnet list package --outdated

# Update if necessary
dotnet add package NSwag.MSBuild --version 14.5.0
```

### 4.4. Verify All Dependencies

```bash
dotnet list package
```

**Expected output**:
```
Project 'MercadoBitcoin.Client' has the following package references
   [net10.0]:
   Top-level Package                                             Requested   Resolved
   > Microsoft.Extensions.DependencyInjection.Abstractions       10.0.0      10.0.0
   > Microsoft.Extensions.Http                                   10.0.0      10.0.0
   > Microsoft.Extensions.Options                                10.0.0      10.0.0
   > Polly                                                       8.5.0       8.5.0
   > Polly.Extensions.Http                                       3.0.0       3.0.0
```

---

## 5. Breaking Changes

### 5.1. Identify Breaking Changes

```bash
# Use API Analyzer
dotnet add package Microsoft.CodeAnalysis.PublicApiAnalyzers --version 3.11.0
```

#### PublicAPI.Shipped.txt

Create file `PublicAPI.Shipped.txt` in project root:

```
MercadoBitcoin.Client.MercadoBitcoinClient
MercadoBitcoin.Client.MercadoBitcoinClient.MercadoBitcoinClient(System.Net.Http.HttpClient httpClient, MercadoBitcoin.Client.Http.AuthHttpClient authHandler, Microsoft.Extensions.Options.IOptions<MercadoBitcoin.Client.Configuration.MercadoBitcoinClientOptions> options) -> void
MercadoBitcoin.Client.MercadoBitcoinClient.GetTickerAsync(string symbol, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> System.Threading.Tasks.Task<MercadoBitcoin.Client.Generated.Ticker>
# ... all public APIs
```

### 5.2. Known Breaking Changes

#### .NET 10 Breaking Changes

| API | Change | Impact | Action |
|-----|--------|--------|--------|
| `HttpClient` | HTTP/3 default (if available) | None | Transparent |
| `JsonSerializer` | Performance improvements | None | Only gains |
| `Regex` | Source generators mandatory | None | Already used |

**Reference**: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0

### 5.3. Polly 8.5.0 Breaking Changes

```csharp
// BEFORE (Polly 8.2.0)
var policy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(/* ... */);

// AFTER (Polly 8.5.0 - no changes!)
// API compatible, only performance improvements
```

**Conclusion**: No breaking changes in Polly 8.5.0.

---

## 6. Build Configuration

### 6.1. Add Optimization Configurations

#### MercadoBitcoin.Client.csproj

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>14.0</LangVersion>
  <Version>4.0.0-alpha.1</Version>
  <IsAotCompatible>true</IsAotCompatible>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  
  <!-- Optimizations -->
  <Optimize>true</Optimize>
  
  <!-- Tiered Compilation -->
  <TieredCompilation>true</TieredCompilation>
  <TieredCompilationQuickJit>true</TieredCompilationQuickJit>
  <TieredCompilationQuickJitForLoops>false</TieredCompilationQuickJitForLoops>
  
  <!-- Dynamic PGO -->
  <DynamicPGO>true</DynamicPGO>
  
  <!-- GC Configuration -->
  <ServerGarbageCollection>true</ServerGarbageCollection>
  <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
  <RetainVMGarbageCollection>true</RetainVMGarbageCollection>
  
  <!-- Trimming (for AOT) -->
  <PublishTrimmed>false</PublishTrimmed>
  <TrimMode>full</TrimMode>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
</PropertyGroup>
```

### 6.2. Add runtimeconfig.template.json

Create file `runtimeconfig.template.json` in project root:

```json
{
  "configProperties": {
    "System.Runtime.TieredCompilation": true,
    "System.Runtime.TieredCompilation.QuickJit": true,
    "System.Runtime.TieredCompilation.QuickJitForLoops": false,
    "System.Runtime.TieredPGO": true,
    "System.GC.Server": true,
    "System.GC.Concurrent": true,
    "System.GC.RetainVM": true,
    "System.GC.HeapCount": 8,
    "System.GC.HeapAffinitizeMask": "0xFF"
  }
}
```

### 6.3. Update global.json

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
```

---

## 7. Validation

### 7.1. Compilation

```bash
# Clean build
dotnet clean
dotnet restore
dotnet build --configuration Release

# Check warnings
dotnet build --configuration Release /warnaserror
```

### 7.2. Unit Tests

```bash
dotnet test --configuration Release --logger "console;verbosity=detailed"
```

**Success Criterion**: 100% tests pass (64/64).

### 7.3. Integration Tests

```bash
cd tests/MercadoBitcoin.Client.ComprehensiveTests

# Run tests requiring credentials
export MB_API_KEY="your-api-key"
export MB_API_SECRET="your-api-secret"

dotnet test --filter "Category=Integration"
```

### 7.4. Smoke Tests

```bash
cd samples/AuthBalanceConsole
dotnet run

# Verify output:
# ✅ Connected to Mercado Bitcoin API
# ✅ Balance retrieved successfully
```

### 7.5. Benchmarks (Baseline)

```bash
cd tests/Benchmarks
dotnet run -c Release --framework net10.0

# Compare with baseline .NET 9
```

---

## 8. Troubleshooting

### 8.1. Error: "The current .NET SDK does not support targeting .NET 10.0"

**Cause**: .NET 10 SDK not installed or PATH not configured.

**Solution**:
```bash
# Check PATH
echo $env:PATH  # Windows PowerShell
echo $PATH      # Linux/macOS

# Reinstall SDK
winget install Microsoft.DotNet.SDK.10
```

---

### 8.2. Error: "Package X is not compatible with net10.0"

**Cause**: NuGet dependency doesn't support .NET 10.

**Solution**:
```bash
# Check available versions
dotnet list package --outdated

# Update to compatible version
dotnet add package X --version Y.Y.Y

# If no compatible version exists, use TargetFrameworks
<TargetFrameworks>net9.0;net10.0</TargetFrameworks>
```

---

### 8.3. Error: "CS0234: The type or namespace 'RateLimiting' does not exist"

**Cause**: `System.Threading.RateLimiting` is new in .NET 10.

**Solution**:
```csharp
#if NET10_0_OR_GREATER
using System.Threading.RateLimiting;
#endif
```

---

### 8.4. Tests Failing After Migration

**Cause**: Behavior changed in .NET 10.

**Solution**:
1. Isolate failing test
2. Compare behavior .NET 9 vs .NET 10
3. Adjust expectations or code

```bash
# Run specific test
dotnet test --filter "FullyQualifiedName=MercadoBitcoin.Client.Tests.RateLimiterTests.ShouldLimitRequests"
```

---

### 8.5. Performance Regression

**Cause**: Unoptimized configuration or code.

**Symptoms**:
```bash
# Benchmark shows degradation
| Method          | Mean       | Error     | StdDev    | Gen 0  | Allocated |
|---------------- |-----------:|----------:|----------:|-------:|----------:|
| GetTicker_Net9  | 10.23 ms   | 0.201 ms  | 0.188 ms  | 62.5   | 256 KB    |
| GetTicker_Net10 | 12.45 ms   | 0.234 ms  | 0.219 ms  | 93.7   | 384 KB    |  ❌ WORSE!
```

**Solution**:
1. Check PGO/GC configurations
2. Run profiler (dotTrace)
3. Identify hot paths that degraded
4. Apply specific optimizations

---

## 9. Final Checklist

### ✅ Complete Migration

- [ ] .NET 10 SDK installed
- [ ] TargetFramework updated to net10.0
- [ ] LangVersion updated to 14.0
- [ ] Dependencies updated to version 10.x
- [ ] PGO configurations added
- [ ] GC configurations added
- [ ] Build without errors
- [ ] Build without warnings
- [ ] 100% tests pass
- [ ] Benchmarks executed (baseline)
- [ ] Smoke tests pass
- [ ] Documentation updated
- [ ] Commit and push

### 🎯 Success Criteria

| Criterion | Status | Note |
|-----------|--------|------|
| **Build** | ✅ | Zero errors, zero warnings |
| **Tests** | ✅ | 64/64 passing |
| **Performance** | ✅ | No regression (baseline) |
| **API Compatibility** | ✅ | No breaking changes |

---

## 10. Next Steps

After successful migration:

1. ✅ **Commit and Tag**
   ```bash
   git add .
   git commit -m "chore: migrate to .NET 10"
   git tag checkpoint-net10-migration
   git push origin refactor/net10-migration --tags
   ```

2. ➡️ **Next Document**: [05-FOLDER-STRUCTURE.md](05-FOLDER-STRUCTURE.md)
   - Reorganize folder structure
   - Add folders for pooling, optimizations

3. ➡️ **Phase 3**: Memory Pooling
   - Implement ArrayPool
   - Implement MemoryPool
   - Implement ObjectPool

---

**Document**: 04-NET10-MIGRATION.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: ✅ Complete  
**Next**: [05-FOLDER-STRUCTURE.md](05-FOLDER-STRUCTURE.md)

