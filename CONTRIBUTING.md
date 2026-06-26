# Contributing to FreshCart

This is a portfolio reference repository. External pull requests are welcome
where they sharpen a pattern, fix a bug, or improve documentation — provided
they keep the senior-architect tone of the codebase.

## Ground rules

1. **One concern per pull request.** No drive-by refactors mixed with feature
   work.
2. **Trunk-based development.** Branch from `master`, keep the branch short
   (ideally <3 days), squash-merge back.
3. **Conventional commits** (`feat(scope): …`, `fix(scope): …`,
   `chore(scope): …`, `docs(scope): …`, `refactor(scope): …`,
   `test(scope): …`, `perf(scope): …`).
4. **Green CI is non-negotiable.** Build + tests + SonarCloud quality gate +
   Trivy must all pass before review.
5. **No skipped hooks**, no `--no-verify`, no `[skip ci]` outside docs-only
   commits.
6. **PR template** must be filled. Empty checkboxes block review.

## Coding standards

See `docs/CONVENTIONS.md`. The short version:

- C# 13 features welcome, but readability outranks novelty.
- Variable names are full descriptive nouns/verbs. `repo`, `ctx`, `tmp` are
  rejected at review.
- No filler comments. Comments explain *why*, never *what*.
- Records for DTOs, commands, queries, value objects. Classes for entities and
  aggregates.
- `CancellationToken` on every `async` method.
- `ConfigureAwait(false)` inside `BuildingBlocks` only — application code
  can omit it (ASP.NET Core has no sync context to deadlock).
- Public APIs are XML-documented; internal members are not unless the *why*
  is non-obvious.

## Architecture

Each microservice deliberately exercises a *different* pattern (Clean
Architecture, Vertical Slice, Layered, Modular Monolith, Hexagonal,
gRPC-first, etc). See `docs/adr/` for the rationale behind each choice and
`docs/interview-tour/` for the per-service walkthrough.

Do not refactor one service into another service's style — the spread is
intentional.

## Tests

- Unit tests in `<Service>.Tests` next to the service.
- Integration tests in `tests/integration/` using Testcontainers — they boot
  real Postgres / SQL Server / Redis / RabbitMQ.
- Contract tests in `tests/contract/` (Pact).
- Load smoke in `tests/load/` (k6).
- E2E in `tests/e2e/` (Playwright).

A PR that ships code without a test is sent back unless it is purely
documentation.

## Local development

```bash
# Bring up local backing services
docker compose -f deploy/docker/docker-compose.yaml up -d

# Run the whole stack via .NET Aspire
dotnet run --project src/AspireAppHost/FreshCart.AppHost

# Run a single service (example: Identity)
dotnet run --project src/Services/Identity/FreshCart.Identity.Api

# Run tests for the whole solution
dotnet test
```
