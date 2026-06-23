using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Features.Products.DeleteProduct;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using Marten;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Products;

public sealed class DeleteProductCommandHandlerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly IDocumentSession documentSession = Substitute.For<IDocumentSession>();
    private readonly HybridCache hybridCache = Substitute.For<HybridCache>();
    private readonly Product existingProduct;
    private readonly DeleteProductCommandHandler handler;

    public DeleteProductCommandHandlerTests()
    {
        existingProduct = new Product
        {
            Id = ProductId,
            Name = "Neon Drift Racing",
            Slug = "neon-drift-racing",
            Sku = "FC-GM-0003",
            BasePrice = 39.99m,
            CurrencyCode = "USD",
            IsActive = true,
            IsDigital = true,
            UpdatedOnUtc = KnownInstantUtc.AddDays(-7),
        };

        documentSession.LoadAsync<Product>(ProductId, Arg.Any<CancellationToken>()).Returns(existingProduct);

        handler = new DeleteProductCommandHandler(
            documentSession,
            hybridCache,
            new FixedTimeProvider(KnownInstantUtc));
    }

    [Fact]
    public async Task SoftDeletesByDeactivatingTheDocumentInsteadOfRemovingIt()
    {
        await handler.Handle(new DeleteProductCommand(ProductId), CancellationToken.None);

        existingProduct.IsActive.Should().BeFalse();
        existingProduct.UpdatedOnUtc.Should().Be(KnownInstantUtc);
        documentSession.Received(1).Store(Arg.Is<Product[]>(stored => stored.Single() == existingProduct));
        documentSession.DidNotReceive().Delete(Arg.Any<Product>());
        await documentSession.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvictsBothIdAndSlugCacheEntriesAfterSoftDeleting()
    {
        await handler.Handle(new DeleteProductCommand(ProductId), CancellationToken.None);

        await hybridCache.Received(1)
            .RemoveAsync(CatalogCachePolicy.ProductKey(ProductId), Arg.Any<CancellationToken>());
        await hybridCache.Received(1)
            .RemoveAsync(CatalogCachePolicy.ProductKey(existingProduct.Slug), Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task ThrowsNotFoundWhenProductDoesNotExist()
    {
        documentSession.LoadAsync<Product>(ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var deleting = () => handler.Handle(new DeleteProductCommand(ProductId), CancellationToken.None);

        return deleting.Should().ThrowAsync<NotFoundException>().WithMessage($"*{nameof(Product)}*");
    }
}
