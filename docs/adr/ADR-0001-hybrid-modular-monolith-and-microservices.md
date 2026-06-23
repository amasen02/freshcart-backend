# ADR-0001 — Hybrid: modular monolith plus selected microservices

- **Status:** Accepted
- **Date:** 2026-05-25
- **Author:** Ama Senevirathne

## Context

FreshCart is a portfolio reference implementation of an online supermarket. The candidate's CV
covers both microservices and modular monolith experience. A purist single-monolith repo would
demonstrate one half of the CV; a purist microservices-only repo would demonstrate the other half
and force a junior-grade decomposition (every entity its own service). Neither is honest about how
production systems actually look in 2026 — the industry has rebalanced away from "microservices for
everything" toward modular monolith + selectively-extracted microservices.

## Decision

The platform is split as follows:

1. **Twelve bounded contexts** (Identity, Catalog, Pricing, Basket, Ordering, Inventory, Payment,
   Delivery, Notification, CustomerSupport, Reviews, Reporting) ship as **independent microservices**
   because they have meaningfully different scaling, persistence and ownership profiles.
2. **AdminBackoffice** ships as a **modular monolith** with `IModule`-style boundaries (Catalog
   admin module, Inventory admin module, Reporting module). The intent is to demonstrate the
   pattern explicitly and to show that "not everything has to be a separate process".
3. The two evolution stages **before** the microservices split (single-app monolith, monolith + DB,
   monolith + UI) are documented in `docs/evolution/` as ADRs and small reference projects so the
   *climb up the ladder* is part of the interview narrative.

Each microservice intentionally uses a *different* architectural style internally — Clean
Architecture, Vertical Slice, Layered, Hexagonal — so the repo doubles as a teaching artifact.
This choice is described in `ARCHITECTURE.md` and called out in each service's
`docs/interview-tour/` card.

## Consequences

**Positive**

- The repo demonstrates every CV-listed pattern in its native habitat instead of forcing one style.
- The modular monolith is real and runnable, not just a slide.
- The architecture supports a credible "we should not extract this" answer when an interviewer
  asks about decomposition limits.

**Negative**

- The repo is larger and more complex than either a pure monolith or a pure microservice repo would
  be. Mitigated by an interview-tour deck under `docs/interview-tour/` that gives a 90-second
  walkthrough per service.
- Newcomers must learn more than one architectural style. Mitigated by `docs/CONVENTIONS.md`
  describing the cross-cutting style rules that apply across all services.

## Alternatives considered

- **Pure microservices.** Rejected — twelve services *all* in Clean Architecture would hide the
  candidate's Vertical Slice + Modular Monolith + Layered experience.
- **Pure modular monolith.** Rejected — would not demonstrate microservices, saga, outbox, gRPC,
  service mesh, polyglot persistence at any depth.
- **Three or four services only.** Rejected — too few surfaces to cover the full pattern catalog.

## References

- `~/.claude/knowledge/microservices/architecture-decision-framework.md`
- 2026 industry trend toward modular monolith with .NET Aspire — [`aspiresoftwareconsultancy.com`](https://aspiresoftwareconsultancy.com/dotnet-aspire-modular-monolith/)
