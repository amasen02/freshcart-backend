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

        // Pass arrays, not the IReadOnlyList: Marten 8's Store<T>(params T[]) otherwise binds T to the
        // list type itself and rejects it ("Do not use IEnumerable<T> here as the document type").
        documentSession.Store(CatalogSeedData.Categories.ToArray());
        documentSession.Store(CatalogSeedData.Brands.ToArray());
        documentSession.Store(CatalogSeedData.Products.ToArray());
        await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "CatalogDataSeeder stored {CategoryCount} categories, {BrandCount} brands and {ProductCount} products.",
            CatalogSeedData.Categories.Count,
            CatalogSeedData.Brands.Count,
            CatalogSeedData.Products.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
