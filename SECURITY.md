# Security policy

## Threat model

Each microservice ships with its own STRIDE threat model under
`docs/threat-models/<service>.md`. The repo-wide control mapping against
OWASP Top 10 2025 lives in `docs/adr/ADR-0004-owasp-top-10-control-mapping.md`.

## Controls in force

| Control | Implementation |
|---|---|
| Transport security | HTTPS everywhere, HSTS with preload, TLS 1.3 minimum |
| Auth (browser) | ASP.NET Identity + HttpOnly + Secure + SameSite=Strict cookie, anti-forgery double-submit |
| Auth (service-to-service) | JWT (client credentials) issued by Identity service, signed with a key rotated from Azure Key Vault |
| Password hashing | Argon2id (memory-hard) via Konscious.Security.Cryptography; legacy users migrate on next sign-in |
| Input validation | FluentValidation on every command/query; rejected at the `ValidationBehavior` pipeline before any handler runs |
| Output encoding | Razor / Angular default; manual encoding when interpolating into HTML attributes |
| SQL injection | Parameterized only — EF Core or Dapper; no `string.Format` into SQL |
| SSRF | `OutboundUrlAllowListHandler` on every typed `HttpClient` rejecting any host not on the per-service allow list; cluster egress NetworkPolicy blocks `169.254.169.254` |
| Security headers | NWebsec middleware: CSP strict, X-Frame-Options=DENY, X-Content-Type-Options=nosniff, Referrer-Policy=strict-origin, Permissions-Policy minimal |
| Secrets | Azure Key Vault + Workload Identity on AKS; no secrets in env vars at rest |
| Encryption at rest | TDE on SQL; column-level `EncryptedString` value converter for PII (AES-GCM) |
| Audit logging | Append-only `AuditEvents` table per service; Serilog → App Insights with correlation id |
| Dependency hygiene | Dependabot weekly; `dotnet list package --vulnerable` in CI; Trivy image scan fails on HIGH+ |
| Image integrity | Cosign-signed container images; admission controller verifies signature in prod cluster |

## Reporting a vulnerability

Email `amabandarasp@gmail.com` with subject prefix `[SECURITY]`. Do not open a
public issue. Expect acknowledgement within 72 hours.

## Coordinated disclosure window

90 days from acknowledgement, unless mutually extended.
