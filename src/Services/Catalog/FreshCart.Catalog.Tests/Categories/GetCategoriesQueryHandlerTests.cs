using FluentAssertions;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Features.Categories.GetCategories;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Categories;

public sealed class GetCategoriesQueryHandlerTests
{
    private readonly ICatalogQueries catalogQueries = Substitute.For<ICatalogQueries>();
    private readonly PassThroughHybridCache hybridCache = new();
    private readonly GetCategoriesQueryHandler handler;

    public GetCategoriesQueryHandlerTests()
    {
        handler = new GetCategoriesQueryHandler(catalogQueries, hybridCache);
    }

    [Fact]
    public async Task BuildsTheNestedTreeFromActiveCategoriesOnACacheMiss()
    {
        var softwareId = Guid.NewGuid();
        var developerToolsId = Guid.NewGuid();
        catalogQueries.GetActiveCategoriesAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new Category { Id = softwareId, Name = "Software", Slug = "software", SortOrder = 1, IsActive = true },
            new Category
            {
                Id = developerToolsId,
                Name = "Developer Tools",
                Slug = "developer-tools",
                ParentCategoryId = softwareId,
                SortOrder = 1,
                IsActive = true,
            },
        ]);

        var categoryTree = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        var softwareNode = categoryTree.Should().ContainSingle().Subject;
        softwareNode.Id.Should().Be(softwareId);
        softwareNode.Children.Should().ContainSingle().Which.Id.Should().Be(developerToolsId);
    }

    [Fact]
    public async Task ReadsThroughTheSharedCategoryTreeCacheKey()
    {
        catalogQueries.GetActiveCategoriesAsync(Arg.Any<CancellationToken>()).Returns([]);

        await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        hybridCache.RequestedKeys.Should().ContainSingle()
            .Which.Should().Be(CatalogCachePolicy.CategoryTreeKey);
    }
}
