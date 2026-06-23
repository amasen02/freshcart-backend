# FreshCart tests

Four layers, each with a single purpose. Run them all with one command:

```bash
dotnet test --settings tests/coverlet.runsettings --collect "XPlat Code Coverage"
```

## Layers

| Folder | Purpose | Stack |
|---|---|---|
| `src/**/*.Tests` | Unit + service-level integration. xUnit + FluentAssertions + NSubstitute. Per-service projects sit next to their service. Service-level integration uses Testcontainers to boot real SQL Server / Postgres / MySQL / MongoDB / Redis / RabbitMQ. | xUnit, Testcontainers |
| `tests/integration` | Cross-service integration scenarios. Boots two or more services in a single process via `WebApplicationFactory<Program>` and exercises end-to-end. | xUnit, WebApplicationFactory |
| `tests/contract` | Provider verification of consumer Pact files. | Pact.NET |
| `tests/load` | k6 scripts that match the SLO targets in `docs/REQUIREMENTS.md`. | k6, JavaScript |
| `tests/e2e` | Playwright happy-path coverage of the storefront. | Playwright, TypeScript |

## Coverage

Coverlet collects line and branch coverage on every run. Configuration in
`tests/coverlet.runsettings` excludes:

- EF Core migrations (auto-generated)
- `Program.cs` bootstrap files (hosted integration coverage measures these)
- `GlobalUsings.cs`
- Anything attributed `[ExcludeFromCodeCoverage]`, `[GeneratedCode]`, `[CompilerGenerated]`,
  `[Obsolete]`

The coverage target is **85% line coverage on the Application + Domain + BuildingBlocks
projects**. The SonarCloud quality gate fails any pull request that drops coverage on the
changed lines below 80%.

## Running a single layer

```bash
# Unit tests for a service
dotnet test src/Services/Identity/FreshCart.Identity.Tests

# BuildingBlocks suite
dotnet test src/BuildingBlocks/FreshCart.BuildingBlocks.Tests

# Playwright (3 browsers + mobile)
cd tests/e2e && npm test
```
