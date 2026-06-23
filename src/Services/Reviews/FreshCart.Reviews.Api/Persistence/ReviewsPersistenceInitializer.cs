using FreshCart.Reviews.Api.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// Creates the review and purchase indexes on startup. The two unique indexes enforce the one-review-per
/// -product-per-customer rule and the once-per-order entitlement; the remaining indexes keep the
/// storefront, my-reviews and moderation-queue listings index-backed instead of degrading to collection
/// scans as the data grows.
/// </summary>
public sealed partial class ReviewsPersistenceInitializer(
    ReviewsMongoContext mongoContext,
    ILogger<ReviewsPersistenceInitializer> logger) : IHostedService
{
    private const string ReviewsProductCustomerUniqueIndexName = "UX_product_reviews_ProductSku_CustomerId";
    private const string ReviewsProductApprovedTimelineIndexName = "IX_product_reviews_ProductSku_Status_CreatedOnUtc";
    private const string ReviewsCustomerTimelineIndexName = "IX_product_reviews_CustomerId_CreatedOnUtc";
    private const string ReviewsStatusTimelineIndexName = "IX_product_reviews_Status_CreatedOnUtc";
    private const string PurchasesEntitlementUniqueIndexName = "UX_purchase_records_CustomerId_ProductSku_OrderId";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var productCustomerUniqueIndex = new CreateIndexModel<ProductReview>(
            Builders<ProductReview>.IndexKeys
                .Ascending(review => review.ProductSku)
                .Ascending(review => review.CustomerId),
            new CreateIndexOptions { Name = ReviewsProductCustomerUniqueIndexName, Unique = true });

        var productApprovedTimelineIndex = new CreateIndexModel<ProductReview>(
            Builders<ProductReview>.IndexKeys
                .Ascending(review => review.ProductSku)
                .Ascending(review => review.Status)
                .Descending(review => review.CreatedOnUtc),
            new CreateIndexOptions { Name = ReviewsProductApprovedTimelineIndexName });

        var customerTimelineIndex = new CreateIndexModel<ProductReview>(
            Builders<ProductReview>.IndexKeys
                .Ascending(review => review.CustomerId)
                .Descending(review => review.CreatedOnUtc),
            new CreateIndexOptions { Name = ReviewsCustomerTimelineIndexName });

        var statusTimelineIndex = new CreateIndexModel<ProductReview>(
            Builders<ProductReview>.IndexKeys
                .Ascending(review => review.Status)
                .Descending(review => review.CreatedOnUtc),
            new CreateIndexOptions { Name = ReviewsStatusTimelineIndexName });

        var entitlementUniqueIndex = new CreateIndexModel<PurchaseRecord>(
            Builders<PurchaseRecord>.IndexKeys
                .Ascending(record => record.CustomerId)
                .Ascending(record => record.ProductSku)
                .Ascending(record => record.OrderId),
            new CreateIndexOptions { Name = PurchasesEntitlementUniqueIndexName, Unique = true });

        await mongoContext.Reviews.Indexes
            .CreateManyAsync(
                [productCustomerUniqueIndex, productApprovedTimelineIndex, customerTimelineIndex, statusTimelineIndex],
                cancellationToken)
            .ConfigureAwait(false);

        await mongoContext.Purchases.Indexes
            .CreateOneAsync(entitlementUniqueIndex, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        LogPersistenceVerified();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Reviews persistence verified")]
    private partial void LogPersistenceVerified();
}
