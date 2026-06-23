# ADR-0003 — Polyglot persistence, chosen by access pattern

- **Status:** Accepted
- **Date:** 2026-05-25
- **Author:** Ama Senevirathne

## Context

A reference repo could plausibly run every service against the same SQL Server instance. That would
miss the point: the CV claims experience with SQL Server, PostgreSQL, MySQL, MongoDB, Redis and
Cosmos DB, and the question worth answering at interview is *why* a given store was chosen for a
given service. Picking one store per service makes that conversation possible.

The constraint is that every store choice must be **defensible by access pattern**, not chosen as a
checkbox.

## Decision

| Service | Store | Reason |
|---|---|---|
| Identity | Azure SQL | ASP.NET Identity ships with first-class EF Core SQL Server support; relational integrity matters; PII column-level encryption is straightforward |
| Catalog | Azure Postgres Flexible Server (via Marten) | Marten gives JSONB document features on top of a real relational engine — ideal for product attribute heterogeneity without losing transactions |
| Pricing | SQLite in container | Reference data only; small enough to ship inside the image; no network hop |
| Basket | Azure Postgres + Redis HybridCache | Persistent cart with sub-millisecond hot reads via L1+L2 cache |
| Ordering | Azure SQL + EF Core writes + Dapper reads | Transactional aggregate, hybrid data-access pattern explicitly called out on the CV |
| Inventory | Azure SQL + Dapper | Strong consistency for stock; Dapper for hot reads |
| Payment | MongoDB (event store) + Azure SQL (read projection) | Event-sourced audit trail; SQL for the read model that the rest of the platform queries |
| Delivery | MongoDB | `2dsphere` geo index for slot/route queries |
| Notification | Azure Cosmos DB (SQL API) | Globally distributed low-latency writes |
| CustomerSupport | MongoDB | Schemaless chat transcripts; flexible message types |
| Reviews | MongoDB | Doc-shaped data; large free-text + media references |
| Reporting | MySQL | OLAP warehouse, intentionally a different engine from OLTP to demonstrate ETL boundary |

## Consequences

**Positive**

- Each interview question of the form "why did you use X here?" has a concrete answer rooted in the
  workload.
- Operational complexity is real but bounded by the use of managed services in Azure for every
  store.
- Migration paths are independent — moving Reviews to Cosmos DB later (if/when global distribution
  matters) does not require touching any other service.

**Negative**

- Twelve services × N stores is more infrastructure than a uniform pick. Mitigated by Bicep
  modules + Testcontainers + Aspire AppHost so local-dev stays "single command".
- Polyglot adds operational expertise per store. Mitigated by managed services + per-service
  runbooks under `docs/runbooks/`.

## Alternatives considered

- **SQL Server for everything.** Rejected — would not demonstrate the polyglot CV claim and is a
  worse fit for some access patterns (geo, schemaless, global low-latency).
- **PostgreSQL for everything.** Considered. Postgres can do JSONB, full-text, even geo (PostGIS).
  Rejected because it would erase the CV's MongoDB / MySQL / Cosmos experience.

## References

- `~/.claude/knowledge/microservices/patterns-catalog.md` — section E "Data patterns"
