# FreshCart — Requirements

Authoritative functional and non-functional requirements per bounded context. Each requirement
carries a stable id (`FR-<service>-<n>`, `NFR-<service>-<n>`) that is referenced from the
implementation tests in `<Service>.Tests/Traceability.cs` so the traceability matrix at the
bottom of this file stays accurate as the codebase grows.

Conventions:

- Requirements are written in the **shall / must** style.
- Acceptance criteria are kept short and verifiable. A criterion that cannot be expressed as a
  pass-or-fail test is rewritten until it can.
- Non-functional requirements include the measurement method, not just the target. A target
  without a measurement is aspirational, not a requirement.

---

## 1. Identity

### Functional requirements

| ID | Requirement | Acceptance |
|---|---|---|
| FR-IDN-01 | The service shall let a visitor register a customer account by submitting email, display name and password. | `POST /auth/sign-up` returns `201 Created` with the new user id. A row exists in `identity.AspNetUsers`. |
| FR-IDN-02 | The service shall reject sign-up when the email is already registered. | Second `POST /auth/sign-up` with the same email returns `409 Conflict`. |
| FR-IDN-03 | The service shall enforce password policy: minimum 12 characters, three of {lower, upper, digit, symbol}. | `POST /auth/sign-up` with a weak password returns `400 Bad Request` with a `validationErrors.Password` entry. |
| FR-IDN-04 | The service shall authenticate a registered user by email and password. | `POST /auth/sign-in` with valid credentials returns `200 OK`. |
| FR-IDN-05 | The service shall issue an HttpOnly + Secure + SameSite=Strict session cookie when the caller opts into cookie mode. | After cookie-mode sign-in the response carries `Set-Cookie: FreshCart.Session; HttpOnly; Secure; SameSite=Strict`. |
| FR-IDN-06 | The service shall issue an access token + refresh token when the caller opts into bearer mode. | After bearer-mode sign-in the response body carries `accessToken` and `refreshToken`. |
| FR-IDN-07 | The service shall rotate the refresh token on every refresh and revoke the previous one. | After `POST /auth/refresh` the previously presented token returns `403 Forbidden`. |
| FR-IDN-08 | The service shall detect refresh-token reuse and revoke the entire token family for that user. | After replaying a rotated token, all sibling tokens move to `RevokedOnUtc`. |
| FR-IDN-09 | The service shall lock the account after five failed sign-in attempts for fifteen minutes. | Six consecutive bad-password sign-ins produce `403 Forbidden` with `Title="ForbiddenException"`. |
| FR-IDN-10 | The service shall support TOTP-based multi-factor authentication. | When MFA is enabled, sign-in without a six-digit `MultiFactorCode` returns `400 Bad Request`. |
| FR-IDN-11 | The service shall record every authentication event in an append-only audit table. | After `POST /auth/sign-in` a row exists in `identity.AuditEvents` with `EventType = auth.sign-in.succeeded`. |
| FR-IDN-12 | The service shall expose the current user's profile to authenticated callers. | `GET /account/me` returns `AuthenticationProfile` with email, display name, roles and MFA flag. |
| FR-IDN-13 | The service shall let the current user sign out and invalidate all sessions and refresh tokens. | After `POST /auth/sign-out` the prior session cookie and every refresh token become invalid. |
| FR-IDN-14 | The service shall seed three demo accounts (Customer, SupportAgent, Administrator) when `ASPNETCORE_ENVIRONMENT = Development`. | After first start in Development the three accounts can sign in with the credentials documented in `README.md`. |
| FR-IDN-15 | The seeder shall refuse to run in Staging or Production. | When the environment is not Development the seeder logs a warning and exits. |

### Non-functional requirements

| ID | Requirement | Target | Measurement |
|---|---|---|---|
| NFR-IDN-01 | Sign-in p95 latency | < 300 ms | k6 script `tests/load/identity-sign-in.js` at 200 req/s steady-state |
| NFR-IDN-02 | Password hashing algorithm | Argon2id, 64 MiB memory, 3 iterations, parallelism 4 | Verified by `Argon2PasswordHasherTests.ProducedHashEncodesArgon2idParameters` |
| NFR-IDN-03 | Session cookie security flags | HttpOnly + Secure + SameSite=Strict | Verified by `SignUpAndSignInIntegrationTests` |
| NFR-IDN-04 | Refresh-token storage | SHA-256 hash; plaintext never persisted | Code review + `RefreshTokenServiceTests.RotateOnlyReturnsPlaintextOnce` |
| NFR-IDN-05 | Horizontal scalability | Three replicas under k6 load with zero error-rate | k6 + Helm chart `replicaCount: 3` |
| NFR-IDN-06 | Availability | 99.9% monthly | Azure Monitor availability test against `/health` |
| NFR-IDN-07 | OWASP A07 coverage | Lockout, MFA, HttpOnly+Secure+SameSite cookies, anti-forgery | Mapped in `docs/adr/ADR-0004-owasp-top-10-control-mapping.md` |
| NFR-IDN-08 | Audit log retention | 365 days minimum | Database backup + retention policy in `infra/modules/sql.bicep` |

---

## 2. Catalog

### Functional requirements

| ID | Requirement | Acceptance |
|---|---|---|
| FR-CAT-01 | Customers shall browse all active products. | `GET /products` returns `200 OK` with a paged result. |
| FR-CAT-02 | Customers shall search products by free text. | `GET /products?search=apple` returns matching products ordered by relevance. |
| FR-CAT-03 | Customers shall filter products by category and brand. | `GET /products?category=Beverages&brand=Acme` returns only matching items. |
| FR-CAT-04 | Administrators shall create, update and delete products. | Authenticated POST/PUT/DELETE on `/products` succeed for Administrator role and return `403` for other roles. |
| FR-CAT-05 | The service shall emit `ProductPriceChangedIntegrationEvent` when a product's price changes. | After PUT with a different `Price`, the event appears on the bus within 30 seconds. |
| FR-CAT-06 | The service shall accept signed-URL uploads of product images to Blob Storage. | `POST /products/{id}/image-upload-url` returns a 15-minute SAS URI scoped to write a single blob. |

### Non-functional requirements

| ID | Requirement | Target | Measurement |
|---|---|---|---|
| NFR-CAT-01 | Read p95 latency | < 80 ms | k6 at 500 req/s |
| NFR-CAT-02 | HybridCache hit ratio | > 90% in steady state | Custom meter `freshcart.catalog.cache.hit-ratio` in Grafana |
| NFR-CAT-03 | Idempotent product create | Duplicate `Idempotency-Key` returns the original 201 response | Integration test `CreateProductIsIdempotentByKey` |
| NFR-CAT-04 | Search availability degrades gracefully | When Postgres is unavailable the service returns the last known cached page | Polly fallback policy |

---

## 3. Pricing

### Functional requirements

| ID | Requirement | Acceptance |
|---|---|---|
| FR-PRC-01 | Callers shall request a priced basket. | gRPC `GetPriceQuote(BasketSnapshot)` returns the priced lines including discounts and totals. |
| FR-PRC-02 | Callers shall validate a coupon. | gRPC `ValidateCoupon(CouponCode)` returns active state and applicable scope. |
| FR-PRC-03 | The admin UI shall observe live price changes. | Server-streaming RPC `StreamLivePrices` emits a new message every time a rule is published. |

### Non-functional requirements

| ID | Requirement | Target | Measurement |
|---|---|---|---|
| NFR-PRC-01 | Quote p99 latency | < 25 ms | Internal benchmark + Grafana panel |
| NFR-PRC-02 | Proto evolution | Additive only; never renumber a field | Reviewed in PR by `CODEOWNERS` |
| NFR-PRC-03 | Browser fallback | gRPC-Web supported on the same port | Integration test from Angular client |

---

## 4. Basket

### Functional requirements

| ID | Requirement | Acceptance |
|---|---|---|
| FR-BSK-01 | The customer shall add an item to the basket. | `POST /basket/items` returns the updated basket; HybridCache + Postgres both updated. |
| FR-BSK-02 | The customer shall change the quantity of an item. | `PATCH /basket/items/{productId}` updates quantity; emits no event. |
| FR-BSK-03 | The customer shall remove an item. | `DELETE /basket/items/{productId}` removes the line. |
| FR-BSK-04 | The basket shall be priced server-side. | After every mutation, line totals reflect Pricing-service quote. |
| FR-BSK-05 | The basket shall consume `ProductPriceChangedIntegrationEvent` and refresh affected lines. | After consuming the event the cached basket reflects the new price. |
| FR-BSK-06 | Checkout shall publish `BasketCheckoutIntegrationEvent` via the outbox. | After `POST /basket/checkout` the event reaches the Ordering service within 30 seconds; the basket is deleted in the same transaction. |

### Non-functional requirements

| ID | Requirement | Target | Measurement |
|---|---|---|---|
| NFR-BSK-01 | Read p95 latency | < 30 ms (HybridCache hit path) | k6 |
| NFR-BSK-02 | Exactly-once event delivery on checkout | Transactional outbox + inbox in Ordering | Integration test `CheckoutPublishesExactlyOnce` |
| NFR-BSK-03 | Basket TTL | 30 days; abandoned-cart event at 24 hours | BackgroundService + test |

---

## 5. Ordering

### Functional requirements

| ID | Requirement | Acceptance |
|---|---|---|
| FR-ORD-01 | The service shall start a checkout saga on `BasketCheckoutIntegrationEvent`. | Saga state `Started` appears in the SQL table within 5 seconds of the event. |
| FR-ORD-02 | The saga shall reserve stock in Inventory. | After `ReserveStock` succeeds the saga transitions to `StockReserved`. |
| FR-ORD-03 | The saga shall charge the payment intent. | After `ChargePayment` succeeds the saga transitions to `PaymentCaptured`. |
| FR-ORD-04 | The saga shall reserve a delivery slot. | After `ReserveDeliverySlot` succeeds the saga transitions to `DeliveryReserved`. |
| FR-ORD-05 | The saga shall confirm the order. | After `ConfirmOrder` the saga transitions to `Confirmed` and emits `OrderConfirmedIntegrationEvent`. |
| FR-ORD-06 | Each saga step shall have a compensating action. | Manually-injected failure at any step rolls back state and emits a compensation event. |
| FR-ORD-07 | The customer shall view their orders. | `GET /orders` returns the caller's orders, Dapper-backed. |
| FR-ORD-08 | The customer shall view a single order by id. | `GET /orders/{id}` returns the order or `404`. |

### Non-functional requirements

| ID | Requirement | Target | Measurement |
|---|---|---|---|
| NFR-ORD-01 | Checkout end-to-end p95 | < 1.5 s (gateway to confirmation) | k6 |
| NFR-ORD-02 | Saga durability | Survives pod restart with no in-flight state loss | Test that kills the pod mid-saga |
| NFR-ORD-03 | Domain-event publication | Exactly-once via outbox + dispatch interceptor on `SaveChanges` | Integration test `OrderConfirmedEmitsOnceAcrossRetries` |

---

## 6. Inventory

| ID | Requirement | Acceptance |
|---|---|---|
| FR-INV-01 | gRPC `ReserveStock(orderId, lines)` shall atomically decrement on-hand stock for every line. | Single SQL transaction; partial reservation is never visible. |
| FR-INV-02 | gRPC `ReleaseStock(orderId)` shall reverse a prior reservation. | After release the on-hand count returns to the original value. |
| FR-INV-03 | REST `POST /replenishment` shall let an administrator top up stock. | New `on_hand` value persisted; `StockReplenishedIntegrationEvent` published. |
| FR-INV-04 | The service shall emit `LowStockIntegrationEvent` when `on_hand <= reorder_threshold`. | The event appears on the bus within 30 seconds of the threshold being crossed. |

| ID | Non-functional | Target | Measurement |
|---|---|---|---|
| NFR-INV-01 | Reservation p99 latency | < 50 ms | k6 |
| NFR-INV-02 | Consistency | Strong (row-level lock) | Integration test under contention |

---

## 7. Payment

| ID | Requirement | Acceptance |
|---|---|---|
| FR-PAY-01 | The service shall create a payment intent for an order. | Returns a tokenised intent; never stores PAN. |
| FR-PAY-02 | The service shall capture a payment intent. | Emits `PaymentCapturedIntegrationEvent`. |
| FR-PAY-03 | The service shall refund a captured payment. | Emits `PaymentRefundedIntegrationEvent`. |
| FR-PAY-04 | Every state change shall be persisted as an event in MongoDB. | Append-only `payment_events` collection. |
| FR-PAY-05 | The read projection shall rebuild from the event log on demand. | `dotnet run -- --rebuild-projection` produces identical state. |

| ID | Non-functional | Target | Measurement |
|---|---|---|---|
| NFR-PAY-01 | PCI scope | No PAN in any store or log; only tokens | Static analysis + manual review |
| NFR-PAY-02 | Gateway mTLS | Mutual TLS to the external gateway | Pipeline secret + cert pinning |
| NFR-PAY-03 | Idempotency | `Idempotency-Key` required on every mutating endpoint | Integration test |

---

## 8. Delivery

| ID | Requirement | Acceptance |
|---|---|---|
| FR-DEL-01 | The service shall reserve a delivery slot for an order. | Returns the chosen slot; slot capacity decremented. |
| FR-DEL-02 | The service shall plan an optimal route per driver per day. | Background service runs at 04:00 UTC and writes routes to MongoDB. |
| FR-DEL-03 | The driver app shall poll assigned stops. | `GET /drivers/{id}/stops` returns the day's stops. |
| FR-DEL-04 | The driver shall report a stop completion. | `POST /stops/{id}/complete` updates state and emits a notification event. |

| ID | Non-functional | Target | Measurement |
|---|---|---|---|
| NFR-DEL-01 | Slot query p95 | < 200 ms with 100 km geo radius | k6 |
| NFR-DEL-02 | External maps provider isolated | Single adapter, swappable in one file | Code review |

---

## 9. Notification

| ID | Requirement | Acceptance |
|---|---|---|
| FR-NOT-01 | The service shall consume every `Order*`, `Payment*` and `Delivery*` integration event. | Consumer endpoint receives the event within 5 seconds. |
| FR-NOT-02 | The service shall resolve recipient preferences before dispatching. | Caller-defined preferences honoured (in-app default). |
| FR-NOT-03 | The service shall push to connected browsers via SignalR. | The notification toast appears on the target client within 1 second of consumption. |
| FR-NOT-04 | The service shall persist a notification record in Cosmos DB. | Record exists with channel + delivery status. |

| ID | Non-functional | Target | Measurement |
|---|---|---|---|
| NFR-NOT-01 | Backplane | Redis, cross-pod fan-out | Multi-pod integration test |
| NFR-NOT-02 | Event-to-push p95 | < 1 s | Grafana panel `freshcart.notification.delivery-latency` |

---

## 10. CustomerSupport

| ID | Requirement | Acceptance |
|---|---|---|
| FR-SUP-01 | The customer shall open a chat session. | WebSocket handshake completes within 1 second. |
| FR-SUP-02 | The system shall route the session to the first available agent in the customer's region. | Test agent receives the join event. |
| FR-SUP-03 | The agent shall see all active chats in their queue. | Admin SPA renders the queue updated in real time. |
| FR-SUP-04 | The full transcript shall be persisted to MongoDB. | After session close the transcript can be replayed. |

| ID | Non-functional | Target | Measurement |
|---|---|---|---|
| NFR-SUP-01 | Sticky sessions on AKS | Session affinity configured on the Ingress | Helm values |
| NFR-SUP-02 | Concurrent chats per pod | 500 with no message loss | k6 WebSocket script |

---

## 11. Reviews

| ID | Requirement | Acceptance |
|---|---|---|
| FR-REV-01 | Authenticated customers shall post one review per product per order. | Second `POST /reviews` for the same product+order returns `409`. |
| FR-REV-02 | Reviews shall pass through a moderation pipeline before being published. | New review state is `Pending` until an administrator approves. |
| FR-REV-03 | The service shall expose average rating and review count per product. | `GET /products/{id}/rating-summary` returns the aggregate. |

---

## 12. Reporting

### Functional requirements

| ID | Requirement | Acceptance |
|---|---|---|
| FR-REP-01 | The service shall return the headline sales KPIs for any period. | `GET /dashboards/sales/overview?preset=Last30Days` returns the tiles for current + previous period. |
| FR-REP-02 | The service shall return the time-series for a sales-trend chart. | `GET /dashboards/sales/time-series?bucket=Daily` returns one row per bucket. |
| FR-REP-03 | The service shall return revenue split by category and by payment method. | `GET /dashboards/sales/breakdown` returns both arrays in one round trip. |
| FR-REP-04 | The service shall return the top-N selling or slow-moving products. | `GET /reports/products/top?take=20&mode=BestSellers` returns the ranking. |
| FR-REP-05 | The service shall return the top-N customers by lifetime value. | `GET /reports/customers/leaderboard?take=20` returns the ranking. |
| FR-REP-06 | The service shall report inventory health. | `GET /dashboards/inventory/health` returns SKU counts and value-at-cost. |
| FR-REP-07 | The service shall report delivery performance. | `GET /dashboards/delivery/performance?preset=Last30Days` returns on-time / late / failed counts. |
| FR-REP-08 | The service shall generate a PDF invoice for any confirmed order, idempotent per order id. | `POST /invoices` returns the same invoice number on retry; PDF is stored once. |
| FR-REP-09 | Invoice numbers shall be gap-free per (year, kind). | Allocated under a database row lock; concurrent allocations produce strictly monotonic sequence. |
| FR-REP-10 | The service shall return a signed-URL download for any existing invoice. | `GET /invoices/{number}` returns a 15-minute SAS URI. |
| FR-REP-11 | The service shall stream the invoice PDF for callers that prefer not to use SAS URIs. | `GET /invoices/{number}/content.pdf` returns the bytes with `Content-Type: application/pdf`. |
| FR-REP-12 | The service shall export the daily-bucketed transactions for any period as Excel. | `GET /exports/sales-transactions.xlsx` returns a styled workbook. |
| FR-REP-13 | The service shall write yesterday's sales summary to Blob Storage exactly once per day. | `DailySalesReportBackgroundService` writes `scheduled-reports/daily/<yyyy-MM-dd>.xlsx` and skips if it already exists. |
| FR-REP-14 | The service shall consume `OrderConfirmedIntegrationEvent` and update the warehouse. | Within 30 seconds of the event the sales facts reflect the new order. |
| FR-REP-15 | The service shall consume `OrderRefundedIntegrationEvent` and subtract the refund. | Within 30 seconds the `RefundTotal` for the order's day is incremented and `NetRevenue` is decremented. |

### Non-functional requirements

| ID | Requirement | Target | Measurement |
|---|---|---|---|
| NFR-REP-01 | Dashboard p95 latency | < 200 ms | k6 |
| NFR-REP-02 | Invoice generation p99 | < 800 ms (PDF render + Blob upload) | k6 |
| NFR-REP-03 | Excel export p95 | < 1.5 s for a 90-day daily-bucketed report | k6 |
| NFR-REP-04 | Projection idempotency | Replaying the same event twice produces identical state | `WarehouseProjectionWriterTests.ReplayProducesIdenticalState` |
| NFR-REP-05 | Authorisation | Every endpoint requires `BackOfficeUser` role (Administrator + SupportAgent + Manager) | Verified by `DashboardEndpointsRequireBackOfficeRoleTests` |
| NFR-REP-06 | Storage authentication | Workload Identity in cluster; connection string only in dev | Bicep + Helm review |

---

## 13. Cross-cutting

| ID | Requirement | Target | Measurement |
|---|---|---|---|
| NFR-CC-01 | Transport security | HTTPS only; HSTS preload; TLS 1.3 minimum | Front Door config + integration test |
| NFR-CC-02 | Observability | Every request carries a `traceparent` header and lands in App Insights | Integration test asserts trace context propagation |
| NFR-CC-03 | Error format | RFC 7807 ProblemDetails on every error response | `CustomExceptionHandlerTests` |
| NFR-CC-04 | Anti-forgery | Double-submit XSRF on every state-changing endpoint behind cookie auth | Integration test |
| NFR-CC-05 | SSRF defence | Outbound HTTP rejects non-allow-listed hosts | `OutboundUrlAllowListHandlerTests` |
| NFR-CC-06 | Secret storage | Azure Key Vault via Workload Identity in cluster; user-secrets locally | Bicep review + secret-scanning in CI |
| NFR-CC-07 | Dependency hygiene | Weekly Dependabot + `dotnet list package --vulnerable` in CI | `.github/dependabot.yml` |
| NFR-CC-08 | Image integrity | Cosign-signed images verified by admission controller in prod | CI + admission policy |
| NFR-CC-09 | Test coverage | Line coverage on Domain + Application + Behaviors > 85% | Coverlet + SonarCloud quality gate |
| NFR-CC-10 | Build hygiene | `TreatWarningsAsErrors = true`; Roslyn analyzers run on every build | `Directory.Build.props` |

---

## Traceability matrix

| Requirement | Implementation | Test |
|---|---|---|
| FR-IDN-01 | `SignUpCommandHandler` | `SignUpAndSignInIntegrationTests.SignUpThenSignInIssuesSessionCookieAndAllowsProtectedAccess` |
| FR-IDN-04 | `SignInCommandHandler` | `SignUpAndSignInIntegrationTests.SignUpThenSignInIssuesSessionCookieAndAllowsProtectedAccess` |
| FR-IDN-05 | `AuthenticationConfiguration` | `SignUpAndSignInIntegrationTests` (asserts cookie flags) |
| FR-IDN-07 | `RefreshTokenService.RotateAsync` | `RefreshTokenServiceTests.RotationRevokesPreviousToken` |
| FR-IDN-08 | `RefreshTokenService.RotateAsync` | `RefreshTokenServiceTests.ReuseOfRotatedTokenRevokesFamily` |
| FR-IDN-14 | `IdentityDataSeeder` | `IdentityDataSeederTests.SeedsThreeDemoAccountsOnFirstStart` |
| FR-IDN-15 | `IdentityDataSeeder` | `IdentityDataSeederTests.SkipsSeedingWhenNotDevelopment` |
| NFR-IDN-02 | `Argon2PasswordHasher` | `Argon2PasswordHasherTests.ProducedHashEncodesArgon2idParameters` |
| FR-REP-08 | `GenerateInvoiceCommandHandler` | `GenerateInvoiceCommandHandlerTests.ReturnsExistingInvoiceOnSecondCall` |
| FR-REP-09 | `InvoiceRepository.AllocateNextNumberAsync` | `InvoiceNumberTests.AllocateAndParseRoundTrip` |
| FR-REP-14 | `OrderConfirmedProjectionConsumer` | `WarehouseProjectionWriterTests.AppliesOrderConfirmedIdempotently` |
| NFR-CC-03 | `CustomExceptionHandler` | `CustomExceptionHandlerTests.MapsKnownExceptionsToProblemDetails` |
| NFR-CC-05 | `OutboundUrlAllowListHandler` | `OutboundUrlAllowListHandlerTests.BlocksHostNotOnAllowList` |
