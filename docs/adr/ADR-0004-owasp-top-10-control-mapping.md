# ADR-0004 — OWASP Top 10 2025 control mapping

- **Status:** Accepted
- **Date:** 2026-05-25
- **Author:** Ama Senevirathne

## Context

FreshCart handles customer identity, payment information and personally-identifiable shipping
addresses. The OWASP Top 10 release published in 2025 collapsed SSRF into Broken Access Control and
expanded Vulnerable Components into Software Supply Chain Failures. The repo must show explicit,
testable controls against every category so that the interview answer to "how do you secure this?"
is concrete.

## Decision

| OWASP rank | Risk | Control in FreshCart |
|---|---|---|
| A01 | Broken Access Control (now incl. SSRF) | Per-endpoint `[Authorize(Policy = ...)]`; resource-based authorisation handlers (defeat BOLA); outbound `OutboundUrlAllowListHandler` rejects any host not on the per-service allow list; AKS NetworkPolicy blocks egress to `169.254.169.254` |
| A02 | Cryptographic Failures | HTTPS only + HSTS preload; TLS 1.3 minimum; Azure Key Vault for secrets; column-level `EncryptedString` value converter for PII; SQL TDE; AES-GCM for app-level encryption |
| A03 | Injection | EF Core + Dapper parameterised queries only; FluentValidation on every command; Angular's built-in sanitisation; Razor output encoding |
| A04 | Insecure Design | ADR per service decision; STRIDE threat model per bounded context in `docs/threat-models/` |
| A05 | Security Misconfiguration | `UseFreshCartSecurityHeaders()` applies CSP, X-Frame-Options=DENY, X-Content-Type-Options=nosniff, Referrer-Policy, Permissions-Policy, COOP/COEP/CORP; `Server` and `X-Powered-By` stripped |
| A06 | Software Supply Chain Failures | Dependabot weekly; `dotnet list package --vulnerable` in CI; Trivy image scan fails on HIGH+; SBOM (CycloneDX) per release; package source pinned |
| A07 | ID & Auth Failures | ASP.NET Identity + Argon2id; lockout after 5 failures for 15 minutes; HttpOnly + Secure + SameSite=Strict session cookie; anti-forgery double-submit; idle + absolute session timeouts |
| A08 | Software / Data Integrity | Cosign image signing; admission controller verifies signature in PROD; signed Azure DevOps artifacts; tamper-proof transactional outbox + inbox |
| A09 | Security Logging & Monitoring | Serilog → App Insights + Log Analytics with correlation id; append-only `AuditEvents` table per service; security event sink for failed sign-ins, token replay, lockouts |
| A10 | (merged into A01) — explicit SSRF defence | `OutboundUrlAllowListHandler` + AKS egress NetworkPolicy |

## Consequences

**Positive**

- The interview answer "show me how you protect against X" lands on a specific class/middleware/policy.
- Controls are tested by integration tests (cookie flags, status codes, audit rows) and by static
  analysis (analyzers + SonarCloud quality gate).

**Negative**

- The control set adds operational overhead (Key Vault, Cosign, admission controller). Mitigated by
  Bicep modules and Azure DevOps pipeline templates that bake the controls in.

## References

- OWASP Top 10 — 2025 release notes
- `~/.claude/knowledge/microservices/cross-cutting-concerns.md`
