using FluentAssertions;
using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.BuildingBlocks.Tests.Pagination;

public sealed class PaginatedResultTests
{
    [Theory]
    [InlineData(1,  10, 100, 10, true,  false)]
    [InlineData(2,  10, 100, 10, true,  true)]
    [InlineData(10, 10, 100, 10, false, true)]
    [InlineData(1,  10,   0,  0, false, false)]
    public void DerivedPropertiesReflectPagingState(
        int pageNumber,
        int pageSize,
        long totalItemCount,
        int expectedTotalPageCount,
        bool expectedHasNextPage,
        bool expectedHasPreviousPage)
    {
        var result = new PaginatedResult<int>(pageNumber, pageSize, totalItemCount, Array.Empty<int>());

        result.TotalPageCount.Should().Be(expectedTotalPageCount);
        result.HasNextPage.Should().Be(expectedHasNextPage);
        result.HasPreviousPage.Should().Be(expectedHasPreviousPage);
    }

    [Fact]
    public void TotalPageCountIsZeroWhenPageSizeIsNonPositive()
    {
        var result = new PaginatedResult<int>(1, 0, 50, Array.Empty<int>());

        result.TotalPageCount.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }
}
