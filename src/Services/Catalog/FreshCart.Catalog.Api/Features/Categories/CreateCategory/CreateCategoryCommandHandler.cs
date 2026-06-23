using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Caching;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Api.Slugs;
using Marten;
using Microsoft.Extensions.Caching.Hybrid;

namespace FreshCart.Catalog.Api.Features.Categories.CreateCategory;

/// <summary>
/// Creates a category and evicts the cached tree so storefront menus pick it up immediately. The
/// slug doubles as the natural key: two categories generating the same slug would shadow each other
/// in storefront URLs, so a duplicate is a conflict rather than something to auto-suffix.
/// </summary>
public sealed class CreateCategoryCommandHandler(
    IDocumentSession documentSession,
    ICatalogQueries catalogQueries,
    HybridCache hybridCache,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<CreateCategoryResult> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slug = SlugGenerator.Generate(command.Name);

        if (await catalogQueries.CategorySlugExistsAsync(slug, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A category with slug \"{slug}\" already exists.");
        }

        if (command.ParentCategoryId is { } parentCategoryId)
        {
            _ = await documentSession.LoadAsync<Category>(parentCategoryId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(nameof(Category), parentCategoryId);
        }

        var category = new Category
        {
            Id = Guid.CreateVersion7(),
            Name = command.Name.Trim(),
            Slug = slug,
            Description = command.Description,
            ParentCategoryId = command.ParentCategoryId,
            SortOrder = command.SortOrder,
            IsActive = true,
            CreatedOnUtc = timeProvider.GetUtcNow(),
        };

        documentSession.Store(category);
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await hybridCache.RemoveAsync(CatalogCachePolicy.CategoryTreeKey, cancellationToken).ConfigureAwait(false);

        return new CreateCategoryResult(category.Id, category.Slug);
    }
}
