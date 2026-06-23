using FluentAssertions;
using FreshCart.BuildingBlocks.Pagination;

namespace FreshCart.BuildingBlocks.Tests.Pagination;

public sealed class PaginationRequestTests
{
    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-5, 20, 1, 20)]
    [InlineData(1, 0, 1, 1)]
    [InlineData(1, -3, 1, 1)]
    [InlineData(1, 500, 1, PaginationRequest.MaxPageSize)]
    [InlineData(2, 50, 2, 50)]
    public void NormaliseClampsBothFieldsToTheSafeRange(int rawPageNumber, int rawPageSize, int expectedPageNumber, int expectedPageSize)
    {
        var normalised = new PaginationRequest(rawPageNumber, rawPageSize).Normalise();

        normalised.PageNumber.Should().Be(expectedPageNumber);
        normalised.PageSize.Should().Be(expectedPageSize);
    }

    [Fact]
    public void DefaultsAreConservative()
    {
        var defaults = new PaginationRequest();

        defaults.PageNumber.Should().Be(1);
        defaults.PageSize.Should().Be(20);
    }
}
