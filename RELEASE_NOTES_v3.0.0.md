# Release Notes v3.0.0 (BREAKING CHANGE)

## Principais mudanças

- **Remoção de todos os construtores públicos de `MercadoBitcoinClient`**
  - Agora, a única forma suportada de instanciar o client é via métodos de extensão (`MercadoBitcoinClientExtensions.CreateWithRetryPolicies`, etc.) ou injeção de dependência (`services.AddMercadoBitcoinClient(...)`).
  - Métodos legados como `CreateLegacyHttpClient` e construtores diretos foram removidos.

## Como migrar

**Antes (obsoleto):**
```csharp
var client = new MercadoBitcoinClient();
```

**Depois (recomendado):**
```csharp
var client = MercadoBitcoinClientExtensions.CreateWithRetryPolicies();
// ou via DI:
services.AddMercadoBitcoinClient(...);
```

Consulte o README.md para exemplos completos e instruções detalhadas.

## Motivo da mudança

- Alinhamento com as melhores práticas do .NET 9 e AOT.
- Evita uso incorreto de HttpClient e problemas de performance.
- Facilita integração com DI e configurações modernas.

## Outras melhorias
- Documentação revisada e exemplos atualizados.
- Preparação para futuras extensões e suporte a novas features da API Mercado Bitcoin.

---

Para dúvidas ou problemas na migração, consulte a documentação ou abra uma issue no repositório.