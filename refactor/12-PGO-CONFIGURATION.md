# 12. PGO CONFIGURATION (PROFILE-GUIDED OPTIMIZATION)

## 📋 Index

1. [Objective](#objective)  
2. [PGO Overview in .NET 10](#pgo-overview-in-net-10)  
3. [Dynamic PGO Configuration](#dynamic-pgo-configuration)  
4. [Static PGO + AOT Configuration](#static-pgo--aot-configuration)  
5. [Training Workload Strategy](#training-workload-strategy)  
6. [Integration with MercadoBitcoin.Client](#integration-with-mercadobitcoinclient)  
7. [Performance Measurements](#performance-measurements)  
8. [Risks and Considerations](#risks-and-considerations)  
9. [Implementation Checklist](#implementation-checklist)

---

## 1. Objective

Configure **Profile-Guided Optimization (PGO)** for the library, leveraging:

- Dynamic PGO (runtime).  
- Static PGO (AOT + profile).

Focusing on:

- 🚀 15–30% throughput improvement on hot paths.  
- 💡 Better inlining and code layout based on real usage.

---

## 2. PGO Overview in .NET 10

### 2.1. Dynamic PGO

- Enabled by default starting from .NET 8.  
- Collects execution profiles at runtime.  
- Recompiles hot methods with profile‑guided optimizations.

### 2.2. Static PGO

- Collects profiles from an instrumented run.  
- Uses the profile to compile AOT with optimized layout and inlining.

---

## 3. Dynamic PGO Configuration

### 3.1. In the .csproj

```xml
<PropertyGroup>
  <TieredCompilation>true</TieredCompilation>
  <TieredCompilationQuickJit>true</TieredCompilationQuickJit>
  <TieredCompilationQuickJitForLoops>false</TieredCompilationQuickJitForLoops>
  <DynamicPGO>true</DynamicPGO>
</PropertyGroup>
```

### 3.2. runtimeconfig.json (optional)

```json
{
  "runtimeOptions": {
    "configProperties": {
      "System.Runtime.TieredCompilation": true,
      "System.Runtime.TieredCompilation.QuickJit": true,
      "System.Runtime.TieredCompilation.QuickJitForLoops": false,
      "System.Runtime.TieredPGO": true
    }
  }
}
```

---

## 4. Static PGO + AOT Configuration

### 4.1. csproj Properties

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IsAotCompatible>true</IsAotCompatible>
  <IlcGenerateCompleteTypesMetadata>true</IlcGenerateCompleteTypesMetadata>
  <IlcInstructionSet>native</IlcInstructionSet>
  <StripSymbols>true</StripSymbols>
  <OptimizationPreference>Speed</OptimizationPreference>
  <EnablePGO>true</EnablePGO>
</PropertyGroup>
```

### 4.2. Build Flow with Static PGO

1. Publish an instrumented build:

```bash
 dotnet publish -c Release -r win-x64 \
   /p:PublishAot=false \
   /p:EnablePGO=true \
   /p:IlcGenerateCompleteTypesMetadata=true
```

2. Run the training workload:

```bash
 ./bin/Release/net10.0/win-x64/MercadoBitcoin.Client.Sample.exe --training
```

3. Publish the AOT build using the generated profile:

```bash
 dotnet publish -c Release -r win-x64 \
   /p:PublishAot=true \
   /p:EnablePGO=true
```

---

## 5. Training Workload Strategy

### 5.1. Training Mode Goal

- Exercise **real hot paths** of the library, including:
  - Public endpoints (tickers, order book).  
  - Private endpoints (balance, orders).

### 5.2. Training Mode Implementation

Create a sample executable (in `samples/`) with a `--training` mode:

- Execute:
  - `GetTicker` for multiple symbols.  
  - `GetOrderBook`.  
  - `GetBalance`.  
  - Order creation/cancellation in a test environment.

### 5.3. Minimum Desired Coverage

- Ensure all public methods of `MercadoBitcoinClient` are invoked at least once during training.

---

## 6. Integration with MercadoBitcoin.Client

### 6.1. PGO Transparent to the User

- The end user does not need to know PGO is enabled.  
- PGO configuration is the responsibility of the host project that consumes the library.

### 6.2. Documentation Recommendation

- In the README, suggest that high‑performance applications using the library:  
  - Enable Dynamic PGO.  
  - Consider Static PGO + AOT in extreme latency scenarios.

---

## 7. Performance Measurements

### 7.1. Scenarios

- Compare builds with and without PGO, measuring:
  - Startup time.  
  - Throughput of critical endpoints.  
  - P50/P99 latency.

### 7.2. Tools

- `BenchmarkDotNet` for micro‑benchmarks (see 18-BENCHMARKS.md).  
- `dotnet-counters` and `dotnet-trace` to analyze JIT/PGO.

---

## 8. Risks and Considerations

### 8.1. Non‑Representative Training Workload

- If training does not reflect real usage, PGO may optimize rarely used paths and hurt others.

### 8.2. Build Time

- Static PGO + AOT increases build time.

### 8.3. Debugging

- AOT/PGO builds are harder to debug.

---

## 9. Implementation Checklist

- [ ] Enable Dynamic PGO in the host project (application using the library).  
- [ ] Document PGO recommendations in the library README.  
- [ ] Create a sample with training mode for Static PGO.  
- [ ] Run an experiment with Static PGO + AOT to validate gains.

---

**Document**: 12-PGO-CONFIGURATION.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: ✅ Concept Defined / Implementation in host project

