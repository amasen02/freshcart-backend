using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Features.Products.GetProduct;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using Marten;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Products;

public sealed class GetProductQueryHandlerTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid BrandId = Guid.NewGuid();

    private readonly IQuerySession querySession = Substitute.For<IQuerySession>();
    private readonly ICatalogQueries catalogQueries = Substitute.For<ICatalogQueries>();
    private readonly PassThroughHybridCache hybridCache = new();
    private readonly GetProductQueryHandler handler;

    public GetProductQueryHandlerTests()
    {
        handler = new GetProductQueryHandler(querySession, catalogQueries, hybridCache);
    }

    [Fact]
    public async Task LoadsProductByIdWhenIdentifierParsesAsGuid()
    {
        var product = CreateProduct();
        querySession.LoadAsync<Product>(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        ArrangeCategoryAndBrand("Games", "PixelForge Studios");

        var productDetails = await handler.Handle(
            new GetProductQuery(ProductId.ToString()),
            CancellationToken.None);

        productDetails.Id.Should().Be(ProductId);
        productDetails.PrimaryCategory.Should().Be("Games");
        productDetails.BrandName.Should().Be("PixelForge Studios");
        hybridCache.RequestedKeys.Should().ContainSingle()
            .Which.Should().Be(CatalogCachePolicy.ProductKey(ProductId));
        await catalogQueries.DidNotReceive()
            .FindProductBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadsProductBySlugWhenIdentifierIsNotAGuid()
    {
        var product = CreateProduct();
        catalogQueries.FindProductBySlugAsync(product.Slug, Arg.Any<CancellationToken>()).Returns(product);
        ArrangeCategoryAndBrand("Games", "PixelForge Studios");

        var productDetails = await handler.Handle(new GetProductQuery(product.Slug), CancellationToken.None);

        productDetails.Slug.Should().Be(product.Slug);
        hybridCache.RequestedKeys.Should().ContainSingle()
            .Which.Should().Be(CatalogCachePolicy.ProductKey(product.Slug));
        await querySession.DidNotReceive().LoadAsync<Product>(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task ThrowsNotFoundInsideTheCacheFactorySoMissesAreNeverCached()
    {
        querySession.LoadAsync<Product>(ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var loading = () => handler.Handle(new GetProductQuery(ProductId.ToString()), CancellationToken.None);

        return loading.Should().ThrowAsync<NotFoundException>().WithMessage($"*{nameof(Product)}*");
    }

    [Fact]
    public async Task FallsBackToEmptyDisplayNamesWhenCategoryOrBrandWereDeleted()
    {
        var product = CreateProduct();
        querySession.LoadAsync<Product>(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        querySession.LoadAsync<Category>(CategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);
        querySession.LoadAsync<Brand>(BrandId, Arg.Any<CancellationToken>()).Returns((Brand?)null);

        var productDetails = await handler.Handle(
            new GetProductQuery(ProductId.ToString()),
            CancellationToken.None);

        productDetails.PrimaryCategory.Should().BeEmpty();
        productDetails.BrandName.Should().BeEmpty();
    }

    private void ArrangeCategoryAndBrand(string categoryName, string brandName)
    {
        querySession.LoadAsync<Category>(CategoryId, Arg.Any<CancellationToken>())
            .Returns(new Category { Id = CategoryId, Name = categoryName, Slug = "games", IsActive = true });
        querySession.LoadAsync<Brand>(BrandId, Arg.Any<CancellationToken>())
            .Returns(new Brand { Id = BrandId, Name = brandName, Slug = "pixelforge-studios", IsActive = true });
    }

    private static Product CreateProduct() => new()
    {
        Id = ProductId,
        Name = "Starlight Odyssey Deluxe",
        Slug = "starlight-odyssey-deluxe",
        Sku = "FC-GM-0001",
        BasePrice = 69.99m,
        CurrencyCode = "USD",
        CategoryId = CategoryId,
        BrandId = BrandId,
        IsActive = true,
        IsDigital = true,
    };
}
