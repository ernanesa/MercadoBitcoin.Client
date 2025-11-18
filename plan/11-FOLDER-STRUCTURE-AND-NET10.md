```markdown
# Folder Structure and .NET 10 Migration

> Date: 2025-11-18
> Current: .NET 9 + C# 13
> Target: .NET 10 + C# 14

This document describes the current repository layout and a suggested improved structure for the .NET 10 migration.

### Current Recommended Layout (summary)

```
src/
  MercadoBitcoin.Client/
    Client/
    Configuration/
    Errors/
    Extensions/
    Generated/
    Http/
    Internal/
    Models/
    MercadoBitcoinJsonSerializerContext.cs
```

### Proposed Improved Layout (optional)

```
src/
  MercadoBitcoin.Client/
    Core/
    Infrastructure/
    Domain/
    Generated/
    Observability/
```

### .NET 10 Migration Notes

- Update `<TargetFramework>` to `net10.0` and `LangVersion` to `14.0`.
- Upgrade `Microsoft.Extensions.*` packages to 10.0.0.
- Adopt `System.Threading.RateLimiting` and `Microsoft.Extensions.Http.Resilience` where appropriate.
- Keep NSwag generation isolated in `Generated/` and ensure AOT-friendly options.

### .csproj sample for .NET 10

See `src/MercadoBitcoin.Client/MercadoBitcoin.Client.csproj` (example in refactor documents) for a full recommended project file.

**Next**: implement migration branch and run tests.

```
