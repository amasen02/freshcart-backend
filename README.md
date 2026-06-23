# FreshCart &mdash; Online Supermarket Reference Architecture

> **Start here:** open **[`index.html`](index.html)** in any browser. That single page is the
> animated architecture guide, the demo-credentials sheet, the operations runbook and the
> interview tour, all in one self-contained document.

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-20-red)](https://angular.dev/)
[![Aspire](https://img.shields.io/badge/Aspire-13-blue)](https://learn.microsoft.com/en-us/dotnet/aspire/)
[![Azure](https://img.shields.io/badge/Azure-AKS-0078d4)](https://azure.microsoft.com/en-us/products/kubernetes-service)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

A single, walkable portfolio repository. Twelve bounded contexts, each in a deliberately
different architectural style, behind a YARP gateway, with an Angular 20 signal-based
storefront, hosted on Azure Kubernetes Service through Azure DevOps or GitHub Actions
multi-stage pipelines.

---

## Try it now &mdash; demo URLs + seeded credentials

The platform ships with three pre-seeded accounts so you can sign in and walk through the
customer and back-office flows in under a minute. The
[`IdentityDataSeeder`](src/Services/Identity/FreshCart.Identity.Infrastructure/Persistence/IdentityDataSeeder.cs)
creates them automatically on first boot when `ASPNETCORE_ENVIRONMENT=Development`. It refuses
to run in any other environment.

### Boot the platform

```bash
# 1. Backing services (SQL Server, Postgres, MySQL, MongoDB, Redis, RabbitMQ, Seq, Grafana, Prometheus)
docker compose -f deploy/docker/docker-compose.yaml up -d

# 2. Whole .NET stack via Aspire — boots every microservice + the gateway + seeds demo accounts
dotnet run --project src/AspireAppHost/FreshCart.AppHost

# 3. Customer storefront — ng serve proxies /api and /hubs to the gateway on 7100
cd clients/freshcart-customer && npm install && npm start
```

### Open in the browser

| Surface | URL | What it shows |
|---|---|---|
| **Customer storefront** | http://localhost:4200 | Catalog, basket, checkout, orders, support chat, real-time notifications |
| **Aspire dashboard** | http://localhost:15888 | Every service + live traces, metrics, logs |
| **YARP API Gateway** | https://localhost:7100 | Single public edge; cookie-to-JWT BFF exchange |
| **Identity API** (OpenAPI) | https://localhost:7101 | Sign-up, sign-in, refresh, MFA enrollment |
| **Reporting API** (OpenAPI) | https://localhost:7110 | Dashboards, invoices, Excel exports |
| **Seq** (logs) | http://localhost:5341 | Structured logs across every service |
| **Grafana** (metrics) | http://localhost:3000 | RED + USE dashboards (`admin / freshcart_local_dev`) |
| **Prometheus** | http://localhost:9090 | Raw metric store |
| **RabbitMQ management** | http://localhost:15672 | Queues + exchanges (`freshcart / freshcart_local_dev`) |

### Sign in with one of the seeded accounts

| Role | Email | Password | What this account can do |
|---|---|---|---|
| **Customer** | `demo@freshcart.test` | `Demo-P@ssw0rd-2026` | Browse catalog, add to basket, check out, view orders, chat to support |
| **SupportAgent** | `support@freshcart.test` | `Support-P@ssw0rd-2026` | Answer support chats; query the Reporting API for order context |
| **Administrator** | `admin@freshcart.test` | `Admin-P@ssw0rd-2026` | Reporting API: KPI dashboards, invoices, inventory health, exports |

> **Production safety.** The seeder is guarded by `IHostEnvironment.IsDevelopment()` and the
> sign-up validator rejects any `*.test` email &mdash; these accounts cannot exist in
> Staging or Production.

> The admin SPA is a planned phase (see [`CHANGELOG.md`](CHANGELOG.md)). Until it ships, the
> `SupportAgent` and `Administrator` surfaces are reached through the Reporting API and the
> support hub directly.

### 90-second guided tour after sign-in

1. Open <http://localhost:4200> &rarr; sign in as `demo@freshcart.test`.
2. Browse the catalog (30 seeded products across 8 categories), add three items to the basket.
3. Check out &mdash; accept the default address. The Ordering saga drives stock reservation,
   payment capture, and delivery booking.
4. A toast notification appears within ~1 second &mdash; that is the `NotificationHub`
   SignalR push through the Redis backplane.
5. Open the support panel and start a chat; a `SupportAgent` session picks it up over the
   WebSocket hub.
6. Hit the Reporting API at <https://localhost:7110> with the `admin@freshcart.test` token &rarr;
   *Sales overview* reflects the test order in the GMV and order-count tiles.
7. `POST /invoices` for the order &rarr; a QuestPDF-rendered PDF opens via a 15-minute Azure Blob
   SAS URL.
8. `GET /exports/sales-transactions.xlsx` &mdash; ClosedXML Excel with frozen header and
   auto-width columns.

---

## Why this repo exists

The candidate's CV lists eleven years of work across .NET, Angular, Azure, AWS, IdentityServer4,
SignalR, RabbitMQ, microservices, DDD, CQRS, Event Sourcing, Saga, polyglot persistence,
EF Core + Dapper hybrid, SonarQube, Prometheus + Grafana, AKS, EKS, Azure DevOps CI/CD.

A long list of skills is unfalsifiable. This repository is the **falsifiable evidence** &mdash;
every claim anchored to running code, an ADR, and a walkthrough card.

---

## Domain &mdash; twelve bounded contexts

| # | Context | Architecture | Persistence | Status |
|---|---|---|---|---|
| 1 | **Identity** | Clean Architecture + ASP.NET Identity | Azure SQL | Built |
| 2 | **Catalog** | Vertical Slice + Carter + MediatR | PostgreSQL + Marten | Built |
| 3 | **Pricing** | gRPC service | SQLite (in-container) | Built |
| 4 | **Basket** | Vertical Slice + Outbox + Decorator | PostgreSQL + Redis HybridCache | Built |
| 5 | **Ordering** | Clean / Onion + DDD + Saga | SQL Server (EF Core writes, Dapper reads) | Built |
| 6 | **Inventory** | Layered (3-tier) + Dapper | SQL Server | Built |
| 7 | **Payment** | Clean + Event Sourcing | MongoDB events + SQL Server projection | Built |
| 8 | **Delivery** | Hexagonal (Ports & Adapters) | MongoDB (geo indexes) | Built |
| 9 | **Notification** | Pipes-and-filters + SignalR | MongoDB + Redis backplane | Built |
| 10 | **CustomerSupport** | Stateful WebSocket | MongoDB transcripts + Redis presence | Built |
| 11 | **Reviews** | Vertical Slice + Document DB | MongoDB | Built |
| 12 | **Reporting** | CQRS read model + QuestPDF | MySQL warehouse + Blob Storage | Built |

Edge and client:

| Surface | Technology | Status |
|---|---|---|
| **YARP API Gateway** | .NET 10 + YARP; cookie-to-JWT BFF exchange | Built |
| **Customer SPA** | Angular 20 standalone + signals + Bootstrap 5 | Built |
| **AdminBackoffice** | Modular monolith (`IModule` pattern) | Planned |
| **Admin SPA** | Angular 20 standalone + signals + Bootstrap 5 | Planned |

`BuildingBlocks` carries the cross-cutting libraries: `BuildingBlocks` (CQRS, behaviors,
exception handling, pagination, security), `BuildingBlocks.Messaging` (integration event
contracts, outbox, MassTransit wiring), `BuildingBlocks.Observability`, and `ServiceDefaults`.

---

## Reporting suite

The Reporting service is a first-class member of the platform:

- **Executive KPI dashboards** &mdash; GMV, net revenue, AOV, refund rate, customer LTV,
  delivery success rate, inventory health.
- **PDF invoice generation** via QuestPDF with gap-free per-year per-kind invoice numbering
  (`INV-2026-000123`, `CR-2026-000018`, `PF-2026-000009`).
- **Excel exports** via ClosedXML (managed-only, no Office install).
- **Daily scheduled reports** dropped into Azure Blob Storage by a `BackgroundService`.
- **Event projection pipeline** &mdash; `OrderConfirmed`, `OrderRefunded` integration events
  consumed by MassTransit, UPSERT-ed into denormalised MySQL tables. Idempotent at every hop
  via the projection inbox.

See [`index.html`](index.html) &rarr; *Reporting & Invoices* tab for the full surface.

---

## Cross-cutting concerns

- **Authentication.** ASP.NET Identity + **HttpOnly + Secure + SameSite=Strict cookies** for
  the browser; JWT bearer for service-to-service. Anti-forgery double-submit on every
  state-changing endpoint.
- **Authorisation.** Per-endpoint policies (`Customer`, `SupportAgent`, `Administrator`,
  `BackOfficeUser`); resource-based authorization handlers defeat BOLA.
- **Observability.** OpenTelemetry &rarr; OTLP &rarr; Azure App Insights + Log Analytics;
  Azure Managed Prometheus + Managed Grafana. W3C TraceContext propagated through HTTP,
  gRPC, MassTransit, SignalR.
- **Resilience.** `AddStandardResilienceHandler()` on every typed HttpClient (retry +
  circuit breaker + timeout + bulkhead).
- **Validation.** FluentValidation dispatched by the MediatR `ValidationBehavior`.
- **Errors.** Single `CustomExceptionHandler` mapping domain exceptions to RFC 7807
  `ProblemDetails` with `traceId`.
- **Outbox.** Transactional outbox in Basket and Ordering; `OutboxPublisher` background
  worker; consumers idempotent via inbox.
- **Security headers.** `UseFreshCartSecurityHeaders()` middleware applies CSP strict,
  `X-Frame-Options=DENY`, `Referrer-Policy`, `Permissions-Policy`, COOP/COEP/CORP.
- **SSRF defence.** `OutboundUrlAllowListHandler` on every typed HttpClient + AKS egress
  NetworkPolicy blocks `169.254.169.254`.
- **Crypto.** Argon2id password hashing (replaces PBKDF2). Refresh-token reuse detection.

Full OWASP Top-10 2025 mapping in
[`docs/adr/ADR-0004-owasp-top-10-control-mapping.md`](docs/adr/ADR-0004-owasp-top-10-control-mapping.md).

---

## Infrastructure &amp; deployment

- **Local.** Docker Compose + .NET Aspire AppHost.
- **Azure.** Bicep modules under [`infra/`](infra/) provision AKS, ACR, Azure SQL,
  Postgres Flexible Server, MySQL Flexible Server, Cosmos DB, Cache for Redis, Service Bus,
  Key Vault, App Configuration, Log Analytics, App Insights, Front Door + WAF, Storage,
  VNet + private endpoints.
- **CI/CD.** Two equivalent paths ship in the repo:
  - [`azure-pipelines/`](azure-pipelines/) &mdash; multi-stage YAML, AZ-204-grade.
  - [`.github/workflows/`](.github/workflows/) &mdash; reusable workflows, OIDC federated
    identity (no client secrets), CodeQL, Dependabot, Playwright e2e on PR.
- **Helm.** One chart per service in [`deploy/helm/`](deploy/helm/) with
  `values-dev.yaml`, `values-staging.yaml`, `values-prod.yaml`. PodSecurity restricted,
  non-root, read-only rootfs, NetworkPolicy default-deny.
- **GitOps.** Optional overlay under [`gitops/`](gitops/) for Flux / ArgoCD.

---

## Repository layout

```
FreshCart/
├── index.html                  ← LANDING PAGE — open this first
├── README.md                   ← this file
├── ARCHITECTURE.md             ← C4 narrative (C1 + C2 + C3)
├── CHANGELOG.md                ← gradual version history (Keep-a-Changelog format)
├── docs/                       ← ADRs · threat models · interview-tour cards · conventions
├── src/
│   ├── BuildingBlocks/         ← CQRS, Messaging, Observability, ServiceDefaults
│   ├── Services/               ← twelve bounded contexts (Identity … Reporting)
│   ├── ApiGateways/            ← YARP gateway + gateway tests
│   ├── ModularMonolith/        ← AdminBackoffice (planned)
│   └── AspireAppHost/          ← local orchestration
├── clients/
│   └── freshcart-customer/     ← Angular 20 storefront
├── deploy/
│   ├── docker/                 ← docker-compose stack
│   ├── helm/                   ← Helm charts
│   └── k8s/                    ← raw manifests (teaching reference)
├── infra/                      ← Bicep modules + env params
├── azure-pipelines/            ← Azure DevOps multi-stage YAML
├── .github/workflows/          ← GitHub Actions
├── gitops/                     ← Flux / ArgoCD manifests
└── tests/
    ├── integration/            ← Testcontainers
    ├── contract/               ← Pact
    ├── load/                   ← k6
    └── e2e/                    ← Playwright (3-browser + mobile matrix)
```

---

## Tests

- **Unit + integration.** xUnit + FluentAssertions + NSubstitute + Testcontainers (real SQL
  Server / Postgres / MongoDB / Redis / RabbitMQ in Docker).
- **End-to-end.** Playwright &mdash; three browser projects (Chromium / Firefox / WebKit) +
  mobile viewport. Tests under [`tests/e2e/specs/`](tests/e2e/specs/) cover the cookie
  sign-in flow and the full customer happy path; the admin reporting dashboard spec is skipped
  until the admin SPA ships.
- **Contract / load.** Pact (provider verification) and k6 are planned phases
  (see [`CHANGELOG.md`](CHANGELOG.md)).
- **Static.** SonarCloud + Trivy + CodeQL + `dotnet list package --vulnerable` &mdash; all
  wired into both CI/CD paths.

---

## Documentation

Open the **[`index.html`](index.html)** landing page for the animated architecture guide. The
deeper references are:

- [`ARCHITECTURE.md`](ARCHITECTURE.md) &mdash; C4 narrative + cross-cutting map.
- [`docs/CONVENTIONS.md`](docs/CONVENTIONS.md) &mdash; naming, async, LINQ discipline.
- [`docs/adr/`](docs/adr/) &mdash; architecture decision records.
- [`docs/interview-tour/`](docs/interview-tour/) &mdash; 90-second pitch per service.
- [`docs/threat-models/`](docs/threat-models/) &mdash; STRIDE per bounded context.
- [`docs/REQUIREMENTS.md`](docs/REQUIREMENTS.md) &mdash; per-service functional + non-functional requirements with a traceability matrix.
- [`docs/CLASS_DIAGRAMS.md`](docs/CLASS_DIAGRAMS.md) &mdash; Mermaid class + sequence diagrams.
- [`docs/INTERNAL_ARCHITECTURE.md`](docs/INTERNAL_ARCHITECTURE.md) &mdash; per-service deep dive.
- [`CHANGELOG.md`](CHANGELOG.md) &mdash; gradual version history (Keep a Changelog format).
- [`SECURITY.md`](SECURITY.md) &mdash; security policy + control table.
- [`CONTRIBUTING.md`](CONTRIBUTING.md) &mdash; coding standards summary.

---

## License

MIT &mdash; see [`LICENSE`](LICENSE).

## Author

**Ama Senevirathne** &mdash; Senior Software Engineer & Tech Lead. Eleven years on
C# / ASP.NET Core / Angular / Azure / AWS.

- [LinkedIn](https://linkedin.com/in/ama-sen)
- [GitHub](https://github.com/amasen02)
- Email: amabandarasp@gmail.com
