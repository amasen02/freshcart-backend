# Interview tour — Identity service

> 90-second pitch · 5-minute drill-down · 15-minute architect-level conversation

## The 90-second pitch

The Identity service is the **security boundary** for the entire platform. It is the first service
implemented end-to-end because every other service depends on its cookies or JWTs.

- **Domain:** users, roles, refresh tokens, audit events.
- **Architecture:** Clean Architecture — four projects, dependencies point inwards. Domain has zero
  external dependencies. Application defines ports. Infrastructure implements them. The API project
  is the composition root and the HTTP boundary.
- **Persistence:** Azure SQL via EF Core + ASP.NET Identity. `__EFMigrationsHistory` lives in the
  `identity` schema; every Identity table lives in the same schema.
- **Crypto:** Argon2id replaces the default PBKDF2 because Argon2id is memory-hard and makes
  GPU-based offline cracking materially more expensive. Existing hashes are re-hashed transparently
  on next sign-in if the cost parameters change.
- **Tokens:** dual scheme. Browsers get an HttpOnly + Secure + SameSite=Strict session cookie.
  Mobile + service-to-service callers get a short-lived (15-minute) JWT plus a rotated refresh
  token. Refresh tokens are stored only as SHA-256 hashes; reuse of a revoked token revokes the
  entire family for that user.
- **Anti-forgery:** double-submit. The SPA reads the `XSRF-TOKEN` cookie and echoes it back as the
  `X-XSRF-TOKEN` header on every state-changing request.

## The 5-minute drill-down

| Question | Where to look |
|---|---|
| "Show me the aggregate" | `src/Services/Identity/FreshCart.Identity.Domain/Users/ApplicationUser.cs:1` |
| "Show me a CQRS handler" | `src/Services/Identity/FreshCart.Identity.Application/Authentication/Commands/SignIn/SignInCommandHandler.cs:1` |
| "Show me validation" | `src/Services/Identity/FreshCart.Identity.Application/Authentication/Commands/SignUp/SignUpCommandValidator.cs:1` |
| "Show me cookie setup" | `src/Services/Identity/FreshCart.Identity.Api/Configuration/AuthenticationConfiguration.cs:1` |
| "Show me Argon2id" | `src/Services/Identity/FreshCart.Identity.Infrastructure/Cryptography/Argon2PasswordHasher.cs:1` |
| "Show me refresh-token rotation + reuse detection" | `src/Services/Identity/FreshCart.Identity.Infrastructure/Tokens/RefreshTokenService.cs:1` |
| "Show me an integration test" | `src/Services/Identity/FreshCart.Identity.Tests/Authentication/SignUpAndSignInIntegrationTests.cs:1` |
| "Show me how exceptions become ProblemDetails" | `src/BuildingBlocks/FreshCart.BuildingBlocks/Exceptions/Handler/CustomExceptionHandler.cs:1` |

## The 15-minute conversation

**Why Clean Architecture for Identity specifically?** Auth is a domain with rich invariants
(lockout policy, MFA, refresh rotation, security stamp). Vertical Slice would scatter the
invariants across feature folders. Clean lets the domain own the invariants and the application
layer own the use cases, so policy changes do not ripple through many CRUD-style endpoints.

**Why not use Duende / IdentityServer for the whole flow?** The candidate has IdentityServer4
experience on the CV; the platform retains the option to swap in Duende later. The current
implementation is intentional — ASP.NET Identity + cookies + JWT issuance covers the OWASP
guidance for browser-first sign-in without dragging in the larger Duende dependency tree. For a
future PR that adds true OAuth2 federation (Google, Microsoft, GitHub) Duende lands in this
project.

**Why two schemes?** Browsers should never see a JWT in JavaScript-readable storage; cookies with
HttpOnly + Secure + SameSite=Strict eliminate the XSS-to-account-takeover path. Mobile and
service-to-service callers cannot use cookies portably; for them, JWT plus refresh-token rotation
is the standard. The two schemes coexist behind the same authorisation policies — downstream
services do not care which mode the caller chose.

**Why refresh-token reuse detection?** Pure-bearer flows cannot tell a thief from a victim if both
present the same refresh token. By revoking the entire token family on reuse, we force the
attacker out: the legitimate user will be prompted to sign in again, but they will be the one
prompting, not the attacker.

**Why audit-events as an append-only table inside the same database?** Two reasons:
(1) every sign-in / sign-out / MFA-enable / lockout has to be recoverable months later for incident
response; (2) keeping the audit log in the same database as the user table means the same EF
transaction commits both — no dual-write gap. If audit volume grows, the table is a clean source
for an ETL into the Reporting warehouse later.
