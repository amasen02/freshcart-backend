## What

<!-- One short paragraph: what does this PR change? -->

## Why

<!-- Link an ADR, an issue, or describe the business / technical driver. -->

## How

<!-- Notable design decisions, trade-offs, alternatives considered. -->

## Affected boundaries

- [ ] Domain layer changed (new aggregate / invariant / domain event)
- [ ] Application layer changed (new command / query / handler / validator)
- [ ] Infrastructure changed (DB schema, migration, external adapter)
- [ ] API contract changed (route, request / response shape, status code)
- [ ] Cross-service contract changed (integration event, gRPC proto)
- [ ] Building blocks changed
- [ ] Infrastructure as code changed (Bicep / Helm / k8s)
- [ ] CI / CD pipeline changed

## Security checklist

- [ ] No secrets committed
- [ ] Input validation present on every new command / query
- [ ] AuthN + AuthZ policies applied to every new endpoint
- [ ] OWASP Top-10 review run for new attack surface
- [ ] Outbound HTTP allow-listed (no SSRF surface added)
- [ ] PII fields encrypted at rest if relevant

## Tests

- [ ] Unit tests added / updated
- [ ] Integration tests added / updated (Testcontainers)
- [ ] Manual smoke test recorded below

## Manual smoke evidence

<!-- Paste a curl, an HTTP request screenshot, or steps to reproduce. -->

## Rollout

- [ ] DB migration is forward-only and idempotent
- [ ] Feature flag wired up (if behaviour change)
- [ ] Dashboard / alert updated for new SLO surface
