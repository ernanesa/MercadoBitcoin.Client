# 11. SOURCE GENERATORS AND JSON (System.Text.Json)

## 📋 Index

1. [Objective](#objective)  
2. [Current Serialization State](#current-serialization-state)  
3. [Strategy with JsonSourceGenerator](#strategy-with-jsonsourcegenerator)  
4. [Serialization Context Configuration](#serialization-context-configuration)  
5. [Integration with AOT (Native AOT)](#integration-with-aot-native-aot)  
6. [Usage Patterns in Code](#usage-patterns-in-code)  
7. [Support for Complex Types and Collections](#support-for-complex-types-and-collections)  
8. [Serialization Error Handling](#serialization-error-handling)  
9. [Metrics and Expected Benefits](#metrics-and-expected-benefits)  
10. [Implementation Checklist](#implementation-checklist)

---

## 1. Objective

Maximize JSON serialization/deserialization performance using **System.Text.Json Source Generators** on .NET 10, ensuring:

- 🚀 Lower startup time (no reflection at initialization).  
- 💾 Fewer allocations during serialization/deserialization.  
- 🔒 Compatibility with **Native AOT**.

---

## 2. Current Serialization State

### 2.1. Current File

- `MercadoBitcoinJsonSerializerContext.cs` already exists in `src/MercadoBitcoin.Client/`.  
- Defines `JsonSerializable` for several API types.

### 2.2. Improvement Points

- Ensure **all** relevant DTOs are included in the context.  
- Eliminate any remaining uses of `JsonSerializer.Serialize(object, options)` without context.  
- Standardize `JsonSerializer.Deserialize` usage with generated `JsonTypeInfo`.

---

## 3. Strategy with JsonSourceGenerator

### 3.1. JsonSourceGenerationOptions

Enforce a single configuration for the library:

- Property names in `camelCase`.  
- Ignore `null` in output.  
- No `WriteIndented` (better performance and smaller payloads).

```csharp
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization,
    UseStringEnumConverter = false)]
public partial class MercadoBitcoinJsonSerializerContext : JsonSerializerContext
{
}
```

### 3.2. JsonSerializable para Todos os Tipos

- Garantir que todos os modelos usados pela API estejam anotados com `[JsonSerializable]`:

```csharp
[JsonSerializable(typeof(Ticker))]
[JsonSerializable(typeof(Balance))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Ticker>))]
[JsonSerializable(typeof(List<Order>))]
[JsonSerializable(typeof(Dictionary<string, decimal>))]
// ... etc
public partial class MercadoBitcoinJsonSerializerContext : JsonSerializerContext
{
}
```

---

## 4. Serialization Context Configuration

### 4.1. JsonSerializerOptions Creation

```csharp
internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
    {
        TypeInfoResolver = MercadoBitcoinJsonSerializerContext.Default,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.Strict,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
```

### 4.2. Centralized Usage

- All serialization/deserialization must use the generated context:

```csharp
var ticker = JsonSerializer.Deserialize(
    span,
    MercadoBitcoinJsonSerializerContext.Default.Ticker);

string json = JsonSerializer.Serialize(
    request,
    MercadoBitcoinJsonSerializerContext.Default.CreateOrderRequest);
```

---

## 5. Integration with AOT (Native AOT)

### 5.1. DynamicDependency

To ensure the linker/AOT does not trim required types:

```csharp
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MercadoBitcoinJsonSerializerContext))]
internal static partial class SerializationBootstrap
{
}
```

### 5.2. Propriedades em csproj

No `.csproj`:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

---

## 6. Usage Patterns in Code

### 6.1. Never Use Reflection-Based APIs

Avoid:

```csharp
JsonSerializer.Serialize(obj, options); // sem contexto
JsonSerializer.Deserialize<T>(json, options); // sem contexto
```

Always prefer overloads with `JsonTypeInfo<T>`:

```csharp
JsonSerializer.Serialize(obj, MercadoBitcoinJsonSerializerContext.Default.SomeType);
JsonSerializer.Deserialize(jsonSpan, MercadoBitcoinJsonSerializerContext.Default.SomeType);
```

### 6.2. Integration with `ArrayPoolManager`

- Receive `ReadOnlySpan<byte>` from a pooled buffer and deserialize directly.

---

## 7. Support for Complex Types and Collections

### 7.1. Collections

- `List<T>`, `Dictionary<string, T>`, arrays, etc., should have explicit `[JsonSerializable]` entries for optimal performance.

### 7.2. Enums

- Evaluate custom converters if needed, but prefer numeric representation for performance when possible (evaluating impact on the public API).

---

## 8. Serialization Error Handling

### 8.1. JsonException

- Catch `JsonException` and enrich with relevant information (without leaking sensitive data):

```csharp
try
{
    var result = JsonSerializer.Deserialize(
        span,
        MercadoBitcoinJsonSerializerContext.Default.SomeResponse);
}
catch (JsonException ex)
{
    throw new MercadoBitcoinApiException("Error deserializing JSON response.", ex);
}
```

### 8.2. Compatibility with API Changes

- If the API returns additional fields, the Source Generator handles them well (as long as required properties remain).

---

## 9. Metrics and Expected Benefits

### 9.1. Benefits

- 40–60% reduction in serialization/deserialization time.  
- Lower startup time (no runtime reflection to build metadata).  
- Fewer allocations related to internal metadata caches.

### 9.2. How to Measure

- Benchmarks comparing:
  - `JsonSerializer` with default options.  
  - vs. `JsonSerializer` using the generated `JsonSerializerContext`.

---

## 10. Implementation Checklist

- [ ] Review `MercadoBitcoinJsonSerializerContext.cs` and ensure all required DTOs are annotated.  
- [ ] Create centralized `JsonOptions` with `TypeInfoResolver = MercadoBitcoinJsonSerializerContext.Default`.  
- [ ] Refactor `JsonSerializer.Serialize/Deserialize` calls to use overloads with `JsonTypeInfo<T>`.  
- [ ] Add `DynamicDependency` to guarantee AOT compatibility.  
- [ ] Create benchmarks comparing before/after.

---

**Document**: 11-SOURCE-GENERATORS.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: 🚧 Planned / Awaiting Implementation  
**Next**: [12-PGO-CONFIGURATION.md](12-PGO-CONFIGURATION.md)
