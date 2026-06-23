using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Features.Products.CreateProduct;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using Marten;
using MassTransit;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Products;

public sealed class CreateProductCommandHandlerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 1, 8, 30, 0, TimeSpan.Zero);
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid BrandId = Guid.NewGuid();

    private readonly IDocumentSession documentSession = Substitute.For<IDocumentSession>();
    private readonly ICatalogQueries catalogQueries = Substitute.For<ICatalogQueries>();
    private readonly IPublishEndpoint publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly CreateProductCommandHandler handler;

    public CreateProductCommandHandlerTests()
    {
        handler = new CreateProductCommandHandler(
            documentSession,
            catalogQueries,
            publishEndpoint,
            new FixedTimeProvider(KnownInstantUtc));
    }

    [Fact]
    public async Task RejectsDuplicateSkuWithConflictBeforeWritingAnything()
    {
        var command = CreateCommand();
        catalogQueries.ProductSkuExistsAsync(command.Sku, Arg.Any<CancellationToken>()).Returns(true);

        var creating = () => handler.Handle(command, CancellationToken.None);

        await creating.Should().ThrowAsync<ConflictException>().WithMessage($"*{command.Sku}*");
        documentSession.DidNotReceive().Store(Arg.Any<Product[]>());
        await publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<ProductCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishesProductCreatedWithCategoryNameResolvedFromCategoryId()
    {
        var command = CreateCommand();
        ArrangeHappyPath(categoryName: "Software");

        await handler.Handle(command, CancellationToken.None);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<ProductCreatedIntegrationEvent>(integrationEvent =>
                integrationEvent.PrimaryCategory == "Software"
                && integrationEvent.ProductSku == command.Sku
                && integrationEvent.ProductName == command.Name
                && integrationEvent.BasePrice == command.BasePrice
                && integrationEvent.CurrencyCode == command.CurrencyCode
                && integrationEvent.InitialStockQuantity == command.InitialStockQuantity
                && integrationEvent.IsDigital == command.IsDigital),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task ThrowsNotFoundWhenCategoryDoesNotExist()
    {
        var command = CreateCommand();
        catalogQueries.ProductSkuExistsAsync(command.Sku, Arg.Any<CancellationToken>()).Returns(false);
        documentSession.LoadAsync<Category>(CategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var creating = () => handler.Handle(command, CancellationToken.None);

        return creating.Should().ThrowAsync<NotFoundException>().WithMessage($"*{nameof(Category)}*");
    }

    [Fact]
    public Task ThrowsNotFoundWhenBrandDoesNotExist()
    {
        var command = CreateCommand();
        catalogQueries.ProductSkuExistsAsync(command.Sku, Arg.Any<CancellationToken>()).Returns(false);
        documentSession.LoadAsync<Category>(CategoryId, Arg.Any<CancellationToken>())
            .Returns(CreateCategory("Software"));
        documentSession.LoadAsync<Brand>(BrandId, Arg.Any<CancellationToken>()).Returns((Brand?)null);

        var creating = () => handler.Handle(command, CancellationToken.None);

        return creating.Should().ThrowAsync<NotFoundException>().WithMessage($"*{nameof(Brand)}*");
    }

    [Fact]
    public async Task StoresActiveProductStampedWithInjectedClockAndReturnsItsIdAndSlug()
    {
        var command = CreateCommand();
        ArrangeHappyPath(categoryName: "Software");
        Product? storedProduct = null;
        documentSession
            .When(session => session.Store(Arg.Any<Product[]>()))
            .Do(storeCall => storedProduct = storeCall.Arg<Product[]>().Single());

        var commandResult = await handler.Handle(command, CancellationToken.None);

        storedProduct.Should().NotBeNull();
        storedProduct!.Slug.Should().Be("taskflow-pro-2026");
        storedProduct.IsActive.Should().BeTrue();
        storedProduct.CreatedOnUtc.Should().Be(KnownInstantUtc);
        storedProduct.UpdatedOnUtc.Should().Be(KnownInstantUtc);
        commandResult.ProductId.Should().Be(storedProduct.Id);
        commandResult.Slug.Should().Be(storedProduct.Slug);
        await documentSession.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallsBackToSkuSuffixedSlugWhenNameSlugIsAlreadyTaken()
    {
        var command = CreateCommand();
        ArrangeHappyPath(categoryName: "Software");
        catalogQueries.ProductSlugExistsAsync("taskflow-pro-2026", Arg.Any<CancellationToken>()).Returns(true);

        var commandResult = await handler.Handle(command, CancellationToken.None);

        commandResult.Slug.Should().Be("taskflow-pro-2026-fc-sw-0100");
    }

    private void ArrangeHappyPath(string categoryName)
    {
        catalogQueries.ProductSkuExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        catalogQueries.ProductSlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        documentSession.LoadAsync<Category>(CategoryId, Arg.Any<CancellationToken>())
            .Returns(CreateCategory(categoryName));
        documentSession.LoadAsync<Brand>(BrandId, Arg.Any<CancellationToken>())
            .Returns(new Brand
            {
                Id = BrandId,
                Name = "NovaSoft",
                Slug = "novasoft",
                IsActive = true,
            });
    }

    private static CreateProductCommand CreateCommand() => new(
        Name: "TaskFlow Pro 2026",
        Description: "Project planning suite.",
        Sku: "FC-SW-0100",
        BasePrice: 89.99m,
        CurrencyCode: "USD",
        CategoryId: CategoryId,
        BrandId: BrandId,
        IsDigital: true,
        InitialStockQuantity: 250);

    private static Category CreateCategory(string categoryName) => new()
    {
        Id = CategoryId,
        Name = categoryName,
        Slug = "software",
        SortOrder = 1,
        IsActive = true,
    };
}
