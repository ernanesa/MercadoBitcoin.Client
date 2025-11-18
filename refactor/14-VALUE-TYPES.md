# 14. VALUE TYPES AND MODEL OPTIMIZATION

## 📋 Index

1. [Objective](#objective)
2. [Analysis of Current Models](#analysis-of-current-models)
3. [Criteria for Using Value Types](#criteria-for-using-value-types)
4. [Target Scenarios](#target-scenarios)
5. [Modeling Patterns](#modeling-patterns)
6. [Integration with JSON and AOT](#integration-with-json-and-aot)
7. [Performance Impacts](#performance-impacts)
8. [Action Plan](#action-plan)

---

## 1. Objective

Define a strategy for using **value types (struct, readonly struct)** in the library models in order to:

- 💾 Reduce heap allocations
- ⚡ Improve cache locality and throughput
- 🔒 Preserve code clarity and safety

---

## 2. Análise dos Modelos Atuais

### 2.1. Modelos Principais

- `CandleData` (ver 13-CANDLEDATA-STRUCT.md)
- Demais DTOs (Tickers, Orders, Trades etc.)

### 2.2. Observações Iniciais

- A maioria dos modelos é usada como DTO para JSON
- Alguns são candidatos a value type, outros não

---

## 3. Critérios para Uso de Value Types

### 3.1. Quando Usar Struct

- Quando o tipo:
  - Tem semântica de **valor** (e.g., candle, cotação)
  - É pequeno/moderado em tamanho (até ~128 bytes, regra geral)
  - É imutável

### 3.2. Quando Não Usar Struct

- Quando o tipo é:
  - Grande e raramente alocado
  - Rico em comportamento, com herança ou polimorfismo
  - Frequentemente passado por referência

---

## 4. Cenários-alvo

### 4.1. Tipos de Domínio Simples

- Ex.: `SymbolSpan`, tipos de identificadores, pequenos tipos financeiros (ex.: `PriceLevel` com `Price` + `Quantity`)

### 4.2. Inline Types

- Structs usados em coleções densas (arrays, spans) para processamento de indicadores técnicos

---

## 5. Padrões de Modelagem

### 5.1. Value Object Pattern

- Usar `readonly struct` para representar value objects simples:

```csharp
public readonly struct PriceLevel
{
    public decimal Price { get; }
    public decimal Quantity { get; }

    public PriceLevel(decimal price, decimal quantity)
    {
        Price = price;
        Quantity = quantity;
    }
}
```

### 5.2. Tipos Embrulhando Primitivos

- Para dar semântica a valores (ex.: `OrderId`, `TradeId`):

```csharp
public readonly struct OrderId
{
    public string Value { get; }

    public OrderId(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString() => Value;
}
```

> Atenção: para tipos que encapsulam string, o ganho de performance pode ser menor, mas melhora a expressividade.

---

## 6. Integração com JSON e AOT

### 6.1. System.Text.Json

- `readonly struct` é suportado normalmente
- Garantir entrada em `MercadoBitcoinJsonSerializerContext`

### 6.2. Conversores Customizados

- Alguns value types podem precisar de `JsonConverter` customizado

---

## 7. Performance Impacts

### 7.1. Benefits

- Fewer allocations for heavily used types
- Better data density in arrays/spans

### 7.2. Risks

- Very large structs can hurt performance (expensive copies)

---

## 8. Action Plan

1. Convert `CandleData` to `readonly struct` (see doc 13)
2. Identify other candidates (e.g., small domain types)
3. Measure impact in before/after benchmarks
4. Document changed types in release notes

---

**Document**: 14-VALUE-TYPES.md  
**Version**: 1.0  
**Date**: 2025-11-18  
**Status**: ✅ Strategy Defined
