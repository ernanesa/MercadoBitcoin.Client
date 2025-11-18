```markdown
# Implementation Roadmap - Mercado Bitcoin API

## 🗺 Overview

This project already includes a solid base: HTTP/2, retries, rate limiting and Source Generators. The roadmap focuses on expansion, optimization and production hardening.

---

## Phase 1 — Environment Setup

Goals:
- Configure development environment
- Validate test credentials
- Configure tools and build

Tasks:

```bash
# Clone repository
git clone https://github.com/usuario/MercadoBitcoin.Client.git
cd MercadoBitcoin.Client

# Restore
dotnet restore

# Build
dotnet build

# Configure user-secrets (example)
dotnet user-secrets init --project samples/AuthBalanceConsole
dotnet user-secrets set "MercadoBitcoin:ApiId" "your_api_id"
dotnet user-secrets set "MercadoBitcoin:ApiSecret" "your_api_secret"

# Run sample
cd samples/AuthBalanceConsole
dotnet run
```

Checklist:
- [ ] .NET SDK installed
- [ ] IDE configured
- [ ] Git configured
- [ ] Test credentials obtained

Estimated time: 1–2 hours

---

## Phase 2 — Endpoint Validation

Goals:
- Ensure all endpoints work
- Find gaps and inconsistencies
- Validate rate limiting and retries

Tasks include public and private endpoint tests (unit & integration).

Estimated time: 2–3 days

---

## Phase 3 — Missing Features

Examples: WebSocket client, technical indicators, basic trading strategies (Grid, DCA).

Estimated time: 2–3 weeks

---

## Phase 4 — Optimization & Performance

Advanced batching, caching, connection pooling, profiling and benchmarks.

Estimated time: 1–2 weeks

---

## Phase 5 — Production Hardening

Observability, health checks, CI/CD and deployment automation.

Estimated time: 1–2 weeks

---

## Phase 6 — Documentation & Support

Complete API reference, guides and examples.

Estimated time: 1 week

---

## Timeline

Total: ~6–10 weeks depending on team size and priorities.

**Next**: implement Phase 1 and start endpoint validation.

```
