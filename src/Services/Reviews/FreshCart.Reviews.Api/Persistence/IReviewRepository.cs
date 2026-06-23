using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// Persistence port for product reviews. Reads are filtered server-side and paged so the handlers stay
/// free of MongoDB query construction.
/// </summary>
public interface IReviewRepository
{
    Task<bool> ExistsForCustomerAsync(string productSku, Guid customerId, CancellationToken cancellationToken);

    Task InsertAsync(ProductReview review, CancellationToken cancellationToken);

    Task<ProductReview?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken);

    Task ReplaceAsync(ProductReview review, CancellationToken cancellationToken);

    Task<PaginatedResult<ProductReview>> GetApprovedForProductAsync(
        string productSku,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RatingBucket>> GetApprovedRatingBucketsForProductAsync(
        string productSku,
        CancellationToken cancellationToken);

    Task<PaginatedResult<ProductReview>> GetForCustomerAsync(
        Guid customerId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken);

    Task<PaginatedResult<ProductReview>> GetByStatusAsync(
        ReviewStatus status,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken);
}
