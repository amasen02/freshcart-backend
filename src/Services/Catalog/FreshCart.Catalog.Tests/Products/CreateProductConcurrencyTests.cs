using System.Globalization;
using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Catalog.Api.Data;
using FreshCart.Catalog.Api.Features.Products.CreateProduct;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Tests.TestInfrastructure;
using Marten;
using MassTransit;
using NSubstitute;
using Xunit;

namespace FreshCart.Catalog.Tests.Products;

/// <summary>
/// Proves CAT-001 against a real PostgreSQL/Marten store: the sku existence pre-check in
/// <see cref="CreateProductCommandHandler"/> is not atomic with the write, so concurrent creates for the
/// same sku can all pass it, but the unique index on Sku admits exactly one product and every racing
/// loser is mapped to a <see cref="ConflictException"/> (a 409) rather than surfacing a raw 500.
/// </summary>
[Collection(CatalogMartenCollection.Name)]
public sealed class CreateProductConcurrencyTests(CatalogMartenFixture fixture)
{
    private static readonly DateTimeOffset CreatedOnUtc = new(2026, 6, 26, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ConcurrentCreatesForTheSameSkuYieldExactlyOneProductAndTheRestConflict()
    {
        const int RacerCount = 15;
        var sku = "FC-RACE-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var (categoryId, brandId) = await SeedCategoryAndBrandAsync();

        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, RacerCount).Select(_ => AttemptCreateAsync(sku, categoryId, brandId)));

        outcomes.Count(outcome => outcome == CreateOutcome.Created)
            .Should().Be(1, "the unique sku index admits exactly one product");
        outcomes.Count(outcome => outcome == CreateOutcome.Conflict)
            .Should().Be(RacerCount - 1, "every racing loser is mapped to a conflict, never a raw database error");

        await using var querySession = fixture.DocumentStore.QuerySession();
        var persistedCount = await querySession.Query<Product>().Where(product => product.Sku == sku).CountAsync();
        persistedCount.Should().Be(1);
    }

    private async Task<CreateOutcome> AttemptCreateAsync(string sku, Guid categoryId, Guid brandId)
    {
        var session = fixture.DocumentStore.LightweightSession();
        await using (session.ConfigureAwait(false))
        {
            var handler = new CreateProductCommandHandler(
                session,
                new MartenCatalogQueries(session),
                Substitute.For<IPublishEndpoint>(),
                new FixedTimeProvider(CreatedOnUtc));

            var command = new CreateProductCommand(
                Name: "Race Product",
                Description: "Concurrency probe.",
                Sku: sku,
                BasePrice: 9.99m,
                CurrencyCode: "USD",
                CategoryId: categoryId,
                BrandId: brandId,
                IsDigital: true,
                InitialStockQuantity: 100);

            try
            {
                await handler.Handle(command, CancellationToken.None);
                return CreateOutcome.Created;
            }
            catch (ConflictException)
            {
                return CreateOutcome.Conflict;
            }
        }
    }

    private async Task<(Guid CategoryId, Guid BrandId)> SeedCategoryAndBrandAsync()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var suffix = categoryId.ToString("N", CultureInfo.InvariantCulture);

        var session = fixture.DocumentStore.LightweightSession();
        await using (session.ConfigureAwait(false))
        {
            session.Store(new Category
            {
                Id = categoryId,
                Name = "Race Category",
                Slug = "race-category-" + suffix,
                SortOrder = 1,
                IsActive = true,
            });
            session.Store(new Brand
            {
                Id = brandId,
                Name = "Race Brand",
                Slug = "race-brand-" + suffix,
                IsActive = true,
            });
            await session.SaveChangesAsync();
        }

        return (categoryId, brandId);
    }

    private enum CreateOutcome
    {
        Created,
        Conflict,
    }
}
