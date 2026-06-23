using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Api.Slugs;
using Marten;

namespace FreshCart.Catalog.Api.Features.Brands.CreateBrand;

/// <summary>
/// Creates a brand. As with categories, the slug is the natural key for storefront URLs, so a
/// duplicate slug is rejected as a conflict.
/// </summary>
public sealed class CreateBrandCommandHandler(
    IDocumentSession documentSession,
    ICatalogQueries catalogQueries,
    TimeProvider timeProvider)
    : ICommandHandler<CreateBrandCommand, CreateBrandResult>
{
    public async Task<CreateBrandResult> Handle(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slug = SlugGenerator.Generate(command.Name);

        if (await catalogQueries.BrandSlugExistsAsync(slug, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A brand with slug \"{slug}\" already exists.");
        }

        var brand = new Brand
        {
            Id = Guid.CreateVersion7(),
            Name = command.Name.Trim(),
            Slug = slug,
            LogoUrl = command.LogoUrl,
            IsActive = true,
            CreatedOnUtc = timeProvider.GetUtcNow(),
        };

        documentSession.Store(brand);
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateBrandResult(brand.Id, brand.Slug);
    }
}
