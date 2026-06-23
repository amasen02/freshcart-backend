using FluentAssertions;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Features.Brands;
using FreshCart.Catalog.Api.Features.Brands.GetBrands;
using FreshCart.Catalog.Api.Models;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Brands;

public sealed class GetBrandsQueryHandlerTests
{
    private readonly ICatalogQueries catalogQueries = Substitute.For<ICatalogQueries>();
    private readonly GetBrandsQueryHandler handler;

    public GetBrandsQueryHandlerTests()
    {
        handler = new GetBrandsQueryHandler(catalogQueries);
    }

    [Fact]
    public async Task ProjectsActiveBrandsToDtosPreservingTheOrderFromThePort()
    {
        var chordCollective = CreateBrand("Chord Collective", "chord-collective");
        var novaSoft = CreateBrand("NovaSoft", "novasoft");
        catalogQueries.GetActiveBrandsAsync(Arg.Any<CancellationToken>())
            .Returns([chordCollective, novaSoft]);

        var brands = await handler.Handle(new GetBrandsQuery(), CancellationToken.None);

        brands.Should().HaveCount(2);
        brands[0].Should().Be(new BrandDto(chordCollective.Id, "Chord Collective", "chord-collective", chordCollective.LogoUrl));
        brands[1].Should().Be(new BrandDto(novaSoft.Id, "NovaSoft", "novasoft", novaSoft.LogoUrl));
    }

    [Fact]
    public async Task ReturnsAnEmptyListWhenThereAreNoActiveBrands()
    {
        catalogQueries.GetActiveBrandsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var brands = await handler.Handle(new GetBrandsQuery(), CancellationToken.None);

        brands.Should().BeEmpty();
    }

    private static Brand CreateBrand(string name, string slug) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
        LogoUrl = $"https://cdn.freshcart.test/{slug}.png",
        IsActive = true,
    };
}
