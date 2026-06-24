using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Catalog.Api.Data;
using MassTransit;
using Marten;

namespace FreshCart.Catalog.Api.Seeding;

/// <summary>
/// Stores the deterministic development catalog on first start when the host environment is
/// Development. Guarded so it never writes to Staging or Production, and skipped once any category
/// exists so restarts never duplicate documents. After storing it publishes a ProductCreated event per
/// product (mirroring the create-product use case) so Inventory seeds a stock row for each SKU.
/// </summary>
public sealed class CatalogDataSeeder(
    IServiceScopeFactory serviceScopeFactory,
    IHostEnvironment hostEnvironment,
    ILogger<CatalogDataSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            logger.LogInformation(
                "CatalogDataSeeder skipped because environment is \"{EnvironmentName}\" (Development required).",
                hostEnvironment.EnvironmentName);
            return;
        }

        using var serviceScope = serviceScopeFactory.CreateScope();
        var catalogQueries = serviceScope.ServiceProvider.GetRequiredService<ICatalogQueries>();
        var documentSession = serviceScope.ServiceProvider.GetRequiredService<IDocumentSession>();

        if (await catalogQueries.AnyCategoriesExistAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("CatalogDataSeeder skipped because the catalog already contains categories.");
            return;
        }

        documentSession.Store(CatalogSeedData.Categories.ToArray());
        documentSession.Store(CatalogSeedData.Brands.ToArray());
        documentSession.Store(CatalogSeedData.Products.ToArray());
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var publishEndpoint = serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await PublishProductCreatedEventsAsync(publishEndpoint, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "CatalogDataSeeder stored {CategoryCount} categories, {BrandCount} brands and {ProductCount} products.",
            CatalogSeedData.Categories.Count,
            CatalogSeedData.Brands.Count,
            CatalogSeedData.Products.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task PublishProductCreatedEventsAsync(
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var categoryNamesById = CatalogSeedData.Categories.ToDictionary(
            category => category.Id,
            category => category.Name);

        foreach (var product in CatalogSeedData.Products)
        {
            await publishEndpoint.Publish(
                new ProductCreatedIntegrationEvent
                {
                    ProductId = product.Id,
                    ProductSku = product.Sku,
                    ProductName = product.Name,
                    PrimaryCategory = categoryNamesById.GetValueOrDefault(product.CategoryId, string.Empty),
                    BasePrice = product.BasePrice,
                    CurrencyCode = product.CurrencyCode,
                    InitialStockQuantity = product.InitialStockQuantity,
                    IsDigital = product.IsDigital,
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
