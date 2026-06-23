using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Features.Products.UpdateProduct;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using Marten;
using MassTransit;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Products;

public sealed class UpdateProductCommandHandlerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid BrandId = Guid.NewGuid();

    private const decimal OriginalBasePrice = 89.99m;

    private readonly IDocumentSession documentSession = Substitute.For<IDocumentSession>();
    private readonly IPublishEndpoint publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly HybridCache hybridCache = Substitute.For<HybridCache>();
    private readonly Product existingProduct;
    private readonly UpdateProductCommandHandler handler;

    public UpdateProductCommandHandlerTests()
    {
        existingProduct = new Product
        {
            Id = ProductId,
            Name = "TaskFlow Pro 2026",
            Slug = "taskflow-pro-2026",
            Sku = "FC-SW-0100",
            BasePrice = OriginalBasePrice,
            CurrencyCode = "USD",
            CategoryId = CategoryId,
            BrandId = BrandId,
            IsActive = true,
            IsDigital = true,
            CreatedOnUtc = KnownInstantUtc.AddDays(-30),
            UpdatedOnUtc = KnownInstantUtc.AddDays(-30),
        };

        documentSession.LoadAsync<Product>(ProductId, Arg.Any<CancellationToken>()).Returns(existingProduct);
        documentSession.LoadAsync<Category>(CategoryId, Arg.Any<CancellationToken>())
            .Returns(new Category { Id = CategoryId, Name = "Software", Slug = "software", IsActive = true });
        documentSession.LoadAsync<Brand>(BrandId, Arg.Any<CancellationToken>())
            .Returns(new Brand { Id = BrandId, Name = "NovaSoft", Slug = "novasoft", IsActive = true });

        handler = new UpdateProductCommandHandler(
            documentSession,
            publishEndpoint,
            hybridCache,
            new FixedTimeProvider(KnownInstantUtc));
    }

    [Fact]
    public async Task PublishesProductPriceChangedCarryingOldAndNewPriceWhenBasePriceMoves()
    {
        var command = CreateCommand(basePrice: 99.99m);

        await handler.Handle(command, CancellationToken.None);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<ProductPriceChangedIntegrationEvent>(integrationEvent =>
                integrationEvent.ProductId == ProductId
                && integrationEvent.ProductSku == existingProduct.Sku
                && integrationEvent.OldPrice == OriginalBasePrice
                && integrationEvent.NewPrice == 99.99m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotPublishPriceChangedWhenBasePriceIsUnchanged()
    {
        var command = CreateCommand(basePrice: OriginalBasePrice);

        await handler.Handle(command, CancellationToken.None);

        await publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<ProductPriceChangedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AppliesEditableFieldsAndBumpsUpdatedTimestampWhilePreservingSlugAndSku()
    {
        var command = CreateCommand(basePrice: 79.99m) with { Name = "TaskFlow Pro 2027", IsActive = false };

        await handler.Handle(command, CancellationToken.None);

        existingProduct.Name.Should().Be("TaskFlow Pro 2027");
        existingProduct.BasePrice.Should().Be(79.99m);
        existingProduct.IsActive.Should().BeFalse();
        existingProduct.Slug.Should().Be("taskflow-pro-2026");
        existingProduct.Sku.Should().Be("FC-SW-0100");
        existingProduct.UpdatedOnUtc.Should().Be(KnownInstantUtc);
        await documentSession.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvictsBothIdAndSlugCacheEntriesAfterSaving()
    {
        var command = CreateCommand(basePrice: OriginalBasePrice);

        await handler.Handle(command, CancellationToken.None);

        await hybridCache.Received(1)
            .RemoveAsync(CatalogCachePolicy.ProductKey(ProductId), Arg.Any<CancellationToken>());
        await hybridCache.Received(1)
            .RemoveAsync(CatalogCachePolicy.ProductKey(existingProduct.Slug), Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task ThrowsNotFoundWhenProductDoesNotExist()
    {
        documentSession.LoadAsync<Product>(ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var updating = () => handler.Handle(CreateCommand(basePrice: OriginalBasePrice), CancellationToken.None);

        return updating.Should().ThrowAsync<NotFoundException>().WithMessage($"*{nameof(Product)}*");
    }

    [Fact]
    public async Task ThrowsNotFoundWhenTargetCategoryDoesNotExist()
    {
        documentSession.LoadAsync<Category>(CategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var updating = () => handler.Handle(CreateCommand(basePrice: OriginalBasePrice), CancellationToken.None);

        await updating.Should().ThrowAsync<NotFoundException>().WithMessage($"*{nameof(Category)}*");
        await publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<ProductPriceChangedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    private static UpdateProductCommand CreateCommand(decimal basePrice) => new(
        ProductId: ProductId,
        Name: "TaskFlow Pro 2026",
        Description: "Project planning suite.",
        BasePrice: basePrice,
        CurrencyCode: "USD",
        CategoryId: CategoryId,
        BrandId: BrandId,
        IsDigital: true,
        IsActive: true);
}
