namespace FreshCart.BuildingBlocks.Pagination;

/// <summary>
/// Paged-response envelope. Carrying <c>TotalItemCount</c> rather than a pre-computed page
/// count keeps the contract stable when page size changes between requests.
/// </summary>
public sealed record PaginatedResult<TItem>(
    int PageNumber,
    int PageSize,
    long TotalItemCount,
    IReadOnlyList<TItem> Items)
{
    public int TotalPageCount => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalItemCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPageCount;
}
