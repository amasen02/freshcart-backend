using FreshCart.Catalog.Api.Data;
using Marten;

namespace FreshCart.Catalog.Api.Seeding;

/// <summary>
/// Stores the deterministic development catalog on first start when the host environment is
/// Development. Guarded so it never writes to Staging or Production, and skipped once any category
/// exists so restarts never duplicate documents.
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

        documentSession.Store(CatalogSeedData.Categories);
        documentSession.Store(CatalogSeedData.Brands);
        documentSession.Store(CatalogSeedData.Products);
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "CatalogDataSeeder stored {CategoryCount} categories, {BrandCount} brands and {ProductCount} products.",
            CatalogSeedData.Categories.Count,
            CatalogSeedData.Brands.Count,
            CatalogSeedData.Products.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
