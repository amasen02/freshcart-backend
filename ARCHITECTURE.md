# FreshCart — Architecture

This document is the **C4 narrative** for FreshCart. It covers the system at three levels of zoom:
**Context (C1)** → **Containers (C2)** → **Components (C3)** of the most interesting service.
Implementation-level walkthroughs (C4) live per service under `docs/interview-tour/`.

> Companion document. `README.md` is the elevator pitch; this is the engineering brief.

---

## Tenets

Five principles every decision in this repo is checked against:

1. **One demonstrable pattern per service.** Each bounded context is implemented in a *different*
   architectural style so the repo is a teaching artifact, not a uniform mass.
2. **Database per service.** No cross-service joins. Cross-service reads use API aggregation or a
   materialised read model.
3. **Sync hops are at most one deep.** Beyond that, services communicate via events with the
   transactional outbox guarantee.
4. **Cookie-first for browsers, JWT for everything else.** Browser sessions never see a JWT. Mobile
   and service-to-service callers never see a session cookie.
5. **Observability is not an add-on.** Every service emits OpenTelemetry traces + metrics + logs from
   day one. If you cannot see it, you cannot operate it.

---

## C1 — System context

```
                         ┌────────────────────────────┐
                         │      Customer (browser)    │
                         └─────────────┬──────────────┘
                                       │ HTTPS + cookies
                                       ▼
                         ┌────────────────────────────┐
                         │  Azure Front Door + WAF    │
                         └─────────────┬──────────────┘
                                       │
                                       ▼
                         ┌────────────────────────────┐
                         │   YARP API Gateway (BFF)   │
                         └─────────────┬──────────────┘
                                       │
       ┌───────────┬───────────┬───────┴────────┬───────────┬───────────┐
       ▼           ▼           ▼                ▼           ▼           ▼
  ┌────────┐ ┌─────────┐ ┌────────────┐  ┌──────────┐ ┌──────────┐ ┌────────┐
  │Identity│ │Catalog  │ │Basket+Out- │  │Ordering  │ │Inventory │ │Payment │
  │ (Clean │ │(VSlice) │ │  box, Hyb- │  │ (DDD+    │ │ (Layered │ │ (Clean │
  │  Arch) │ │+Marten  │ │  ridCache) │  │  Saga)   │ │ +Dapper) │ │+Event  │
  │        │ │+Postgres│ │ +Postgres  │  │ +SqlSrv  │ │ +SqlSrv  │ │ Sourc.)│
  └────────┘ └─────────┘ └────────────┘  └──────────┘ └──────────┘ └────────┘
       │           │           │                │           │           │
       └───────────┴───────────┴────────────────┴───────────┴───────────┘
                                  │ events (RabbitMQ / Azure Service Bus)
                                  ▼
       ┌────────────────────────────────────────────────────────┐
       │  Notification (SignalR), Delivery, Reviews, CustSupport│
       │ (WebSocket chat), Reporting (QuestPDF + MySQL warehouse)│
       └────────────────────────────────────────────────────────┘
```

External actors:

| Actor | Touchpoint | Auth |
|---|---|---|
| Customer | Angular customer SPA | Cookie (HttpOnly + Secure + SameSite=Strict) |
| Support agent | Angular admin SPA | Cookie |
| Administrator | Angular admin SPA | Cookie |
| Mobile app *(future)* | REST + WebSocket | JWT + refresh |
| Internal service | gRPC / HTTP / RabbitMQ | JWT (client_credentials) |
| Payment gateway *(external)* | Outbound HTTPS | Mutual TLS |
| Email / SMS provider *(external)* | Outbound HTTPS | API key from Key Vault |

---

## C2 — Containers (deployed units)

Every container in the table maps to a separately-versioned Helm chart, a separately-tagged Docker
image, and a separately-pipelined Azure DevOps YAML.

| Container | Technology | Persistence | Key dependencies |
|---|---|---|---|
| `gateway` (YARP) | .NET 10 | — | Identity (cookie validation), all downstream services |
| `identity` | .NET 10 + ASP.NET Identity + Argon2id | Azure SQL | Redis (sessions) |
| `catalog` | .NET 10 + Carter + MediatR + Marten | Azure Postgres Flexible Server | Redis (HybridCache L2) |
| `pricing` | .NET 10 gRPC | SQLite (reference data) | — |
| `basket` | .NET 10 + Carter + Outbox | Azure Postgres + Redis (HybridCache) | catalog (HTTP), pricing (gRPC), RabbitMQ |
| `ordering` | .NET 10 + EF Core + Dapper + MassTransit state machine | Azure SQL (writes + outbox + saga state) | inventory (gRPC), payment (HTTPS), RabbitMQ |
| `inventory` | .NET 10 + Dapper | Azure SQL | RabbitMQ |
| `payment` | .NET 10 + event sourcing | MongoDB (event store) + Azure SQL (read projection) | external gateway, RabbitMQ |
| `delivery` | .NET 10 + Hexagonal | MongoDB (geo) | Google Maps / Mapbox |
| `notification` | .NET 10 + SignalR + MassTransit consumer | MongoDB | Redis (backplane) |
| `customer-support` | .NET 10 + SignalR over WebSocket | MongoDB (transcripts) | Redis (presence) |
| `reviews` | .NET 10 + Carter | MongoDB | — |
| `reporting` | .NET 10 + QuestPDF + Dapper | MySQL (warehouse) | RabbitMQ (read projection input) |
| `customer-spa` | Angular 20 + Bootstrap 5 | static (Azure Static Web Apps optional) | gateway |
| `admin-backoffice` *(planned)* | .NET 10 modular monolith | Azure Postgres (shared) | — |
| `admin-spa` *(planned)* | Angular 20 + Bootstrap 5 | static | gateway |

Cross-cutting infra:

- `rabbitmq` (DEV) → Azure Service Bus (PROD)
- `redis`
- `seq` (DEV) → Azure App Insights (PROD)
- `prometheus + grafana` (DEV) → Azure Monitor managed Prometheus + Azure Managed Grafana (PROD)
- `otel-collector` everywhere

---

## C3 — Component view (Identity service — Clean Architecture reference)

The Identity service is the worked example at component zoom because every other service depends on
its cookies / JWTs. It uses **Clean Architecture** (four projects, dependencies flow inwards):

```
FreshCart.Identity.Api                ← composition root, HTTP, Carter, cookie + JWT, anti-forgery
        │
        ▼
FreshCart.Identity.Infrastructure     ← EF Core, ASP.NET Identity, Argon2id, JWT issuer, refresh-token service
        │
        ▼
FreshCart.Identity.Application        ← CQRS commands & queries, validators, abstractions (ports)
        │
        ▼
FreshCart.Identity.Domain             ← ApplicationUser, ApplicationRole, RefreshToken, AuditEvent
```

Key components inside Identity:

| Component | Lives in | Purpose |
|---|---|---|
| `SignUpCommand`, `SignInCommand`, `SignOutCommand`, `RefreshAccessTokenCommand`, `GetCurrentUserQuery` | Application | Use cases as CQRS records |
| `ValidationBehavior`, `LoggingBehavior` | BuildingBlocks | Pipeline cross-cutting |
| `CustomExceptionHandler` | BuildingBlocks | RFC 7807 ProblemDetails mapping |
| `IdentityDbContext` | Infrastructure | EF Core unit of work |
| `Argon2PasswordHasher<TUser>` | Infrastructure | Replaces PBKDF2 with memory-hard Argon2id |
| `JwtAccessTokenIssuer` | Infrastructure | HS256 short-lived access token |
| `RefreshTokenService` | Infrastructure | Plaintext token only on issue/rotate; SHA-256 hash in DB; reuse detection revokes the family |
| `EntityFrameworkAuditLog` | Infrastructure | Append-only audit table |
| `AuthenticationConfiguration` | Api | Cookie scheme (HttpOnly + Secure + SameSite=Strict) + JWT bearer scheme |
| `AntiforgeryConfiguration` | Api | Issues XSRF-TOKEN cookie consumed by the SPA |
| `AuthenticationEndpoints`, `AccountEndpoints` | Api | Carter modules per feature |

The same shape &mdash; Domain, Application, Infrastructure, Api &mdash; carries over to **Ordering**
and **Payment**, the other two Clean Architecture services. Catalog, Basket and Reviews collapse to a
single project because Vertical Slice does not need the project split.

---

## Per-service architecture matrix

Each bounded context picks the internal architecture its requirements justify, not a uniform house
style. The twelve services and the reason each chose its shape:

| Service | Internal architecture | Persistence | Why this shape |
|---|---|---|---|
| Identity | Clean Architecture + ASP.NET Identity | Azure SQL | Security boundary with rich rules; PII isolated behind four inward-pointing projects |
| Catalog | Vertical Slice + Carter + MediatR + Marten | Postgres (Marten document store) | Read-heavy CRUD; each feature slice is independent; the document model fits the product schema |
| Pricing | Service classes behind gRPC | SQLite reference data | Stateless calculator on the synchronous hot path; no domain state to model |
| Basket | Vertical Slice + Outbox + Decorator | Postgres + Redis HybridCache | Per-user transient state; Redis is the native fit; the outbox guards the checkout event |
| Ordering | Clean Architecture + DDD + MassTransit saga | SQL Server (EF Core writes, Dapper reads) | Richest domain; invariants live in the aggregate; the saga orchestrates checkout |
| Inventory | Layered (Repository + Unit of Work) + gRPC | SQL Server (Dapper) | Transactional correctness and raw query speed over elegance; gRPC reserve on the hot path |
| Payment | Clean Architecture + Event Sourcing | MongoDB event store + SQL projection | Compliance requires an immutable append-only audit; the read projection serves queries |
| Delivery | Hexagonal (Ports & Adapters) | MongoDB (geo indexes) | Multiple external adapters (maps, driver app, geo); ports keep them swappable |
| Notification | Pipes-and-filters consumers + SignalR | MongoDB + Redis backplane | No domain logic; the complexity is fan-out routing across replicas |
| CustomerSupport | Stateful WebSocket + connection manager | MongoDB transcripts + Redis presence | Stateful connections and round-robin assignment; the domain itself is simple |
| Reviews | Vertical Slice + document store | MongoDB | Schemaless documents; independent features; eventual consistency is acceptable |
| Reporting | CQRS read model + QuestPDF + Dapper | MySQL warehouse + Blob Storage | OLAP query patterns; no writes; Dapper carries the analytical SQL |

The edge and client surfaces:

| Surface | Shape | Status |
|---|---|---|
| YARP API Gateway | Reverse proxy + cookie-to-JWT BFF token exchange | Built |
| Customer SPA | Angular 20 standalone + signals + Bootstrap 5 | Built |
| AdminBackoffice | Modular monolith (`IModule` pattern) | Planned |
| Admin SPA | Angular 20 standalone + signals + Bootstrap 5 | Planned |

---

## Integration events

Every cross-service message is one of the shared contracts in
`BuildingBlocks.Messaging/IntegrationEvents`. Producers and consumers bind to the same record; the
contract is additive-only, so a breaking change means a new event type rather than an edited one.

| Event | Published by | Consumed by |
|---|---|---|
| `ProductCreatedIntegrationEvent` | Catalog | Inventory (seeds stock) |
| `ProductPriceChangedIntegrationEvent` | Catalog | Basket (refreshes cached line prices) |
| `BasketCheckoutStartedIntegrationEvent` | Basket | Ordering (saga start), Delivery (opens a shipment) |
| `OrderPlacedIntegrationEvent` | Ordering | Notification |
| `StockReservedIntegrationEvent` | Inventory | Ordering (saga) |
| `StockReservationFailedIntegrationEvent` | Inventory | Ordering (saga compensation) |
| `PaymentCapturedIntegrationEvent` | Payment | Ordering (saga) |
| `PaymentFailedIntegrationEvent` | Payment | Ordering (saga compensation), Notification |
| `OrderConfirmedIntegrationEvent` | Ordering | Delivery (schedules a slot), Notification, Reviews (verified-purchase eligibility), Reporting (projection) |
| `OrderCancelledIntegrationEvent` | Ordering | Inventory (releases reservations), Notification |
| `OrderRefundedIntegrationEvent` | Ordering | Notification, Reporting (projection) |
| `DeliveryScheduledIntegrationEvent` | Delivery | Notification |
| `DeliveryCompletedIntegrationEvent` | Delivery | Notification |

`BasketCheckoutStartedIntegrationEvent` carries `CheckoutAddress` and `CheckoutLine`;
`OrderConfirmedIntegrationEvent` carries `OrderConfirmedLine`. Cross-service writes that must not be
lost go through the transactional outbox (Basket, Ordering); consumers are idempotent via the inbox.

---

## BFF cookie-to-JWT flow

The gateway is the trust boundary. The browser holds an opaque, HttpOnly + Secure + SameSite=Strict
session cookie and never sees a JWT; downstream services only ever see a JWT and never see the cookie.

1. The SPA calls the gateway over `/api/*`. The gateway authenticates the request against the shared
   cookie scheme (the data-protection key ring is persisted to Redis so any gateway replica can read
   the cookie).
2. `AntiforgeryValidationMiddleware` enforces the double-submit token on every state-changing request
   at the edge.
3. For routes that require a downstream identity, `TokenExchangeTransformProvider` installs a YARP
   request transform. The transform runs for ordinary HTTP proxying and for the WebSocket upgrades
   that carry the SignalR hubs, so the hubs receive the same bearer token as the REST routes.
4. `CookieToJwtTokenExchanger` maps the cookie principal to a `DownstreamPrincipal` (subject, email,
   display name, roles) and asks `HmacDownstreamTokenSigner` for a short-lived HS256 bearer token.
5. The minted token is cached per identity and per authentication instant. Keying on the
   authentication instant means a re-authenticated principal mints a fresh token instead of replaying
   a stale one; an absent instant fails loudly rather than collapsing to a stable, replayable key.
6. The transform strips any inbound `Authorization` header and attaches the minted bearer token, then
   YARP forwards the request to the downstream cluster.

---

## Checkout saga flow

Ordering hosts the orchestrated saga as a MassTransit state machine
(`CheckoutSagaStateMachine`). The machine stays declarative: aggregate updates run in activities and
external side effects run in work consumers driven by saga-internal commands, so each step is
testable on its own. Every event correlates on `OrderId`.

```
BasketCheckoutStarted
        │  submit order + request stock reservation
        ▼
AwaitingStockReservation ──StockReservationFailed──▶ Cancelled (compensate, finalize)
        │ StockReserved
        ▼
AwaitingPayment ──PaymentFailed──▶ Cancelled (compensate, finalize)
        │ PaymentCaptured
        ▼
Confirmed (finalize) ──▶ OrderConfirmed fans out to Delivery, Notification, Reviews, Reporting
```

On `BasketCheckoutStarted` the saga publishes `SubmitOrderFromCheckout` and `ReserveOrderStock`, then
waits in `AwaitingStockReservation`. `StockReserved` marks the order and moves it to `AwaitingPayment`;
`PaymentCaptured` confirms the order and finalizes. Either failure event
(`StockReservationFailed`, `PaymentFailed`) runs the matching compensation activity, transitions to
`Cancelled`, and finalizes. Reserving stock before payment is deliberate: the saga can release the
reservation cleanly when payment fails.

---

## Patterns demonstrated, and where

| Pattern | Lives in |
|---|---|
| Monolith | `docs/evolution/01-monolith.md` (reference only) |
| Modular Monolith | `src/ModularMonolith/FreshCart.AdminBackoffice` *(planned)* |
| Layered Architecture | `src/Services/Inventory` |
| Vertical Slice Architecture | `src/Services/Catalog`, `src/Services/Basket`, `src/Services/Reviews` |
| Clean / Onion Architecture | `src/Services/Identity`, `src/Services/Ordering`, `src/Services/Payment` |
| Hexagonal (Ports & Adapters) | `src/Services/Delivery` |
| CQRS | every service via `BuildingBlocks/CQRS` |
| Mediator | every service via MediatR + `BuildingBlocks/Behaviors` |
| Pipeline Behavior | `BuildingBlocks/Behaviors/{ValidationBehavior, LoggingBehavior}.cs` |
| Domain Driven Design (Aggregate, ValueObject, DomainEvent) | `src/Services/Ordering/Ordering.Domain` |
| Repository + Unit of Work | `src/Services/Ordering/Ordering.Infrastructure`, `src/Services/Inventory` |
| Decorator (CachedRepository) | `src/Services/Basket` |
| Transactional Outbox | `src/Services/Basket`, `src/Services/Ordering`, base in `BuildingBlocks.Messaging/Outbox` |
| Inbox (idempotent consumer) | base in `BuildingBlocks.Messaging`; consumers in Ordering, Payment |
| Saga (orchestrated) | `src/Services/Ordering` MassTransit state machine |
| Event Sourcing | `src/Services/Payment` |
| API Gateway | `src/ApiGateways/FreshCart.Gateway.Yarp` |
| BFF (Backend For Frontend) | gateway cookie-to-JWT token exchange (`src/ApiGateways/FreshCart.Gateway.Yarp/Auth`) |
| Hybrid Cache (L1 + L2) | `src/Services/Basket` |
| Circuit Breaker / Retry / Bulkhead / Timeout | every typed HttpClient via `AddStandardResilienceHandler` |
| gRPC | `src/Services/Pricing.Grpc`, `src/Services/Inventory` reserve endpoints |
| SignalR (notifications) | `src/Services/Notification` |
| WebSocket chat | `src/Services/CustomerSupport` |
| QuestPDF reporting | `src/Services/Reporting` |
| Polyglot persistence | matrix in `README.md` and `docs/adr/ADR-0003-polyglot-persistence.md` |

---

## Cross-cutting concerns map

| Concern | Implementation |
|---|---|
| AuthN (browser) | ASP.NET Identity + cookie scheme; `FreshCart.Session` cookie; HttpOnly + Secure + SameSite=Strict |
| AuthN (service / mobile) | JWT bearer; refresh-token rotation in `RefreshTokenService` |
| AuthZ | Per-endpoint policies (`Customer`, `SupportAgent`, `Administrator`); resource-based handlers for BOLA |
| Validation | FluentValidation, dispatched by `ValidationBehavior` |
| Errors | `CustomExceptionHandler` → RFC 7807 ProblemDetails with `traceId` |
| Observability | OpenTelemetry (traces + metrics + logs) → OTLP → App Insights + Managed Prometheus |
| Logging | Serilog → Console + Seq (DEV) → Log Analytics (PROD), enriched with TraceId/SpanId |
| Resilience | `AddStandardResilienceHandler()` on every typed HttpClient |
| Caching | `HybridCache` (.NET 9+) — L1 in-proc + L2 Redis |
| Idempotency | Inbox + `Idempotency-Key` header on mutating endpoints |
| Anti-forgery | XSRF-TOKEN cookie + X-XSRF-TOKEN header double-submit |
| Security headers | `UseFreshCartSecurityHeaders()` middleware (CSP, X-Frame-Options=DENY, Referrer-Policy, Permissions-Policy, COOP/COEP/CORP) |
| Secrets | Azure Key Vault + Workload Identity on AKS; user-secrets locally |
| Audit | Append-only `AuditEvents` per service |

---

## Quality gates

| Gate | Where |
|---|---|
| Strict compiler | `Directory.Build.props` (`TreatWarningsAsErrors=true`, `Nullable=enable`, `EnforceCodeStyleInBuild=true`) |
| Roslyn analyzers | Microsoft.CodeAnalysis.NetAnalyzers, StyleCop, Sonar, Meziantou, AsyncFixer, Roslynator |
| SonarCloud | Azure DevOps pipeline stage |
| Trivy | Container image scan stage |
| Dependabot | `.github/dependabot.yaml` (later phase) |
| `dotnet list package --vulnerable` | Pipeline |
| Cosign image signing | Pipeline |
| Tests | `dotnet test` — xUnit + Testcontainers |

---

## Deployment topology

- **Local:** Docker Compose + .NET Aspire AppHost — single command brings up backing services + every microservice.
- **Azure:** Bicep modules under `infra/` provision AKS (system + user pool), ACR, Azure SQL, Azure Postgres Flexible, Azure MySQL, Cosmos DB, Azure Cache for Redis, Azure Service Bus, Key Vault, App Configuration, Log Analytics, App Insights, Front Door, Storage.
- **CI/CD:** Azure DevOps multi-stage pipelines (one YAML per service) extend a shared template. Stages: build → quality → container build + Trivy + Cosign + ACR push → deploy DEV → deploy STAGING (1 approver) → deploy PROD (2 approvers, blue/green).

A `gitops/` overlay carries Flux / Argo Application manifests for teams that prefer pull-based delivery.
