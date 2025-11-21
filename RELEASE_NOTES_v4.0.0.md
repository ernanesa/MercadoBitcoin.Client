# Release Notes v4.0.0

## Breaking Changes
- **Construtores removidos**: `new MercadoBitcoinClient()` obsoleto. Use:
  ```csharp
  var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
  // ou DI
  services.AddMercadoBitcoinClient(options => { ... });
  ```
- **.NET 10 required**: Atualize `global.json` e projetos consumidores para `net10.0`.
- **HTTP/3 default**: Configure `options.HttpVersion = HttpVersion.Version20` se necessário.

## Migration Steps
1. Substitua construtores por factories/DI.
2. Atualize `global.json` para .NET 10.
3. Teste com `dotnet test` (65 testes).

## New Features
- HTTP/3, Polly v8, RateLimiting.
- AOT full support (SourceGen JSON).
- Metrics, SIMD candle math.

## Verified
- All endpoints tested with real credentials.
- Pack succeeds, ready for NuGet.

See CHANGELOG.md for full details.