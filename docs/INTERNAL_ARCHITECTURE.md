# FreshCart Internal Architecture

Per-service deep dive. Each section answers four questions:

1. What problem does this service solve?
2. How is it shaped internally and why?
3. Which technologies sit where, and what trade-off justified each one?
4. How does the failure model behave?

Read this in tandem with `docs/CLASS_DIAGRAMS.md` (the static picture) and the animated flow
diagrams in `index.html` (the dynamic picture).

---

## 1. Identity

### Purpose

Single security boundary for the platform. Owns the user record, issues sessions for browser
clients and bearer tokens for service-to-service callers, and exposes one authoritative view
of "who is making this call" to every other service.

### Internal shape

```
FreshCart.Identity.Api             composition root, HTTP, Carter endpoints
        ↓
FreshCart.Identity.Infrastructure  EF Core, ASP.NET Identity, Argon2id, JWT, refresh tokens
        ↓
FreshCart.Identity.Application     CQRS commands + queries + validators + abstractions (ports)
        ↓
FreshCart.Identity.Domain          ApplicationUser, ApplicationRole, RefreshToken, AuditEvent
```

Dependencies always point inward. The Domain project has no NuGet references except the
single `Microsoft.AspNetCore.Identity` package it needs for the base `IdentityUser` and
`IdentityRole` types.

### Authentication state machine

```
[Anonymous] --POST /auth/sign-up--> [Registered, signed-out]
[Registered, signed-out] --POST /auth/sign-in (cookie mode)--> [Signed-in, cookie]
[Registered, signed-out] --POST /auth/sign-in (bearer mode)--> [Signed-in, bearer]
[Signed-in, cookie]  --POST /auth/sign-out--> [Signed-out, refresh tokens revoked]
[Signed-in, bearer]  --POST /auth/refresh--> [Signed-in, rotated bearer]
[Signed-in, bearer]  --replay revoked refresh--> [Signed-out, family revoked]
[*]                  --5 bad passwords in 15 min--> [Locked, 15 min]
```

### Technology choices

| Concern | Technology | Why |
|---|---|---|
| User store | ASP.NET Identity + EF Core + SQL Server | Mature, integrated with the rest of the platform; ASP.NET Identity ships the password-hashing pipeline, lockout, MFA, role claims out of the box |
| Password hashing | Argon2id (Konscious.Security.Cryptography.Argon2) | Memory-hard so GPU-based offline cracking becomes prohibitively expensive; OWASP recommendation for new applications |
| Browser auth | HttpOnly + Secure + SameSite=Strict cookie + double-submit XSRF | Tokens never reach JavaScript-readable storage so an XSS payload cannot exfiltrate them |
| Bearer auth | HS256 JWT issued by `JwtAccessTokenIssuer`, 15-minute lifetime | Short enough that revocation latency is acceptable without a revocation list |
| Refresh tokens | 64-byte random, SHA-256 hashed in DB, rotated on every refresh | Plaintext only on the wire and only at issue time |
| Reuse detection | Replayed token revokes the entire family | If a thief and the victim both hold the token, only one can refresh; the other's attempt blows up the family |
| Audit log | Append-only `identity.AuditEvents` table | Mounted on the same database as the user table so the audit row commits in the same transaction as the user state change |
| Demo data | `IdentityDataSeeder : IHostedService` | Guarded by `IHostEnvironment.IsDevelopment()`; refuses to write in Staging or Production |

### Failure model

| Failure | Behaviour |
|---|---|
| SQL Server unavailable | `IdentityDbContext` is configured with `EnableRetryOnFailure(5)`; transient failures are absorbed; readiness probe flips to unhealthy after the retry budget exhausts |
| Argon2 hashing under load | Pod CPU autoscaler triggers at 65% CPU; sign-in latency tracked by `LoggingBehavior` with a 3-second warning threshold |
| Refresh-token race | Rotation is a single EF transaction: revoke old + insert new + commit. Concurrent rotation attempts serialise on the row lock |
| Key Vault unavailable at startup | `Program.cs` throws if `Jwt:SigningKey` is missing. K8s restart policy retries. The pod stays out of the load balancer until ready |

---

## 2. Reporting

### Purpose

Two complementary surfaces in one bounded context:

1. Read side for the admin SPA. KPI tiles, time-series charts, top-N rankings.
2. Document generation. Customer invoice PDFs and bulk Excel exports.

A single warehouse, denormalised and projected from integration events emitted by every other
service, backs both.

### Internal shape

```
FreshCart.Reporting.Api            Carter endpoints, JWT auth, OpenAPI
        ↓
FreshCart.Reporting.Infrastructure Dapper + EF Core + QuestPDF + ClosedXML + Azure Blob + MassTransit + BackgroundService
        ↓
FreshCart.Reporting.Application    Queries + commands + projection consumers + abstractions
        ↓
FreshCart.Reporting.Domain         Invoice + InvoiceNumber + SalesSnapshot + KpiMetric (pure types)
```

### Persistence layout

The same MySQL database holds three logically distinct tables. EF Core owns the writes that
need transactions (invoice numbering, projection inbox). Dapper owns the dashboard reads.

```
warehouse
├── invoices                       managed by InvoiceRepository (EF Core)
├── invoice_lines                  ← owns line records (FK → invoices)
├── invoice_number_sequences       row-lock allocation of next number per (year, kind)
├── projection_inbox               idempotency keys for the consumer pipeline
├── sales_facts                    denormalised order rows, written by WarehouseProjectionWriter
├── sales_line_facts               denormalised line rows
├── customer_lifetime_value        running per-customer totals
├── inventory_snapshot             populated by a separate hourly job (planned in P5)
├── delivery_facts                 populated by Delivery service projection (planned in P6)
└── customer_segment_snapshot      populated by a daily segmenter job (planned)
```

Why two ORMs on one DB: EF Core gives the change tracker, identity map, transaction scope and
migrations Reporting needs for the write paths that must be atomic. Dapper gives sub-millisecond
read overhead for the dashboard queries that read aggregates of thousands of rows; EF would
spend more time on materialisation than on query execution. Picking the right tool per path
beats picking one tool everywhere.

### Invoice numbering

Tax jurisdictions require monotonically-increasing, gap-free invoice numbers per year per kind
(sale / credit note / pro-forma). The implementation:

1. The (year, kind) row in `invoice_number_sequences` is read inside a transaction.
2. The row is updated (`LastSequence += 1`). On InnoDB this acquires a record lock until the
   transaction commits, so concurrent allocators serialise on the row.
3. The new sequence is returned to the caller, which then composes the human-readable number
   (`INV-2026-000123`) and persists the full invoice in the same transaction.

This is one of the rare cases where pessimistic locking is the right answer. The alternative
(optimistic concurrency with retries) wastes the database round-trip budget when contention
is moderate, and gap-free requires that nobody can "skip" a number after a failed insert.

### Event projection

Consumers are wrapped by an idempotency-inbox check. Each consumer body looks like:

```
if (await inbox.HasProcessedAsync(eventId)) return;
await projectionWriter.Apply{X}Async(event);
await inbox.RecordProcessedAsync(eventId);
```

The three operations run in a single MySQL transaction (Dapper + `BeginTransactionAsync`).
Replays produce the same final state because the UPSERT statements (`ON DUPLICATE KEY UPDATE`)
are idempotent for the projection columns.

### Failure model

| Failure | Behaviour |
|---|---|
| MySQL unavailable | EF retry-on-failure (5 attempts) for writes; Dapper queries surface the failure to the API as a 500 (caught by `CustomExceptionHandler`); readiness probe goes amber after retry exhaustion |
| Blob Storage unavailable | Invoice generation aborts before the DB write, so the caller sees a 5xx and retries with the same `OrderId`. Because the DB write has not happened the next attempt regenerates the same invoice number atomically |
| QuestPDF render exception | Bubbles to `CustomExceptionHandler` and produces a ProblemDetails 500; no partial PDF is uploaded because upload is the next step after a successful render |
| MassTransit consumer crash mid-projection | Transaction rolls back; the message is redelivered (MassTransit message-retry middleware with exponential back-off). The inbox check on next delivery prevents double-application |
| Scheduled report skips a day | `DailySalesReportBackgroundService` ticks hourly; the next tick notices the missing file and generates it. If yesterday's data has since been amended, the file reflects the latest state |

### Why QuestPDF and ClosedXML

| Choice | Alternative | Why this one |
|---|---|---|
| QuestPDF | RDLC | RDLC needs `Microsoft.Reporting.NETCore` which only runs on Windows; FreshCart targets chiselled Linux containers on AKS. QuestPDF is managed-only, fluent, fast |
| QuestPDF Community | QuestPDF Professional | Community is free up to $1M annual revenue, which is exactly the portfolio-repo use case; swap to Professional with one configuration line if commercialised |
| ClosedXML | EPPlus | EPPlus requires a commercial licence since 5.0. ClosedXML is MIT and produces equivalent output. Both work without Office installed |
| Azure Blob + SAS URL | Stream through the API | Streaming through the API wastes pod bandwidth and adds latency. SAS URLs let the browser fetch from Blob directly with a time-bound credential |

---

## 3. BuildingBlocks (cross-cutting library)

### Purpose

Single library every service references. Contains the small, deliberately thin set of types
that justify cross-service consistency: the CQRS interfaces, the pipeline behaviors, the
exception sink, the security primitives. Anything wider belongs in a service, not here.

### What is included and why

| Type | Why it lives in BuildingBlocks |
|---|---|
| `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler` | Every service uses MediatR. Shared interfaces let pipeline behaviors target both commands and queries uniformly |
| `ValidationBehavior` | Every command goes through FluentValidation in the same way. Duplicating this per service would drift |
| `LoggingBehavior` | Same. The slow-handler threshold is the same SLO platform-wide |
| `CustomExceptionHandler` | RFC 7807 ProblemDetails is the contract every service exposes. The mapping table for domain exceptions to HTTP status codes is repeated nowhere else |
| `DomainException` + friends | Shared type hierarchy so the exception handler can map them by base type rather than per-exception per-service |
| `PaginationRequest` + `PaginatedResult` | Same contract on the wire for every paged response. Without this, each service would invent its own page-envelope and clients would proliferate |
| `OutboundUrlAllowListHandler` | Defence-in-depth against SSRF. The same DelegatingHandler is wired into every typed HttpClient |
| `SecurityHeadersMiddleware` | Same set of headers on every response. Inconsistency is a vulnerability |

### What is deliberately not included

- No generic `IRepository<T>`. EF Core already is the abstraction.
- No generic `IUnitOfWork`. `DbContext` is the unit of work.
- No `BaseEntity` god class with audit columns. Audit columns live on `Entity<T>` in the
  Ordering domain where they are needed. Other services do not pay for them.
- No `Result<T>` monad wrapped around every method. The pure-domain `Result` exists for the
  one place it pays for itself; everywhere else, exceptions are clearer.

These omissions are intentional and called out in `docs/CONVENTIONS.md` so reviewers do not
add them back during a refactor.

---

## 4. Cross-service contracts

### Integration events

Concrete events live in `FreshCart.BuildingBlocks.Messaging.IntegrationEvents`, one file per
event, referenced by both producer and consumer. The full flow:

| Event | Producer | Consumers |
|---|---|---|
| `BasketCheckoutStartedIntegrationEvent` | Basket (outbox) | Ordering saga; Delivery (captures the shipping address into a local `PendingShipment`) |
| `OrderPlacedIntegrationEvent` | Ordering (outbox) | Notification |
| `StockReservedIntegrationEvent` / `StockReservationFailedIntegrationEvent` | Ordering work consumer, after the Inventory gRPC call | Ordering saga |
| `PaymentCapturedIntegrationEvent` / `PaymentFailedIntegrationEvent` | Ordering work consumer, after the Payment REST call | Ordering saga; Notification (on PaymentFailed) |
| `OrderConfirmedIntegrationEvent` | Ordering (outbox) | Reporting, Notification, Reviews, Delivery |
| `OrderCancelledIntegrationEvent` | Ordering (outbox) | Notification, Inventory (release) |
| `OrderRefundedIntegrationEvent` | Ordering (outbox) | Reporting, Notification |
| `DeliveryScheduledIntegrationEvent` / `DeliveryCompletedIntegrationEvent` | Delivery | Notification |
| `ProductCreatedIntegrationEvent` | Catalog | Inventory (creates the stock row) |
| `ProductPriceChangedIntegrationEvent` | Catalog | Basket (refresh stored line prices) |

`StockReserved`, `StockReservationFailed`, `PaymentCaptured` and `PaymentFailed` are internal to
the Ordering bounded context: Payment and Inventory expose synchronous contracts (REST and gRPC),
and the Ordering work consumers translate those synchronous outcomes into the saga events above.
Payment publishes nothing to the bus.

Every event extends `IntegrationEvent` from `FreshCart.BuildingBlocks.Messaging.Events`, which
gives each event a stable `EventId`, an `OccurredOnUtc` timestamp and the assembly-qualified type
name for diagnostics. Cross-service writes that must not be lost (Basket checkout, Ordering's
domain events) go through a transactional outbox; consumers are idempotent by `EventId` or by the
natural key of the upsert.

### gRPC contracts

Defined under `Services/{X}/{X}.Grpc/Protos/*.proto` (Pricing) and `Services/Inventory/FreshCart.Inventory.Api/Protos/*.proto` (Inventory). Additive evolution only; never renumber
or repurpose a field. The reviewer in `CODEOWNERS` enforces this.

### REST contracts

OpenAPI documents generated per service at `/openapi/v1.json`. The CI pipeline publishes them
as artifacts so contract tests can fetch them.

---

## 5. Operational concerns

### Health probes

Every service exposes three endpoints, mapped by `MapDefaultHealthEndpoints()`:

| Endpoint | Probe | Implementation |
|---|---|---|
| `/health` | Composite | All registered checks must pass |
| `/alive` | Liveness | Only checks tagged `live` (the default "self" check) |
| `/ready` | Readiness | Only checks tagged `ready` (database, broker, cache) |

Kubernetes uses `/alive` for the liveness probe (restart on failure) and `/ready` for the
readiness probe (remove from load balancer on failure but do not restart).

### Observability

- Logs via Serilog → OpenTelemetry Logs → OTLP. Locally to Seq, in cloud to App Insights.
- Metrics via OpenTelemetry → Prometheus. Locally to Grafana, in cloud to Azure Managed Grafana.
- Traces via OpenTelemetry → OTLP. Cross-service propagation through W3C TraceContext on HTTP,
  gRPC, MassTransit and SignalR.

### Resilience

`AddStandardResilienceHandler()` is applied to every typed HttpClient in `ServiceDefaults`.
It bundles retry, circuit breaker, timeout and bulkhead in one call with sane defaults that
match the platform-wide SLOs.

### Secrets

Locally: `dotnet user-secrets`. In cluster: Azure Key Vault via the Secrets Store CSI Driver
mounted into each pod, bound by Workload Identity (no client-secret credentials).

### Deployment strategy

- DEV: auto on push to `main`. Rolling update.
- STAGING: one approver. Rolling update. Integration tests in cluster.
- PROD: two approvers. Blue/green via Helm + service selector swap. Synthetic monitoring +
  automated rollback on probe failure.
