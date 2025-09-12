# Changelog

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

Consulte também RELEASE_NOTES_v3.0.0.md para detalhes de migração e exemplos.
