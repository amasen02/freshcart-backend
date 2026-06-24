using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Api.Seeding;
using Marten;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FreshCart.Catalog.Tests.Seeding;

public sealed class CatalogDataSeederTests
{
    private readonly IDocumentSession documentSession = Substitute.For<IDocumentSession>();
    private readonly ICatalogQueries catalogQueries = Substitute.For<ICatalogQueries>();
    private readonly IPublishEndpoint publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IServiceScopeFactory serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();

    public CatalogDataSeederTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICatalogQueries)).Returns(catalogQueries);
        serviceProvider.GetService(typeof(IDocumentSession)).Returns(documentSession);
        serviceProvider.GetService(typeof(IPublishEndpoint)).Returns(publishEndpoint);

        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(serviceProvider);
        serviceScopeFactory.CreateScope().Returns(serviceScope);
    }

    [Fact]
    public async Task SeedsCategoriesBrandsAndProductsOnFirstDevelopmentStart()
    {
        hostEnvironment.EnvironmentName.Returns(Environments.Development);
        catalogQueries.AnyCategoriesExistAsync(Arg.Any<CancellationToken>()).Returns(false);

        await CreateSeeder().StartAsync(CancellationToken.None);

        documentSession.Received(1).Store(Arg.Is<Category[]>(stored => stored.Length == CatalogSeedData.Categories.Count));
        documentSession.Received(1).Store(Arg.Is<Brand[]>(stored => stored.Length == CatalogSeedData.Brands.Count));
        documentSession.Received(1).Store(Arg.Is<Product[]>(stored => stored.Length == CatalogSeedData.Products.Count));
        await documentSession.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publishEndpoint.Received(CatalogSeedData.Products.Count)
            .Publish(Arg.Any<ProductCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NeverTouchesTheDatabaseOutsideDevelopment()
    {
        hostEnvironment.EnvironmentName.Returns(Environments.Production);

        await CreateSeeder().StartAsync(CancellationToken.None);

        serviceScopeFactory.DidNotReceive().CreateScope();
    }

    [Fact]
    public async Task SkipsSeedingWhenTheCatalogAlreadyContainsCategoriesSoRestartsNeverDuplicate()
    {
        hostEnvironment.EnvironmentName.Returns(Environments.Development);
        catalogQueries.AnyCategoriesExistAsync(Arg.Any<CancellationToken>()).Returns(true);

        await CreateSeeder().StartAsync(CancellationToken.None);

        documentSession.DidNotReceive().Store(Arg.Any<Category[]>());
        await documentSession.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await publishEndpoint.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<ProductCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    private CatalogDataSeeder CreateSeeder() => new(
        serviceScopeFactory,
        hostEnvironment,
        NullLogger<CatalogDataSeeder>.Instance);
}
