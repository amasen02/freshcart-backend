using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Api.Slugs;
using Marten;
using MassTransit;
using Npgsql;

namespace FreshCart.Catalog.Api.Features.Products.CreateProduct;

/// <summary>
/// Creates the product document and announces it to the platform. The event is published directly
/// through <see cref="IPublishEndpoint"/>: catalog writes are administrative operations where the
/// brief dual-write window is acceptable; the transactional outbox stays reserved for Basket and
/// Ordering, where money depends on the event.
/// </summary>
public sealed class CreateProductCommandHandler(
    IDocumentSession documentSession,
    ICatalogQueries catalogQueries,
    IPublishEndpoint publishEndpoint,
    TimeProvider timeProvider)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await catalogQueries.ProductSkuExistsAsync(command.Sku, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A product with sku \"{command.Sku}\" already exists.");
        }

        var category = await documentSession.LoadAsync<Category>(command.CategoryId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Category), command.CategoryId);

        _ = await documentSession.LoadAsync<Brand>(command.BrandId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Brand), command.BrandId);

        var slug = await ResolveUniqueSlugAsync(command, cancellationToken).ConfigureAwait(false);
        var product = BuildProduct(command, slug, timeProvider.GetUtcNow());

        documentSession.Store(product);
        try
        {
            await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception persistFailure) when (IsSkuUniqueViolation(persistFailure))
        {
            // The sku pre-check above is not atomic with this write, so two concurrent creates for the
            // same sku can both pass it. The unique index on Sku is the real guard; the writer that loses
            // the race lands here, and the violation is mapped to a 409 instead of surfacing a raw 500.
            throw new ConflictException($"A product with sku \"{command.Sku}\" already exists.");
        }

        await publishEndpoint.Publish(
            new ProductCreatedIntegrationEvent
            {
                ProductId = product.Id,
                ProductSku = product.Sku,
                ProductName = product.Name,
                PrimaryCategory = category.Name,
                BasePrice = product.BasePrice,
                CurrencyCode = product.CurrencyCode,
                InitialStockQuantity = product.InitialStockQuantity,
                IsDigital = product.IsDigital,
            },
            cancellationToken).ConfigureAwait(false);

        return new CreateProductResult(product.Id, product.Slug);
    }

    private static Product BuildProduct(CreateProductCommand command, string slug, DateTimeOffset createdOnUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = command.Name.Trim(),
        Slug = slug,
        Description = command.Description,
        Sku = command.Sku,
        BasePrice = command.BasePrice,
        CurrencyCode = command.CurrencyCode,
        CategoryId = command.CategoryId,
        BrandId = command.BrandId,
        IsActive = true,
        IsDigital = command.IsDigital,
        Images = [.. command.Images ?? []],
        Attributes = [.. command.Attributes ?? []],
        InitialStockQuantity = command.InitialStockQuantity,
        CreatedOnUtc = createdOnUtc,
        UpdatedOnUtc = createdOnUtc,
    };

    // Products carry exactly one unique constraint (the Sku index), so any unique violation raised by the
    // insert is a duplicate sku. The Npgsql error surfaces wrapped in Marten's command exception, so the
    // whole exception chain is walked.
    private static bool IsSkuUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException
                && string.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string> ResolveUniqueSlugAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var slug = SlugGenerator.Generate(command.Name);

        if (!await catalogQueries.ProductSlugExistsAsync(slug, cancellationToken).ConfigureAwait(false))
        {
            return slug;
        }

        return SlugGenerator.Generate($"{command.Name} {command.Sku}");
    }
}
