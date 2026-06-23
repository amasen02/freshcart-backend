using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Features.Brands.CreateBrand;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using Marten;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Brands;

public sealed class CreateBrandCommandHandlerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IDocumentSession documentSession = Substitute.For<IDocumentSession>();
    private readonly ICatalogQueries catalogQueries = Substitute.For<ICatalogQueries>();
    private readonly CreateBrandCommandHandler handler;

    public CreateBrandCommandHandlerTests()
    {
        handler = new CreateBrandCommandHandler(
            documentSession,
            catalogQueries,
            new FixedTimeProvider(KnownInstantUtc));
    }

    [Fact]
    public async Task StoresAnActiveBrandWithGeneratedSlugAndClockTimestamp()
    {
        var command = new CreateBrandCommand("PixelForge Studios", "https://cdn.freshcart.test/pixelforge.png");
        catalogQueries.BrandSlugExistsAsync("pixelforge-studios", Arg.Any<CancellationToken>()).Returns(false);
        Brand? storedBrand = null;
        documentSession
            .When(session => session.Store(Arg.Any<Brand[]>()))
            .Do(storeCall => storedBrand = storeCall.Arg<Brand[]>().Single());

        var commandResult = await handler.Handle(command, CancellationToken.None);

        storedBrand.Should().NotBeNull();
        storedBrand!.Name.Should().Be("PixelForge Studios");
        storedBrand.Slug.Should().Be("pixelforge-studios");
        storedBrand.LogoUrl.Should().Be("https://cdn.freshcart.test/pixelforge.png");
        storedBrand.IsActive.Should().BeTrue();
        storedBrand.CreatedOnUtc.Should().Be(KnownInstantUtc);
        commandResult.BrandId.Should().Be(storedBrand.Id);
        commandResult.Slug.Should().Be("pixelforge-studios");
        await documentSession.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectsDuplicateSlugWithConflictBecauseSlugsAreStorefrontNaturalKeys()
    {
        var command = new CreateBrandCommand("PixelForge Studios", null);
        catalogQueries.BrandSlugExistsAsync("pixelforge-studios", Arg.Any<CancellationToken>()).Returns(true);

        var creating = () => handler.Handle(command, CancellationToken.None);

        await creating.Should().ThrowAsync<ConflictException>().WithMessage("*pixelforge-studios*");
        documentSession.DidNotReceive().Store(Arg.Any<Brand[]>());
    }
}
