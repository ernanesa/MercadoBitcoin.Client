```markdown
# 20. Breaking Changes and Compatibility (v3 → v4)

## Index
1. Objective
2. Compatibility overview
3. Internal vs public changes
4. Potentially breaking changes
5. Mitigation strategies
6. Compatibility checklist

---

## 1. Objective

Catalog and control changes that may be breaking between v3.x and v4.0.0.

## 2. Overview

Aim to keep the public API as compatible as possible; internal implementations can change more freely.

## 3. Internal vs Public

Internal folders (`Internal/**`) can be refactored freely. Public facade (`MercadoBitcoinClient`) and DTOs should be stable unless documented.

## 4. Potentially breaking changes

- `class` → `readonly struct` conversions (e.g., `CandleData`) are a binary breaking change.
- Rate limiting behavior and timeout defaults may change runtime behavior.

## 5. Mitigations

- Semver bump to 4.0.0 and document breaking changes.
- Provide migration guide and compatibility overloads where feasible; mark deprecated APIs with `[Obsolete]`.

## 6. Checklist

- [ ] Review public surface
- [ ] Identify binary incompatibilities
- [ ] Document all breaking changes in migration guide and release notes

```
