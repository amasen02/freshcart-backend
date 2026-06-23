using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Features.Categories.CreateCategory;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using Marten;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Categories;

public sealed class CreateCategoryCommandHandlerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 1, 11, 0, 0, TimeSpan.Zero);

    private readonly IDocumentSession documentSession = Substitute.For<IDocumentSession>();
    private readonly ICatalogQueries catalogQueries = Substitute.For<ICatalogQueries>();
    private readonly HybridCache hybridCache = Substitute.For<HybridCache>();
    private readonly CreateCategoryCommandHandler handler;

    public CreateCategoryCommandHandlerTests()
    {
        handler = new CreateCategoryCommandHandler(
            documentSession,
            catalogQueries,
            hybridCache,
            new FixedTimeProvider(KnownInstantUtc));
    }

    [Fact]
    public async Task StoresAnActiveRootCategoryWithGeneratedSlugAndClockTimestamp()
    {
        var command = new CreateCategoryCommand("Online Courses", "Self-paced video courses.", null, 6);
        catalogQueries.CategorySlugExistsAsync("online-courses", Arg.Any<CancellationToken>()).Returns(false);
        Category? storedCategory = null;
        documentSession
            .When(session => session.Store(Arg.Any<Category[]>()))
            .Do(storeCall => storedCategory = storeCall.Arg<Category[]>().Single());

        var commandResult = await handler.Handle(command, CancellationToken.None);

        storedCategory.Should().NotBeNull();
        storedCategory!.Slug.Should().Be("online-courses");
        storedCategory.ParentCategoryId.Should().BeNull();
        storedCategory.SortOrder.Should().Be(6);
        storedCategory.IsActive.Should().BeTrue();
        storedCategory.CreatedOnUtc.Should().Be(KnownInstantUtc);
        commandResult.CategoryId.Should().Be(storedCategory.Id);
        commandResult.Slug.Should().Be("online-courses");
        await documentSession.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectsDuplicateSlugWithConflictBecauseSlugsAreStorefrontNaturalKeys()
    {
        var command = new CreateCategoryCommand("Online Courses", null, null, 6);
        catalogQueries.CategorySlugExistsAsync("online-courses", Arg.Any<CancellationToken>()).Returns(true);

        var creating = () => handler.Handle(command, CancellationToken.None);

        await creating.Should().ThrowAsync<ConflictException>().WithMessage("*online-courses*");
        documentSession.DidNotReceive().Store(Arg.Any<Category[]>());
    }

    [Fact]
    public Task ThrowsNotFoundWhenTheParentCategoryDoesNotExist()
    {
        var missingParentId = Guid.NewGuid();
        var command = new CreateCategoryCommand("Profilers", null, missingParentId, 1);
        catalogQueries.CategorySlugExistsAsync("profilers", Arg.Any<CancellationToken>()).Returns(false);
        documentSession.LoadAsync<Category>(missingParentId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var creating = () => handler.Handle(command, CancellationToken.None);

        return creating.Should().ThrowAsync<NotFoundException>().WithMessage($"*{nameof(Category)}*");
    }

    [Fact]
    public async Task LinksTheChildToAnExistingParentCategory()
    {
        var parentCategoryId = Guid.NewGuid();
        var command = new CreateCategoryCommand("Developer Tools", null, parentCategoryId, 2);
        catalogQueries.CategorySlugExistsAsync("developer-tools", Arg.Any<CancellationToken>()).Returns(false);
        documentSession.LoadAsync<Category>(parentCategoryId, Arg.Any<CancellationToken>())
            .Returns(new Category { Id = parentCategoryId, Name = "Software", Slug = "software", IsActive = true });
        Category? storedCategory = null;
        documentSession
            .When(session => session.Store(Arg.Any<Category[]>()))
            .Do(storeCall => storedCategory = storeCall.Arg<Category[]>().Single());

        await handler.Handle(command, CancellationToken.None);

        storedCategory.Should().NotBeNull();
        storedCategory!.ParentCategoryId.Should().Be(parentCategoryId);
    }

    [Fact]
    public async Task EvictsTheCachedCategoryTreeAfterSaving()
    {
        var command = new CreateCategoryCommand("Online Courses", null, null, 6);
        catalogQueries.CategorySlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        await handler.Handle(command, CancellationToken.None);

        await hybridCache.Received(1)
            .RemoveAsync(CatalogCachePolicy.CategoryTreeKey, Arg.Any<CancellationToken>());
    }
}
