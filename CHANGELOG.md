# Changelog

## [4.0.0-alpha.1] - 2025-11-20
### Breaking Changes
- Migração da biblioteca para **.NET 10** e **C# 14** (`net10.0`).
- Remoção dos construtores públicos de conveniência de `MercadoBitcoinClient`; uso recomendado agora apenas via métodos de extensão (`MercadoBitcoinClientExtensions.CreateWithRetryPolicies`, `CreateWithHttp2`, `CreateForTrading`, etc.) ou DI (`services.AddMercadoBitcoinClient(...)`).
- Padronização da configuração via `MercadoBitcoinClientOptions`, `HttpConfiguration` e `RetryPolicyConfig`.
- HTTP/2 passa a ser o protocolo padrão nas factories; HTTP/3 é suportado via configuração explícita.

### Melhorias
- Suporte opcional a **HTTP/3 (QUIC)** via `HttpConfiguration.CreateHttp3Default()`.
- Novas configurações de retry baseadas em Polly v8 (`CreateTradingRetryConfig`, `CreatePublicDataRetryConfig`).
- Extensões SIMD (`CandleMathExtensions`) para análise de candles de alta performance.
- Novas métricas via `System.Diagnostics.Metrics` e `RateLimiterMetrics` para acompanhar retries e rate limiting.
- Documentação expandida: `README.md` revisado, `docs/USER_GUIDE.md` e `docs/AI_USAGE_GUIDE.md`.

### Notas
- Versão marcada como **alpha**, indicada para testes e validação antes da 4.0.0 estável.
- Consulte `RELEASE_NOTES_v4.0.0-alpha.1.md` para detalhes de migração.

## [3.0.0] - 2025-08-27
### Breaking Changes
- Todos os construtores públicos de `MercadoBitcoinClient` foram removidos. Instanciação agora apenas via métodos de extensão (`MercadoBitcoinClientExtensions.CreateWithRetryPolicies`, etc.) ou DI (`services.AddMercadoBitcoinClient`).
- Métodos legados como `CreateLegacyHttpClient` e construtores diretos foram removidos.
- Alinhamento total com práticas modernas do .NET 9, AOT e DI.
- Documentação e exemplos atualizados para refletir a nova abordagem.
- Preparação para futuras extensões e suporte a novas features da API Mercado Bitcoin.

### Melhorias
- Documentação revisada e exemplos atualizados.
- Release notes detalhados em RELEASE_NOTES_v3.0.0.md.

## [2.1.1] - 2025-06-10
### Patch
- Compatibilidade AOT aprimorada (TypeInfoResolver, DTOs extras no JsonSerializerContext).
- Correção de erros de deserialização JSON em builds AOT.
- Ajuste de threshold de memória nos testes.
- Todos os 64 testes passando.

## [2.1.0] - 2025-05-01
### Minor
- Jitter configurável em backoff.
- Circuit breaker manual.
- CancellationToken em 100% dos endpoints.
- User-Agent customizável via env.
- Métricas nativas: counters e histogram de latência.
- Test suite expandida: 64 cenários.

## [2.0.1] - 2024-12-15
### Patch
- Correção de tratamento de erros no AuthHttpClient.
- Cobertura de testes ampliada.
- Otimização de uso de memória.

## [2.0.0] - 2024-11-01
### Major (BREAKING CHANGES)
- Migração completa para System.Text.Json com Source Generators.
- Suporte total a AOT.
- .NET 9 e C# 13.
- HTTP/2 nativo.
- Testes abrangentes.
- Remoção de dependência do Newtonsoft.Json.
- Mudança de snake_case nos nomes JSON.
- Melhorias de performance e arquitetura.

---

Consulte também `RELEASE_NOTES_v3.0.0.md` e `RELEASE_NOTES_v4.0.0-alpha.1.md` para detalhes de migração e exemplos.
