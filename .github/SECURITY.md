# Security policy

## Threat model

The repo-wide control mapping against OWASP Top 10 2025 lives in
`docs/adr/ADR-0004-owasp-top-10-control-mapping.md`. Per-service STRIDE threat models have not
been written; the table below is the inventory of what actually ships, and it is the only
security claim this repository makes.

## Controls in force

| Control | Implementation |
|---|---|
| Transport security | HTTPS everywhere; TLS terminated at the ingress via cert-manager. The gateway emits `Strict-Transport-Security: max-age=31536000; includeSubDomains` — no `preload`, which is a domain-wide, effectively irreversible commitment. Azure-managed resources pin TLS 1.2 as the floor |
| Auth (browser) | ASP.NET Identity + HttpOnly + Secure + SameSite=Strict cookie, anti-forgery double-submit |
| Auth (service-to-service) | JWT signed with a single symmetric HS256 key (`Jwt:SigningKey`, sourced from Azure Key Vault) that every service holds; the Ordering saga mints its own service token with it. There is no client-credentials endpoint and no rotation code. **Known limitation:** with a symmetric key, verifying a token and issuing one are the same capability, so any service holding the key can mint `ServiceCaller` or `Administrator` tokens that Payment, Inventory and Ordering will accept. Moving issuance to Identity with an asymmetric key (RS256/ES256 + JWKS) is the intended fix and is not implemented |
| Password hashing | Argon2id (memory-hard) via Konscious.Security.Cryptography; legacy users migrate on next sign-in |
| Rate limiting | Fixed-window limiter at the gateway (100/10s globally, 10/min on `/api/auth/sign-in`, `/sign-up`, `/refresh`), partitioned on the connection address. `X-Forwarded-For` is honoured only from a hop listed in `ForwardedHeaders:KnownProxies`/`KnownNetworks`; with none configured the header is ignored outright so a caller cannot choose its own partition |
| Input validation | FluentValidation on every command/query; rejected at the `ValidationBehavior` pipeline before any handler runs |
| Output encoding | Razor / Angular default; manual encoding when interpolating into HTML attributes |
| SQL injection | Parameterized only — EF Core or Dapper; no `string.Format` into SQL |
| SSRF | Both typed `HttpClient`s use a fixed `BaseAddress` with relative request URIs, so no caller-supplied URL reaches an outbound request. Cluster egress NetworkPolicy permits only `10.0.0.0/8`, Azure DNS and kube-system DNS, which denies `169.254.169.254`. `OutboundUrlAllowListHandler` exists in BuildingBlocks with unit tests but is **not** attached to any typed `HttpClient` — do not rely on it until it is registered |
| Security headers | `SecurityHeadersMiddleware` (`UseFreshCartSecurityHeaders()`): CSP strict, X-Frame-Options=DENY, X-Content-Type-Options=nosniff, Referrer-Policy, Permissions-Policy, COOP/COEP/CORP |
| Secrets | Azure Key Vault + Workload Identity on AKS |
| Key ring | Data-protection keys persist to a dedicated Redis instance (`ConnectionStrings:dataprotection`) reachable only by the gateway and Identity. Outside Development the services refuse to start rather than write the ring to the shared application cache. The ring itself is stored unencrypted, so that isolation is the control |
| Encryption at rest | Platform-level only: TDE on Azure SQL, plus the service-managed encryption the other Azure data services apply by default. There is **no** application-layer or column-level PII encryption — customer email, display name and shipping/billing addresses sit in plaintext columns |
| Audit logging | Append-only `AuditEvents` table in the Identity service (sign-in, sign-out, refresh, MFA enrolment); Serilog → App Insights with correlation id everywhere |
| Dependency hygiene | Dependabot weekly; `dotnet list package --vulnerable` in CI; Trivy image scan fails on fixable HIGH/CRITICAL |
| Image integrity | Images pushed from `master` are signed keylessly with Cosign/Sigstore. No admission-controller policy ships in this repository, so signature verification at deploy time is not enforced here |

## Reporting a vulnerability

Email `amabandarasp@gmail.com` with subject prefix `[SECURITY]`. Do not open a
public issue. Expect acknowledgement within 72 hours.

## Coordinated disclosure window

90 days from acknowledgement, unless mutually extended.
