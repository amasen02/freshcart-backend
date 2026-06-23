# FreshCart Class Diagrams

Mermaid class diagrams for the implemented services. Render with any Mermaid-aware viewer
(GitHub markdown does it natively).

The diagrams show the dependency direction: every arrow `A --> B` means "A depends on B".
For the Clean Architecture services the arrows therefore all point inward.

---

## BuildingBlocks

```mermaid
classDiagram
    class ICommand~TResponse~ {
        <<interface>>
    }
    class ICommandHandler~TCommand_TResponse~ {
        <<interface>>
        +Handle(TCommand, CancellationToken) Task~TResponse~
    }
    class IQuery~TResponse~ {
        <<interface>>
    }
    class IQueryHandler~TQuery_TResponse~ {
        <<interface>>
        +Handle(TQuery, CancellationToken) Task~TResponse~
    }
    class ValidationBehavior~TRequest_TResponse~ {
        -IEnumerable~IValidator~ validators
        +Handle(TRequest, RequestHandlerDelegate, CancellationToken) Task~TResponse~
    }
    class LoggingBehavior~TRequest_TResponse~ {
        -ILogger logger
        +Handle(TRequest, RequestHandlerDelegate, CancellationToken) Task~TResponse~
    }
    class CustomExceptionHandler {
        -ILogger logger
        +TryHandleAsync(HttpContext, Exception, CancellationToken) ValueTask~bool~
    }
    class DomainException
    class NotFoundException
    class BadRequestException
    class ConflictException
    class ForbiddenException
    class InternalServerException
    class PaginationRequest {
        +int PageNumber
        +int PageSize
        +Normalise() PaginationRequest
    }
    class PaginatedResult~TItem~ {
        +int PageNumber
        +int PageSize
        +long TotalItemCount
        +IReadOnlyList~TItem~ Items
    }
    class OutboundUrlAllowListHandler {
        -HashSet~string~ allowedHosts
        #SendAsync(HttpRequestMessage, CancellationToken) Task~HttpResponseMessage~
    }

    ICommandHandler~TCommand_TResponse~ ..|> ICommand~TResponse~
    IQueryHandler~TQuery_TResponse~ ..|> IQuery~TResponse~
    DomainException --|> Exception
    NotFoundException --|> Exception
    BadRequestException --|> Exception
    ConflictException --|> Exception
    ForbiddenException --|> Exception
    InternalServerException --|> Exception
    CustomExceptionHandler ..> NotFoundException
    CustomExceptionHandler ..> BadRequestException
    CustomExceptionHandler ..> ConflictException
    CustomExceptionHandler ..> ForbiddenException
    CustomExceptionHandler ..> InternalServerException
    CustomExceptionHandler ..> DomainException
```

---

## Identity service

### Domain

```mermaid
classDiagram
    class ApplicationUser {
        +string DisplayName
        +bool MarketingConsent
        +DateTimeOffset CreatedOnUtc
        +DateTimeOffset? LastSignInOnUtc
        +DateTimeOffset SecurityStampUpdatedOnUtc
        +RecordSuccessfulSignIn(DateTimeOffset)
        +InvalidateExistingSessions(DateTimeOffset)
    }
    class ApplicationRole {
        +string? Description
        +DateTimeOffset CreatedOnUtc
    }
    class RefreshToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTimeOffset ExpiresOnUtc
        +DateTimeOffset? RevokedOnUtc
        +string? ReplacedByTokenHash
        +bool IsActive
        +Revoke(string, string?, DateTimeOffset)
    }
    class AuditEvent {
        +Guid Id
        +string EventType
        +Guid? UserId
        +string Description
        +string? IpAddress
        +DateTimeOffset OccurredOnUtc
    }
    class CanonicalRoles {
        <<static>>
        +Customer$
        +SupportAgent$
        +Administrator$
    }
    class AuditEventType {
        <<static>>
        +SignUpSucceeded$
        +SignInSucceeded$
        +SignInFailed$
        +SignedOut$
        +RefreshTokenIssued$
        +RefreshTokenRevoked$
        +PasswordResetRequested$
        +PasswordResetCompleted$
        +MultiFactorEnabled$
        +MultiFactorDisabled$
        +AccountLockedOut$
    }
    ApplicationUser --|> IdentityUser
    ApplicationRole --|> IdentityRole
```

### Application

```mermaid
classDiagram
    class SignUpCommand {
        +string Email
        +string Password
        +string DisplayName
        +bool MarketingConsent
        +bool SignInImmediately
    }
    class SignUpCommandHandler {
        +Handle(SignUpCommand, CancellationToken) Task~SignUpResult~
    }
    class SignInCommand {
        +string Email
        +string Password
        +string? MultiFactorCode
        +bool UseCookie
        +bool RememberMe
    }
    class SignInCommandHandler {
        +Handle(SignInCommand, CancellationToken) Task~SignInResult~
    }
    class SignOutCommand {
        +Guid UserId
    }
    class SignOutCommandHandler
    class RefreshAccessTokenCommand {
        +string RefreshToken
    }
    class RefreshAccessTokenCommandHandler
    class GetCurrentUserQuery {
        +Guid UserId
    }
    class GetCurrentUserQueryHandler
    class IAccessTokenIssuer {
        <<interface>>
        +Issue(ApplicationUser, IReadOnlyCollection~string~) AccessTokenIssueResult
    }
    class IRefreshTokenService {
        <<interface>>
        +IssueAsync(Guid, string?, string?, CancellationToken) Task~RefreshTokenIssueResult~
        +RotateAsync(string, string?, string?, CancellationToken) Task~RefreshTokenRotateResult~
        +RevokeAllForUserAsync(Guid, string, CancellationToken) Task
    }
    class IIdentityAuditLog {
        <<interface>>
        +RecordAsync(AuditEvent, CancellationToken) Task
    }
    class ICurrentRequestContext {
        <<interface>>
        +string? IpAddress
        +string? UserAgent
        +string? CorrelationId
        +Guid? AuthenticatedUserId
    }
    class AuthenticationProfile {
        +Guid UserId
        +string Email
        +string DisplayName
        +IReadOnlyCollection~string~ Roles
        +bool MultiFactorEnabled
    }

    SignUpCommandHandler ..> IAccessTokenIssuer
    SignUpCommandHandler ..> IRefreshTokenService
    SignUpCommandHandler ..> IIdentityAuditLog
    SignUpCommandHandler ..> ICurrentRequestContext
    SignInCommandHandler ..> IAccessTokenIssuer
    SignInCommandHandler ..> IRefreshTokenService
    SignInCommandHandler ..> IIdentityAuditLog
    SignInCommandHandler ..> ICurrentRequestContext
    RefreshAccessTokenCommandHandler ..> IAccessTokenIssuer
    RefreshAccessTokenCommandHandler ..> IRefreshTokenService
    SignOutCommandHandler ..> IRefreshTokenService
    SignOutCommandHandler ..> IIdentityAuditLog
```

### Infrastructure

```mermaid
classDiagram
    class IdentityDbContext {
        +DbSet~RefreshToken~ RefreshTokens
        +DbSet~AuditEvent~ AuditEvents
    }
    class Argon2PasswordHasher~TUser~ {
        +HashPassword(TUser, string) string
        +VerifyHashedPassword(TUser, string, string) PasswordVerificationResult
    }
    class JwtAccessTokenIssuer {
        +Issue(ApplicationUser, IReadOnlyCollection~string~) AccessTokenIssueResult
    }
    class RefreshTokenService {
        +IssueAsync(Guid, string?, string?, CancellationToken) Task~RefreshTokenIssueResult~
        +RotateAsync(string, string?, string?, CancellationToken) Task~RefreshTokenRotateResult~
        +RevokeAllForUserAsync(Guid, string, CancellationToken) Task
    }
    class EntityFrameworkAuditLog {
        +RecordAsync(AuditEvent, CancellationToken) Task
    }
    class IdentityDataSeeder {
        +StartAsync(CancellationToken) Task
    }
    class JwtIssuerOptions {
        +string Issuer
        +string Audience
        +string SigningKey
        +TimeSpan AccessTokenLifetime
        +TimeSpan RefreshTokenLifetime
    }

    Argon2PasswordHasher~TUser~ ..|> IPasswordHasher~TUser~
    JwtAccessTokenIssuer ..|> IAccessTokenIssuer
    RefreshTokenService ..|> IRefreshTokenService
    EntityFrameworkAuditLog ..|> IIdentityAuditLog
    IdentityDataSeeder ..|> IHostedService
    JwtAccessTokenIssuer ..> JwtIssuerOptions
    RefreshTokenService ..> JwtIssuerOptions
    RefreshTokenService ..> IdentityDbContext
    EntityFrameworkAuditLog ..> IdentityDbContext
```

### API

```mermaid
classDiagram
    class AuthenticationEndpoints
    class AccountEndpoints
    class HttpContextCurrentRequestContext
    class AuthenticationConfiguration {
        <<static>>
        +AddFreshCartAuthentication(IServiceCollection, IConfiguration) IServiceCollection
        +SessionCookieName$
    }
    class AntiforgeryConfiguration {
        <<static>>
        +AddFreshCartAntiforgery(IServiceCollection) IServiceCollection
        +IssueAntiforgeryCookie(HttpContext, IAntiforgery)
        +ClientReadableCookieName$
        +ClientHeaderName$
    }

    AuthenticationEndpoints ..|> ICarterModule
    AccountEndpoints ..|> ICarterModule
    HttpContextCurrentRequestContext ..|> ICurrentRequestContext
```

---

## Reporting service

### Domain

```mermaid
classDiagram
    class Invoice {
        +Guid Id
        +string InvoiceNumber
        +InvoiceKind Kind
        +Guid OrderId
        +Guid CustomerId
        +string CustomerEmail
        +string CustomerDisplayName
        +InvoiceAddress BillingAddress
        +InvoiceAddress ShippingAddress
        +IReadOnlyList~InvoiceLine~ Lines
        +decimal Subtotal
        +decimal DiscountTotal
        +decimal TaxTotal
        +decimal ShippingTotal
        +decimal GrandTotal
        +string CurrencyCode
        +DateTimeOffset IssuedOnUtc
    }
    class InvoiceKind {
        <<enumeration>>
        Sale
        CreditNote
        ProForma
    }
    class InvoiceAddress {
        <<record>>
        +string FullName
        +string AddressLine1
        +string? AddressLine2
        +string City
        +string? State
        +string PostalCode
        +string Country
    }
    class InvoiceLine {
        <<record>>
        +int LineNumber
        +string ProductSku
        +string ProductName
        +int Quantity
        +decimal UnitPrice
        +decimal DiscountAmount
        +decimal TaxAmount
        +decimal LineTotal
    }
    class InvoiceNumber {
        <<record struct>>
        +InvoiceKind Kind
        +int Year
        +long Sequence
        +string Value
        +Allocate(InvoiceKind, int, long)$ InvoiceNumber
        +TryParse(string, out InvoiceNumber)$ bool
    }
    class SalesSnapshot {
        <<record>>
        +DateOnly Day
        +int OrderCount
        +int UniqueCustomerCount
        +decimal GrossRevenue
        +decimal DiscountTotal
        +decimal RefundTotal
        +decimal TaxTotal
        +decimal ShippingTotal
        +decimal NetRevenue
        +decimal AverageOrderValue
        +decimal RefundRate
    }
    class ReportingPeriod {
        <<record struct>>
        +DateTimeOffset FromUtc
        +DateTimeOffset ToUtcExclusive
        +Contains(DateTimeOffset) bool
        +TimeSpan Duration
        +Today(TimeProvider)$ ReportingPeriod
        +Last30Days(TimeProvider)$ ReportingPeriod
        +MonthToDate(TimeProvider)$ ReportingPeriod
        +YearToDate(TimeProvider)$ ReportingPeriod
    }
    class AggregationBucket {
        <<enumeration>>
        Hourly
        Daily
        Weekly
        Monthly
    }
    class KpiMetric {
        <<record>>
        +string Code
        +string DisplayName
        +decimal CurrentValue
        +decimal? PreviousValue
        +KpiUnit Unit
        +decimal? DeltaPercentage
        +KpiTrend Trend
    }

    Invoice o-- InvoiceAddress
    Invoice o-- InvoiceLine
    InvoiceNumber ..> InvoiceKind
```

### Application

```mermaid
classDiagram
    class ISalesReadWarehouse {
        <<interface>>
        +GetAggregateAsync(ReportingPeriod, CancellationToken) Task~SalesSnapshot~
        +GetTimeSeriesAsync(ReportingPeriod, AggregationBucket, CancellationToken) Task~IReadOnlyList~SalesSnapshot~~
        +GetRevenueByCategoryAsync(ReportingPeriod, CancellationToken) Task~IReadOnlyList~RevenueByCategoryRow~~
        +GetRevenueByPaymentMethodAsync(ReportingPeriod, CancellationToken) Task~IReadOnlyList~RevenueByPaymentMethodRow~~
    }
    class IProductReadWarehouse {
        <<interface>>
    }
    class ICustomerReadWarehouse {
        <<interface>>
    }
    class IDeliveryReadWarehouse {
        <<interface>>
    }
    class IInvoiceRepository {
        <<interface>>
        +FindByNumberAsync(InvoiceNumber, CancellationToken) Task~Invoice?~
        +FindByOrderIdAsync(Guid, CancellationToken) Task~Invoice?~
        +AllocateNextNumberAsync(InvoiceKind, int, CancellationToken) Task~InvoiceNumber~
        +AddAsync(Invoice, CancellationToken) Task
    }
    class IInvoiceRenderer {
        <<interface>>
        +InvoiceRenderingFormat Format
        +RenderAsync(Invoice, CancellationToken) Task~RenderedDocument~
    }
    class IExcelExporter {
        <<interface>>
        +ExportTabularAsync(string, IEnumerable~TRow~, ExcelExportOptions?, CancellationToken) Task~RenderedDocument~
    }
    class IDocumentStore {
        <<interface>>
        +StoreAsync(string, string, ReadOnlyMemory~byte~, string, CancellationToken) Task~Uri~
        +OpenReadAsync(string, string, CancellationToken) Task~Stream~
        +CreateReadOnlySharedAccessSignatureAsync(string, string, TimeSpan, CancellationToken) Task~Uri~
    }
    class IProjectionInbox {
        <<interface>>
        +HasProcessedAsync(Guid, CancellationToken) Task~bool~
        +RecordProcessedAsync(Guid, CancellationToken) Task
    }
    class IProjectionWriter {
        <<interface>>
        +ApplyOrderConfirmedAsync(OrderConfirmedIntegrationEvent, CancellationToken) Task
        +ApplyOrderRefundedAsync(OrderRefundedIntegrationEvent, CancellationToken) Task
    }
    class GetSalesOverviewQueryHandler
    class GetSalesTimeSeriesQueryHandler
    class GetRevenueBreakdownQueryHandler
    class GetTopProductsQueryHandler
    class GetCustomerLeaderboardQueryHandler
    class GetInventoryHealthQueryHandler
    class GetDeliveryPerformanceQueryHandler
    class GenerateInvoiceCommandHandler
    class ExportSalesTransactionsCommandHandler
    class OrderConfirmedProjectionConsumer
    class OrderRefundedProjectionConsumer

    GetSalesOverviewQueryHandler ..> ISalesReadWarehouse
    GetSalesTimeSeriesQueryHandler ..> ISalesReadWarehouse
    GetRevenueBreakdownQueryHandler ..> ISalesReadWarehouse
    GetTopProductsQueryHandler ..> IProductReadWarehouse
    GetCustomerLeaderboardQueryHandler ..> ICustomerReadWarehouse
    GetInventoryHealthQueryHandler ..> IProductReadWarehouse
    GetDeliveryPerformanceQueryHandler ..> IDeliveryReadWarehouse
    GenerateInvoiceCommandHandler ..> IInvoiceRepository
    GenerateInvoiceCommandHandler ..> IInvoiceRenderer
    GenerateInvoiceCommandHandler ..> IDocumentStore
    ExportSalesTransactionsCommandHandler ..> ISalesReadWarehouse
    ExportSalesTransactionsCommandHandler ..> IExcelExporter
    OrderConfirmedProjectionConsumer ..> IProjectionInbox
    OrderConfirmedProjectionConsumer ..> IProjectionWriter
    OrderRefundedProjectionConsumer ..> IProjectionInbox
    OrderRefundedProjectionConsumer ..> IProjectionWriter
```

### Infrastructure

```mermaid
classDiagram
    class WarehouseDbContext {
        +DbSet~InvoiceRecord~ Invoices
        +DbSet~InvoiceLineRecord~ InvoiceLines
        +DbSet~InvoiceNumberSequence~ InvoiceNumberSequences
        +DbSet~ProjectionInboxEntry~ ProjectionInbox
    }
    class InvoiceRepository
    class DapperSalesReadWarehouse
    class DapperProductReadWarehouse
    class DapperCustomerReadWarehouse
    class DapperDeliveryReadWarehouse
    class WarehouseProjectionWriter
    class EntityFrameworkProjectionInbox
    class QuestPdfInvoiceRenderer
    class ClosedXmlExcelExporter
    class AzureBlobDocumentStore
    class DailySalesReportBackgroundService
    class MySqlWarehouseConnectionFactory
    class IWarehouseConnectionFactory {
        <<interface>>
    }

    InvoiceRepository ..|> IInvoiceRepository
    DapperSalesReadWarehouse ..|> ISalesReadWarehouse
    DapperProductReadWarehouse ..|> IProductReadWarehouse
    DapperCustomerReadWarehouse ..|> ICustomerReadWarehouse
    DapperDeliveryReadWarehouse ..|> IDeliveryReadWarehouse
    WarehouseProjectionWriter ..|> IProjectionWriter
    EntityFrameworkProjectionInbox ..|> IProjectionInbox
    QuestPdfInvoiceRenderer ..|> IInvoiceRenderer
    ClosedXmlExcelExporter ..|> IExcelExporter
    AzureBlobDocumentStore ..|> IDocumentStore
    DailySalesReportBackgroundService ..|> IHostedService
    MySqlWarehouseConnectionFactory ..|> IWarehouseConnectionFactory
    InvoiceRepository ..> WarehouseDbContext
    DapperSalesReadWarehouse ..> IWarehouseConnectionFactory
```

---

## Catalog service

Vertical Slice in a single web project; no layer split. Each feature folder owns its command or
query, handler, endpoint and validator. Marten over Postgres stores the documents; HybridCache
fronts single-product reads and the category tree.

```mermaid
classDiagram
    class Product {
        +Guid Id
        +string Name
        +string Slug
        +string Sku
        +decimal BasePrice
        +string CurrencyCode
        +Guid CategoryId
        +Guid BrandId
        +bool IsActive
        +bool IsDigital
        +List~ProductImage~ Images
        +List~ProductAttribute~ Attributes
        +int InitialStockQuantity
    }
    class Category {
        +Guid Id
        +string Name
        +string Slug
        +Guid? ParentCategoryId
        +int SortOrder
        +bool IsActive
    }
    class Brand {
        +Guid Id
        +string Name
        +string Slug
        +bool IsActive
    }
    class CreateProductCommandHandler
    class UpdateProductCommandHandler
    class GetProductQueryHandler
    class GetProductsQueryHandler
    class SearchProductsQueryHandler
    class GetCategoriesQueryHandler

    CreateProductCommandHandler ..> IDocumentSession
    CreateProductCommandHandler ..> IPublishEndpoint
    UpdateProductCommandHandler ..> IDocumentSession
    UpdateProductCommandHandler ..> IPublishEndpoint
    GetProductQueryHandler ..> HybridCache
    GetCategoriesQueryHandler ..> HybridCache
    Product o-- Category
    Product o-- Brand
```

---

## Pricing service

Plain service classes behind a gRPC facade; no CQRS, no MediatR, no events. SQLite via EF Core
holds the discount rules and coupon codes.

```mermaid
classDiagram
    class PricingGrpcService {
        +PriceBasket(PriceBasketRequest, ServerCallContext) Task~PriceBasketResponse~
        +ValidateCoupon(ValidateCouponRequest, ServerCallContext) Task~ValidateCouponResponse~
    }
    class BasketPriceCalculator {
        +CalculateAsync(BasketPriceInput, CancellationToken) Task~BasketPriceResult~
    }
    class CouponValidator {
        +ValidateAsync(string, Guid, decimal, CancellationToken) Task~CouponValidationResult~
    }
    class BasketPriceResult {
        <<record>>
    }
    class CouponValidationResult {
        <<record>>
        +Valid(...)$ CouponValidationResult
        +Invalid(string)$ CouponValidationResult
    }
    class DiscountRule {
        +Guid Id
        +Guid ProductId
        +decimal DiscountPercentage
        +DateTimeOffset ValidFromUtc
        +DateTimeOffset ValidToUtc
        +bool IsActive
    }
    class CouponCode {
        +Guid Id
        +string Code
        +CouponDiscountType DiscountType
        +decimal DiscountValue
        +decimal? MinimumOrderAmount
        +int? UsageLimit
        +int UsageCount
        +bool IsActive
    }
    class PricingDbContext

    PricingGrpcService ..> BasketPriceCalculator
    BasketPriceCalculator ..> CouponValidator
    BasketPriceCalculator ..> PricingDbContext
    BasketPriceCalculator ..> BasketPriceResult
    CouponValidator ..> CouponCode
    CouponValidator ..> CouponValidationResult
    PricingDbContext ..> DiscountRule
    PricingDbContext ..> CouponCode
```

---

## Basket service

Vertical Slice with a repository decorator and a transactional outbox over Marten. The
`CachedBasketRepository` decorates `MartenBasketRepository`; the one money-critical event is
written to the outbox in the same Marten session that archives and deletes the basket.

```mermaid
classDiagram
    class ShoppingBasket {
        +Guid Id
        +string CurrencyCode
        +List~BasketItem~ Items
        +string? CouponCode
        +DateTimeOffset UpdatedOnUtc
    }
    class BasketItem {
        +Guid ProductId
        +string ProductSku
        +string ProductName
        +decimal UnitPrice
        +int Quantity
        +bool IsDigital
    }
    class IBasketRepository {
        <<interface>>
        +GetAsync(Guid, CancellationToken) Task~ShoppingBasket?~
        +UpsertAsync(ShoppingBasket, CancellationToken) Task
        +DeleteAsync(Guid, CancellationToken) Task
        +ArchiveAsync(ArchivedBasket, CancellationToken) Task
    }
    class MartenBasketRepository
    class CachedBasketRepository
    class IBasketPricingClient {
        <<interface>>
        +PriceBasketAsync(...) Task~BasketPricingResult~
        +ValidateCouponAsync(...) Task~CouponCheckResult~
    }
    class CatalogProductClient
    class MartenOutboxStore
    class CheckoutCommandHandler
    class ProductPriceChangedConsumer

    MartenBasketRepository ..|> IBasketRepository
    CachedBasketRepository ..|> IBasketRepository
    CachedBasketRepository o-- MartenBasketRepository
    CachedBasketRepository ..> HybridCache
    CheckoutCommandHandler ..> IBasketRepository
    CheckoutCommandHandler ..> IBasketPricingClient
    CheckoutCommandHandler ..> MartenOutboxStore
    CheckoutCommandHandler ..> CatalogProductClient
    ProductPriceChangedConsumer ..> IBasketRepository
    ShoppingBasket o-- BasketItem
```

---

## Ordering service

Clean Architecture plus DDD plus a MassTransit saga state machine. The aggregate holds the
invariants; the saga holds the orchestration; work consumers hold the side effects.

### Domain

```mermaid
classDiagram
    class Order {
        +Guid Id
        +Guid CustomerId
        +OrderStatus Status
        +IReadOnlyList~OrderLine~ Lines
        +Money Subtotal
        +Money GrandTotal
        +Address BillingAddress
        +Address? ShippingAddress
        +Guid? ReservationId
        +Guid? PaymentId
        +Submit(OrderSubmission)$ Order
        +MarkStockReserved(Guid)
        +MarkPaid(Guid)
        +Confirm(DateTimeOffset)
        +Cancel(string, DateTimeOffset)
        +Refund(string, DateTimeOffset)
        +DequeueDomainEvents() IReadOnlyList~IDomainEvent~
    }
    class OrderStatus {
        <<enumeration>>
        Submitted
        StockReserved
        Paid
        Confirmed
        Cancelled
        Refunded
    }
    class Money {
        <<record>>
        +decimal Amount
        +string CurrencyCode
    }
    class Address {
        <<record>>
        +string Line1
        +string? Line2
        +string City
        +string PostalCode
        +string CountryCode
    }
    class OrderLine {
        <<record>>
        +Guid ProductId
        +string ProductSku
        +Money UnitPrice
        +int Quantity
        +bool IsDigital
        +Money LineTotal
    }
    class OrderSubmittedDomainEvent
    class OrderConfirmedDomainEvent
    class OrderCancelledDomainEvent
    class OrderRefundedDomainEvent

    Order o-- OrderLine
    Order o-- Money
    Order o-- Address
    Order ..> OrderStatus
    OrderLine o-- Money
```

### Saga state machine

The correlation id is the order id. The machine moves through three states and finalizes; every
transition delegates its side effect to a work consumer.

```mermaid
stateDiagram-v2
    [*] --> AwaitingStockReservation : BasketCheckoutStarted / Submit order
    AwaitingStockReservation --> AwaitingPayment : StockReserved / MarkStockReserved
    AwaitingStockReservation --> [*] : StockReservationFailed / Cancel (outbox OrderCancelled)
    AwaitingPayment --> [*] : PaymentCaptured / MarkPaid + Confirm (outbox OrderConfirmed)
    AwaitingPayment --> [*] : PaymentFailed / release reservation + Cancel
```

```mermaid
classDiagram
    class CheckoutState {
        +Guid CorrelationId
        +string CurrentState
        +Guid CustomerId
        +Guid? ReservationId
        +Guid? PaymentId
        +byte[] RowVersion
    }
    class CheckoutSagaStateMachine
    class ReserveOrderStockConsumer
    class CaptureOrderPaymentConsumer
    class IInventoryClient {
        <<interface>>
        +ReserveStockAsync(...) Task~StockReservationOutcome~
        +ReleaseReservationAsync(...) Task
    }
    class IPaymentClient {
        <<interface>>
        +CaptureAsync(...) Task~PaymentOutcome~
        +RefundAsync(...) Task
    }
    class GrpcInventoryClient
    class HttpPaymentClient
    class DomainEventsToOutboxInterceptor
    class IOrderReadQueries {
        <<interface>>
    }
    class DapperOrderReadQueries

    CheckoutSagaStateMachine o-- CheckoutState
    CheckoutSagaStateMachine ..> ReserveOrderStockConsumer
    CheckoutSagaStateMachine ..> CaptureOrderPaymentConsumer
    ReserveOrderStockConsumer ..> IInventoryClient
    CaptureOrderPaymentConsumer ..> IPaymentClient
    GrpcInventoryClient ..|> IInventoryClient
    HttpPaymentClient ..|> IPaymentClient
    DapperOrderReadQueries ..|> IOrderReadQueries
    DomainEventsToOutboxInterceptor ..> Order
```

---

## Inventory service

Layered (endpoint or gRPC then service then repository) with Dapper and explicit transactions.
No CQRS and no rich domain; transactional correctness and read latency dominate.

```mermaid
classDiagram
    class InventoryGrpcService {
        +ReserveStock(ReserveStockRequest, ServerCallContext) Task~ReserveStockResponse~
        +ReleaseReservation(ReleaseReservationRequest, ServerCallContext) Task~ReleaseReservationResponse~
    }
    class StockReservationService {
        +ReserveAsync(Guid, IReadOnlyList~ReservationLine~, CancellationToken) Task~ReservationOutcome~
        +ReleaseAsync(Guid, CancellationToken) Task
    }
    class IStockRepository {
        <<interface>>
        +GetBySkuAsync(...) Task
        +GetPagedAsync(...) Task
        +UpsertAsync(...) Task
        +AdjustQuantityAsync(...) Task
    }
    class IReservationRepository {
        <<interface>>
        +GetByOrderIdAsync(...) Task
        +InsertAsync(...) Task
        +MarkReleasedAsync(...) Task
    }
    class SqlStockRepository
    class SqlReservationRepository
    class SqlConnectionFactory
    class ProductCreatedConsumer
    class OrderCancelledConsumer

    InventoryGrpcService ..> StockReservationService
    StockReservationService ..> IStockRepository
    StockReservationService ..> IReservationRepository
    SqlStockRepository ..|> IStockRepository
    SqlReservationRepository ..|> IReservationRepository
    SqlStockRepository ..> SqlConnectionFactory
    SqlReservationRepository ..> SqlConnectionFactory
    ProductCreatedConsumer ..> IStockRepository
    OrderCancelledConsumer ..> StockReservationService
```

---

## Payment service

Clean Architecture plus event sourcing. The aggregate is rebuilt from events; a synchronous
projector keeps the SQL read model current after every append. A declined card is a domain
outcome, not a fault.

```mermaid
classDiagram
    class PaymentAggregate {
        +Guid PaymentId
        +PaymentStatus Status
        +decimal CapturedAmount
        +decimal RefundedAmount
        +int Version
        +Rehydrate(IEnumerable~IPaymentEvent~)$ PaymentAggregate
        +Initiate(...)
        +Authorize(string)
        +Capture()
        +Decline(string)
        +Refund(decimal, string)
    }
    class PaymentStatus {
        <<enumeration>>
        Initiated
        Authorized
        Captured
        Declined
        Refunded
        PartiallyRefunded
    }
    class IPaymentEvent {
        <<interface>>
        +Guid PaymentId
        +int Version
        +DateTimeOffset OccurredOnUtc
    }
    class IPaymentEventStore {
        <<interface>>
        +AppendAsync(...) Task
        +LoadStreamAsync(...) Task
    }
    class IPaymentReadModelWriter {
        <<interface>>
    }
    class IPaymentReadQueries {
        <<interface>>
    }
    class IPaymentProvider {
        <<interface>>
        +AuthorizeAsync(...) Task
        +CaptureAsync(...) Task
        +RefundAsync(...) Task
    }
    class CapturePaymentCommandHandler
    class RefundPaymentCommandHandler
    class MongoPaymentEventStore
    class SqlPaymentReadModelWriter
    class DapperPaymentReadQueries
    class SimulatedCardPaymentProvider

    PaymentAggregate ..> PaymentStatus
    PaymentAggregate ..> IPaymentEvent
    CapturePaymentCommandHandler ..> IPaymentEventStore
    CapturePaymentCommandHandler ..> IPaymentReadModelWriter
    CapturePaymentCommandHandler ..> IPaymentProvider
    CapturePaymentCommandHandler ..> PaymentAggregate
    RefundPaymentCommandHandler ..> IPaymentEventStore
    MongoPaymentEventStore ..|> IPaymentEventStore
    SqlPaymentReadModelWriter ..|> IPaymentReadModelWriter
    DapperPaymentReadQueries ..|> IPaymentReadQueries
    SimulatedCardPaymentProvider ..|> IPaymentProvider
```

---

## Delivery service

Hexagonal. The domain core references no infrastructure; every external concern is an adapter
behind a port. The service captures the shipping address from `BasketCheckoutStarted` into a
local `PendingShipment`, then schedules on `OrderConfirmed`.

```mermaid
classDiagram
    class Delivery {
        +Guid Id
        +Guid OrderId
        +Guid CustomerId
        +Address Address
        +DeliveryStatus Status
        +DateTimeOffset SlotStartUtc
        +DateTimeOffset SlotEndUtc
        +Guid? DriverId
    }
    class DeliverySlot {
        +Guid Id
        +Guid ZoneId
        +int Capacity
        +int BookedCount
        +Book()
    }
    class DeliveryZone {
        +Guid Id
        +string Name
        +ZonePolygon Polygon
    }
    class DeliverySchedulingPolicy {
        +SelectSlotAndDriver(...) ScheduleDecision
    }
    class IDeliveryRepository {
        <<interface>>
    }
    class ISlotRepository {
        <<interface>>
    }
    class IZoneRepository {
        <<interface>>
    }
    class IGeocodingService {
        <<interface>>
        +GeocodeAsync(Address, CancellationToken) Task~GeoCoordinate~
    }
    class ScheduleDeliveryService
    class CompleteDeliveryService
    class OrderConfirmedConsumer
    class DeterministicGeocodingAdapter
    class MongoDeliveryRepository

    ScheduleDeliveryService ..> DeliverySchedulingPolicy
    ScheduleDeliveryService ..> IDeliveryRepository
    ScheduleDeliveryService ..> ISlotRepository
    ScheduleDeliveryService ..> IZoneRepository
    ScheduleDeliveryService ..> IGeocodingService
    OrderConfirmedConsumer ..> ScheduleDeliveryService
    DeterministicGeocodingAdapter ..|> IGeocodingService
    MongoDeliveryRepository ..|> IDeliveryRepository
    Delivery ..> DeliveryZone
```

---

## Notification service

Bus consumers plus a SignalR hub plus channel senders. No domain logic; the complexity is
fan-out routing. History lives behind `INotificationStore` (MongoDB locally).

```mermaid
classDiagram
    class NotificationHub {
        +OnConnectedAsync() Task
        +MarkAsRead(Guid) Task
    }
    class NotificationDocument {
        +Guid Id
        +Guid UserId
        +string Type
        +string Title
        +string Message
        +Guid? OrderId
        +Guid SourceEventId
        +bool IsRead
    }
    class INotificationStore {
        <<interface>>
        +AddAsync(...) Task
        +GetForUserAsync(...) Task
        +MarkAsReadAsync(...) Task
        +CountUnreadAsync(...) Task
    }
    class INotificationChannel {
        <<interface>>
        +SendAsync(NotificationDocument, CancellationToken) Task
    }
    class NotificationDispatcher
    class SignalRNotificationChannel
    class EmailNotificationChannel
    class IEmailSender {
        <<interface>>
    }
    class MongoNotificationStore
    class OrderPlacedConsumer
    class OrderConfirmedConsumer
    class PaymentFailedConsumer

    NotificationDispatcher o-- INotificationChannel
    SignalRNotificationChannel ..|> INotificationChannel
    EmailNotificationChannel ..|> INotificationChannel
    EmailNotificationChannel ..> IEmailSender
    MongoNotificationStore ..|> INotificationStore
    OrderPlacedConsumer ..> INotificationStore
    OrderPlacedConsumer ..> NotificationDispatcher
    OrderConfirmedConsumer ..> INotificationStore
    OrderConfirmedConsumer ..> NotificationDispatcher
    PaymentFailedConsumer ..> INotificationStore
    NotificationHub ..> INotificationStore
```

---

## CustomerSupport service

Connection manager plus SignalR hub plus repositories; no CQRS. The showcase is the round-robin
agent assignment, kept atomic across replicas by a Redis Lua script. Hub orchestration is
extracted into `ChatSessionCoordinator` so it can be unit-tested without a live connection.

```mermaid
classDiagram
    class SupportChatHub {
        +RequestChat(string) Task
        +SendMessage(Guid, string) Task
        +SetTyping(Guid, bool) Task
        +EndChat(Guid) Task
        +OnDisconnectedAsync(Exception?) Task
    }
    class ChatSessionCoordinator {
        +RequestChatAsync(...) Task
        +SendMessageAsync(...) Task
        +EndChatAsync(...) Task
        +HandleAgentDisconnectAsync(...) Task
    }
    class IAgentAvailabilityRegistry {
        <<interface>>
        +RegisterAsync(...) Task
        +DeregisterAsync(...) Task
    }
    class IAgentAssignmentStrategy {
        <<interface>>
        +AssignAsync(CancellationToken) Task~Guid?~
        +ReleaseAsync(Guid, CancellationToken) Task
    }
    class IChatWaitingLine {
        <<interface>>
        +EnqueueAsync(...) Task
        +DequeueAsync(...) Task
    }
    class ISupportChatNotifier {
        <<interface>>
        +ChatAssignedAsync(...) Task
        +MessageReceivedAsync(...) Task
        +QueuePositionChangedAsync(...) Task
    }
    class RedisAgentAvailabilityRegistry
    class RedisAgentAssignmentStrategy
    class RedisChatWaitingLine
    class SignalRSupportChatNotifier
    class IChatSessionRepository {
        <<interface>>
    }
    class IChatMessageRepository {
        <<interface>>
    }
    class MongoChatSessionRepository
    class MongoChatMessageRepository

    SupportChatHub ..> ChatSessionCoordinator
    ChatSessionCoordinator ..> IAgentAssignmentStrategy
    ChatSessionCoordinator ..> IChatWaitingLine
    ChatSessionCoordinator ..> ISupportChatNotifier
    ChatSessionCoordinator ..> IChatSessionRepository
    ChatSessionCoordinator ..> IChatMessageRepository
    RedisAgentAvailabilityRegistry ..|> IAgentAvailabilityRegistry
    RedisAgentAssignmentStrategy ..|> IAgentAssignmentStrategy
    RedisChatWaitingLine ..|> IChatWaitingLine
    SignalRSupportChatNotifier ..|> ISupportChatNotifier
    MongoChatSessionRepository ..|> IChatSessionRepository
    MongoChatMessageRepository ..|> IChatMessageRepository
```

---

## Reviews service

Vertical Slice on MongoDB, the same slice idiom as Catalog. The verified-purchase badge comes
from purchase entitlements the service records locally from `OrderConfirmed`.

```mermaid
classDiagram
    class ProductReview {
        +Guid Id
        +string ProductSku
        +Guid CustomerId
        +int Rating
        +string Title
        +string Body
        +bool IsVerifiedPurchase
        +ReviewStatus Status
    }
    class PurchaseRecord {
        +Guid Id
        +Guid CustomerId
        +string ProductSku
        +Guid OrderId
    }
    class CreateReviewCommandHandler
    class GetProductReviewsQueryHandler
    class ModerateReviewCommandHandler
    class OrderConfirmedConsumer
    class IReviewRepository {
        <<interface>>
    }
    class IPurchaseRecordRepository {
        <<interface>>
    }

    CreateReviewCommandHandler ..> IReviewRepository
    CreateReviewCommandHandler ..> IPurchaseRecordRepository
    GetProductReviewsQueryHandler ..> IReviewRepository
    ModerateReviewCommandHandler ..> IReviewRepository
    OrderConfirmedConsumer ..> IPurchaseRecordRepository
```

---

## API Gateway (YARP, BFF)

The trust boundary where the browser cookie becomes a downstream JWT. A YARP transform provider
applies the cookie-to-JWT exchange on every proxied route, including the WebSocket upgrade for
the hubs; minted tokens are cached to avoid re-signing every request.

```mermaid
classDiagram
    class TokenExchangeTransformProvider {
        <<ITransformProvider>>
        +ValidateRoute(TransformRouteValidationContext)
        +Apply(TransformBuilderContext)
    }
    class CookieToJwtTokenExchanger {
        +TryIssueToken(ClaimsPrincipal, out string) bool
    }
    class AntiforgeryValidationMiddleware {
        +InvokeAsync(HttpContext, RequestDelegate) Task
    }
    class IMemoryCache {
        <<interface>>
    }
    class IAntiforgery {
        <<interface>>
    }

    TokenExchangeTransformProvider ..> CookieToJwtTokenExchanger
    CookieToJwtTokenExchanger ..> IMemoryCache
    AntiforgeryValidationMiddleware ..> IAntiforgery
```

---

## Customer SPA (Angular 20)

Standalone, zoneless, signals-first. Cross-feature state lives in NgRx SignalStores; one store
per concern. SignalR connections are owned by the realtime stores and torn down on sign-out.

```mermaid
classDiagram
    class AuthStore {
        <<SignalStore>>
        +user: Signal
        +isAuthenticated: Signal
        +signIn()
        +signOut()
        +initialize()
    }
    class BasketStore {
        <<SignalStore>>
        +items: Signal
        +totals: Signal
        +addItem()
        +updateQuantity()
        +applyCoupon()
        +clearAfterCheckout()
    }
    class NotificationsStore {
        <<SignalStore>>
        +items: Signal
        +unreadCount: Signal
        +connectionState: Signal
        +markAsRead()
    }
    class SupportChatStore {
        <<SignalStore>>
        +session: Signal
        +messages: Signal
        +requestChat()
        +sendMessage()
        +endChat()
    }
    class SignalrConnectionFactory {
        +create(hubPath) HubConnection
    }
    class CatalogApiService
    class BasketApiService
    class OrdersApiService

    NotificationsStore ..> SignalrConnectionFactory
    SupportChatStore ..> SignalrConnectionFactory
    NotificationsStore ..> AuthStore
    BasketStore ..> BasketApiService
```

---

## Checkout saga (sequence)

The saga owns three states between Submitted and Confirmed. It calls Inventory and Payment
through work consumers (not from inside the state machine), so the state machine stays pure and
the side effects stay testable. Delivery is not a saga step: it consumes `OrderConfirmed`
downstream and schedules a slot against the shipping address it captured earlier from
`BasketCheckoutStarted`.

```mermaid
sequenceDiagram
    autonumber
    participant Browser
    participant Gateway as YARP Gateway
    participant Basket
    participant Bus as RabbitMQ
    participant Ordering as Ordering saga
    participant Inventory
    participant Payment
    participant Notification

    Browser->>Gateway: POST /api/basket/checkout (cookie)
    Gateway->>Basket: cookie to JWT, forward request
    Basket->>Basket: one Marten session: store outbox + archive + delete basket
    Basket-->>Browser: 202 Accepted { orderId }
    Basket->>Bus: BasketCheckoutStartedIntegrationEvent (OutboxPublisher)

    Bus->>Ordering: deliver event
    Ordering->>Ordering: Initially: Submit order, state to AwaitingStockReservation
    Ordering->>Inventory: ReserveOrderStock consumer calls ReserveStock (gRPC)
    Inventory-->>Ordering: StockReserved
    Ordering->>Ordering: MarkStockReserved, state to AwaitingPayment
    Ordering->>Payment: CaptureOrderPayment consumer calls POST /payments (Idempotency-Key)
    Payment-->>Ordering: PaymentCaptured
    Ordering->>Ordering: MarkPaid then Confirm, outbox emits OrderConfirmed, Finalize
    Ordering->>Bus: OrderConfirmedIntegrationEvent

    Bus->>Notification: deliver OrderConfirmed
    Notification->>Browser: notificationReceived over SignalR (Redis backplane)

    Note over Ordering,Inventory: PaymentFailed path: saga releases the reservation via the Inventory client, cancels the order, outbox emits OrderCancelled
```

---

## Invoice generation (sequence)

```mermaid
sequenceDiagram
    autonumber
    participant AdminSpa as Admin SPA
    participant Gateway as YARP
    participant Reporting
    participant Db as MySQL warehouse
    participant Blob as Azure Blob
    participant Browser

    AdminSpa->>Gateway: POST /invoices (JWT)
    Gateway->>Reporting: GenerateInvoiceCommand
    Reporting->>Db: FindByOrderIdAsync
    alt invoice already exists
        Db-->>Reporting: existing record
        Reporting->>Blob: mint SAS URL
        Reporting-->>AdminSpa: GenerateInvoiceResult (existing)
    else first generation
        Reporting->>Db: AllocateNextNumberAsync (row lock)
        Db-->>Reporting: INV-2026-000123
        Reporting->>Reporting: QuestPDF render
        Reporting->>Blob: upload PDF
        Reporting->>Db: INSERT invoice + lines
        Reporting->>Blob: mint SAS URL
        Reporting-->>AdminSpa: GenerateInvoiceResult (new)
    end
    AdminSpa->>Browser: open SAS URL
```
