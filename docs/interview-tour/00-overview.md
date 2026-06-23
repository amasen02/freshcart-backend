# Interview tour — overview

> The 5-minute pitch. After this page, pick any service from the matrix to drill into.

## What this repo is

FreshCart is a runnable reference implementation of an online supermarket platform built to exercise
every architectural pattern, every cross-cutting concern, and every Azure capability that the
candidate (Ama Senevirathne) describes on the CV — in one place, with one consistent style of code,
and with a defensible reason for every decision.

## What it is not

- It is not a hosted commercial product.
- It is not a copy of an existing reference repo. It is an original synthesis of the candidate's
  experience and a curated set of 2026 best practices.
- It is not over-engineered. Where a simpler pattern fits, the simpler pattern is used (Vertical
  Slice for CRUD services, Layered for transactional data-heavy services, Clean Architecture only
  where the domain is rich enough to need it).

## How to walk the repo at the interview

1. **Start here**, then go to `ARCHITECTURE.md` for the C4 narrative.
2. Pick a service from the matrix in `README.md`. Open its `docs/interview-tour/<service>.md`
   card — it gives you the 90-second pitch (what / why / trade-off / code link).
3. If asked to drill deeper, follow the code link. Each service's structure is consistent with
   `docs/CONVENTIONS.md`.
4. If asked "why is service X using pattern Y?", open the matching ADR under `docs/adr/`.

## The order to talk about things

If you have **5 minutes**: Overview → pick **Ordering** (Clean / DDD / Saga / Outbox) — it covers
the senior-level rope in one walk.

If you have **15 minutes**: Overview → Identity (cookies + JWT + Argon2) → Ordering → one more
service the interviewer picks.

If you have **30 minutes**: Overview → Identity → Catalog (Vertical Slice) → Basket (Outbox +
HybridCache + Decorator) → Ordering (DDD + Saga) → Notification (SignalR) → CustomerSupport
(WebSocket).

## What every service has

- **Dockerfile** — multi-stage, chiselled runtime, non-root user.
- **Health checks** — `/health`, `/alive`, `/ready`.
- **OpenTelemetry** — traces, metrics, logs via `AddFreshCartServiceDefaults`.
- **Serilog** — Console (DEV) + App Insights (PROD).
- **Resilience** — `AddStandardResilienceHandler` on every typed HttpClient.
- **Validation** — FluentValidation dispatched by a pipeline behavior.
- **Errors** — `CustomExceptionHandler` → RFC 7807 ProblemDetails.
- **Tests** — `<Service>.Tests` project, Testcontainers-backed integration tests.
- **Pipeline** — `azure-pipelines/<service>-pipeline.yaml` with the standard stages.
- **Helm chart** — `deploy/helm/<service>` with `values-{dev,staging,prod}.yaml`.

## Where to find each pattern in code

See `ARCHITECTURE.md` § "Patterns demonstrated, and where" for the full table.
