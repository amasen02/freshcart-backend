namespace FreshCart.BuildingBlocks.Pagination;

/// <summary>
/// Paged-endpoint request envelope. Defaults are deliberately conservative; opting into a
/// larger page size has to be explicit so a caller cannot accidentally drain the database.
/// </summary>
public sealed record PaginationRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int MaxPageSize = 200;

    /// <summary>Returns a copy with both fields clamped to a safe range.</summary>
    public PaginationRequest Normalise()
    {
        var pageNumber = PageNumber < 1 ? 1 : PageNumber;
        var pageSize = PageSize switch
        {
            < 1            => 1,
            > MaxPageSize  => MaxPageSize,
            _              => PageSize,
        };

        return new PaginationRequest(pageNumber, pageSize);
    }
}
