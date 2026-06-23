# ADR-0002 — Cookie-first authentication for browsers, JWT for everything else

- **Status:** Accepted
- **Date:** 2026-05-25
- **Author:** Ama Senevirathne

## Context

The platform must support two distinct caller shapes:

1. **The Angular customer SPA + admin SPA running in a browser.** XSS-driven token theft is the
   dominant risk; browsers persist cookies safely and the SPA owns no token storage.
2. **Mobile clients and other microservices.** Cookies do not make sense for these — they need
   short-lived bearer tokens they can attach to outbound requests, plus a refresh-token rotation
   flow to avoid re-prompting the user.

We need a single authentication boundary (the Identity service) that can serve both, and a
gateway that can carry both modes onward without forcing every downstream service to validate two
schemes.

## Decision

The Identity service exposes both schemes and lets the caller pick:

- **Cookie scheme** (`FreshCart.Session`): HttpOnly, Secure, SameSite=Strict, sliding 8-hour
  expiry. Default for the Angular SPAs. Anti-forgery is enforced via the
  `XSRF-TOKEN` / `X-XSRF-TOKEN` double-submit pattern.
- **JWT bearer scheme**: HS256-signed access token (15-minute lifetime), refresh-token rotation
  every refresh (14-day lifetime). Refresh tokens are stored as SHA-256 hashes; reuse of a revoked
  token revokes the entire family for that user.

Both schemes are exposed as ASP.NET Core authentication schemes (`Cookies`, `Bearer`). Per-endpoint
authorisation policies (`Customer`, `SupportAgent`, `Administrator`) require an authenticated user
in either scheme — the gateway and downstream services do not need to know which mode the caller
chose.

Passwords are hashed with **Argon2id** (memory-hard, 64 MiB memory, 3 iterations, parallelism 4)
via a custom `Argon2PasswordHasher<TUser>` that replaces the PBKDF2 default. Existing users are
re-hashed transparently on next sign-in if parameters change.

## Consequences

**Positive**

- Browser sessions never see a JWT in JavaScript-reachable storage — eliminating the most common
  XSS-to-account-takeover path.
- Mobile + service-to-service callers get the standard bearer flow they expect.
- Refresh-token reuse detection adds a thief-vs-victim signal that pure-bearer flows do not have.
- Argon2id makes offline GPU-based password cracking materially more expensive than PBKDF2.

**Negative**

- Two schemes to test, document and rotate keys for. Mitigated by Testcontainers integration tests
  that exercise both paths.
- Anti-forgery double-submit is one more moving part than pure same-origin XSRF protection. The
  XSRF-TOKEN cookie is refreshed lazily by the SPA via `GET /auth/anti-forgery-token`.

## Alternatives considered

- **JWT only.** Rejected — token theft via XSS would put the access token directly in
  attacker-readable storage.
- **Cookies only.** Rejected — mobile + service-to-service callers cannot use cookies portably.
- **PASETO instead of JWT.** Considered. JWT is the established standard for ASP.NET Core; the
  PASETO ecosystem is still thin on .NET. Revisit when `Microsoft.IdentityModel` ships first-class
  PASETO support.

## References

- OWASP Authentication Cheat Sheet
- ASP.NET Core 10 cookie + JWT hybrid guide
- 2026 best-practice: [`medium.com/@bertoneill/asp-net-core-identity-apis-…`](https://medium.com/@bertoneill/asp-net-core-identity-apis-with-jwt-refresh-httponly-cookies-role-based-security-within-a-bf1de86744b9)
