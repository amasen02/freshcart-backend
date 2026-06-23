using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// MongoDB-backed review store. Listing reads lean on the (ProductSku, Status), (CustomerId,
/// CreatedOnUtc) and (Status, CreatedOnUtc) indexes the initializer creates; the rating summary is
/// computed by a server-side group aggregation rather than pulled into memory.
/// </summary>
public sealed class MongoReviewRepository : IReviewRepository
{
    private const string RatingBucketIdField = "_id";
    private const string RatingBucketCountField = "count";

    private readonly IMongoCollection<ProductReview> _reviews;

    public MongoReviewRepository(ReviewsMongoContext mongoContext)
    {
        ArgumentNullException.ThrowIfNull(mongoContext);

        _reviews = mongoContext.Reviews;
    }

    public Task<bool> ExistsForCustomerAsync(string productSku, Guid customerId, CancellationToken cancellationToken)
    {
        var filterBuilder = Builders<ProductReview>.Filter;
        var alreadyReviewed = filterBuilder.And(
            filterBuilder.Eq(review => review.ProductSku, productSku),
            filterBuilder.Eq(review => review.CustomerId, customerId));

        return _reviews.Find(alreadyReviewed).AnyAsync(cancellationToken);
    }

    public Task InsertAsync(ProductReview review, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(review);

        return _reviews.InsertOneAsync(review, options: null, cancellationToken);
    }

    // FirstOrDefaultAsync genuinely yields null on a miss but the driver leaves its result
    // non-null-annotated, so the task is forgiven to the honest nullable contract this port declares.
    public Task<ProductReview?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken) =>
        _reviews
            .Find(review => review.Id == reviewId)
            .FirstOrDefaultAsync(cancellationToken)!;

    public Task ReplaceAsync(ProductReview review, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(review);

        return _reviews.ReplaceOneAsync(stored => stored.Id == review.Id, review, options: (ReplaceOptions?)null, cancellationToken);
    }

    public Task<PaginatedResult<ProductReview>> GetApprovedForProductAsync(
        string productSku,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        var filterBuilder = Builders<ProductReview>.Filter;
        var approvedForProduct = filterBuilder.And(
            filterBuilder.Eq(review => review.ProductSku, productSku),
            filterBuilder.Eq(review => review.Status, ReviewStatus.Approved));

        return PageNewestFirstAsync(approvedForProduct, paginationRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<RatingBucket>> GetApprovedRatingBucketsForProductAsync(
        string productSku,
        CancellationToken cancellationToken)
    {
        var filterBuilder = Builders<ProductReview>.Filter;
        var approvedForProduct = filterBuilder.And(
            filterBuilder.Eq(review => review.ProductSku, productSku),
            filterBuilder.Eq(review => review.Status, ReviewStatus.Approved));

        var grouped = await _reviews
            .Aggregate(new AggregateOptions())
            .Match(approvedForProduct)
            .Group(
                review => review.Rating,
                grouping => new BsonDocument
                {
                    { RatingBucketIdField, grouping.Key },
                    { RatingBucketCountField, grouping.Count() },
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return grouped
            .Select(bucket => new RatingBucket(
                bucket[RatingBucketIdField].AsInt32,
                bucket[RatingBucketCountField].ToInt64()))
            .ToList();
    }

    public Task<PaginatedResult<ProductReview>> GetForCustomerAsync(
        Guid customerId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        var ownReviews = Builders<ProductReview>.Filter.Eq(review => review.CustomerId, customerId);

        return PageNewestFirstAsync(ownReviews, paginationRequest, cancellationToken);
    }

    public Task<PaginatedResult<ProductReview>> GetByStatusAsync(
        ReviewStatus status,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        var byStatus = Builders<ProductReview>.Filter.Eq(review => review.Status, status);

        return PageNewestFirstAsync(byStatus, paginationRequest, cancellationToken);
    }

    private async Task<PaginatedResult<ProductReview>> PageNewestFirstAsync(
        FilterDefinition<ProductReview> filter,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var normalisedRequest = paginationRequest.Normalise();

        var totalItemCount = await _reviews
            .CountDocumentsAsync(filter, options: null, cancellationToken)
            .ConfigureAwait(false);

        var documents = await _reviews
            .Find(filter)
            .SortByDescending(review => review.CreatedOnUtc)
            .Skip((normalisedRequest.PageNumber - 1) * normalisedRequest.PageSize)
            .Limit(normalisedRequest.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<ProductReview>(
            normalisedRequest.PageNumber,
            normalisedRequest.PageSize,
            totalItemCount,
            documents);
    }
}
