# FreshCart end-to-end tests (Playwright)

Browser-driven happy-path coverage for the customer storefront and a smoke-test for the admin
dashboard. Runs against any deployed environment — pass the URL via `PLAYWRIGHT_BASE_URL`.

## Suites

| File | What it covers |
|---|---|
| `specs/auth.spec.ts` | Sign-up → HttpOnly+Secure+SameSite=Strict session cookie issued → XSRF token present → sign-out → sign-in |
| `specs/customer-journey.spec.ts` | Sign-up → add to basket → checkout → order confirmation → SignalR notification toast appears |
| `specs/reporting-dashboard.spec.ts` | Admin sign-in → sales overview tiles + trend chart + export button (skipped unless `ADMIN_BASE_URL` is set) |

## Running locally

```bash
# In the storefront checkout (keep this process running).
npm start -- --host 127.0.0.1 --port 4200 --ssl true

# In this checkout.
cd tests/e2e
npm ci
npm run install:browsers
export PLAYWRIGHT_BASE_URL=https://localhost:4200
npm test
```

The CI stack uses the same HTTPS setup because the session cookie is `Secure` and WebKit rejects
that cookie on plain HTTP localhost. Playwright already ignores the local development certificate.
Plain HTTP remains compatible with flows that do not depend on the session cookie, but it cannot
satisfy this full WebKit authentication suite.

Open the HTML report:

```bash
npm run report
```

Generate a new spec interactively:

```bash
npm run codegen
```

## Running in CI

The repo ships `.github/workflows/e2e-playwright.yml` — it runs all three browser projects in
parallel and uploads the HTML report as a workflow artifact on every PR. Nightly runs target
the staging environment.
