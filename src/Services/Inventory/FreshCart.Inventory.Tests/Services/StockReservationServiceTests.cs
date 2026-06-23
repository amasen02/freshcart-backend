using FluentAssertions;
using FreshCart.Inventory.Api.Models;
using FreshCart.Inventory.Api.Repositories;
using FreshCart.Inventory.Api.Services;
using FreshCart.Inventory.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FreshCart.Inventory.Tests.Services;

[Collection(InventoryDatabaseFixture.CollectionName)]
public sealed class StockReservationServiceTests : IAsyncLifetime, IAsyncDisposable
{
    private const string SeededProductName = "Integration test product";

    private readonly InventoryDatabaseFixture _databaseFixture;
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly StockRepository _stockRepository;
    private readonly ReservationRepository _reservationRepository;
    private readonly StockReservationService _reservationService;

    public StockReservationServiceTests(InventoryDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
        _connectionFactory = new SqlConnectionFactory(databaseFixture.ConnectionString);
        _stockRepository = new StockRepository(_connectionFactory);
        _reservationRepository = new ReservationRepository(_connectionFactory);
        _reservationService = CreateReservationService(_connectionFactory);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public ValueTask DisposeAsync() => _connectionFactory.DisposeAsync();

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Fact]
    public async Task ReserveAllocatesReservedQuantityWhenStockIsAvailable()
    {
        var productSku = await SeedStockItemAsync(quantityOnHand: 10);
        var orderId = Guid.NewGuid();

        var reservationResult = await _reservationService.ReserveAsync(
            orderId,
            [new StockReservationLine { ProductSku = productSku, Quantity = 3 }],
            CancellationToken.None);

        reservationResult.Succeeded.Should().BeTrue();
        reservationResult.ReservationId.Should().NotBeEmpty();
        reservationResult.UnavailableSkus.Should().BeEmpty();

        var stockItem = await GetRequiredStockItemAsync(productSku);
        stockItem.QuantityOnHand.Should().Be(10);
        stockItem.QuantityReserved.Should().Be(3);
        stockItem.QuantityAvailable.Should().Be(7);
    }

    [Fact]
    public async Task ReserveMergesDuplicateSkuLinesIntoASingleReservationLine()
    {
        var productSku = await SeedStockItemAsync(quantityOnHand: 5);
        var orderId = Guid.NewGuid();

        var reservationResult = await _reservationService.ReserveAsync(
            orderId,
            [
                new StockReservationLine { ProductSku = productSku, Quantity = 2 },
                new StockReservationLine { ProductSku = productSku, Quantity = 3 },
            ],
            CancellationToken.None);

        reservationResult.Succeeded.Should().BeTrue();

        var persistedReservation = await _reservationRepository
            .GetByOrderIdAsync(orderId, transaction: null, CancellationToken.None);

        persistedReservation.Should().NotBeNull();
        var mergedLine = persistedReservation!.Lines.Should().ContainSingle().Subject;
        mergedLine.ProductSku.Should().Be(productSku);
        mergedLine.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task ReserveFailsListingExactShortfallSkusAndLeavesQuantitiesUntouched()
    {
        var plentifulSku = await SeedStockItemAsync(quantityOnHand: 5);
        var scarceSku = await SeedStockItemAsync(quantityOnHand: 1);
        var unknownSku = UniqueSkuGenerator.CreateProductSku();
        var orderId = Guid.NewGuid();

        var reservationResult = await _reservationService.ReserveAsync(
            orderId,
            [
                new StockReservationLine { ProductSku = plentifulSku, Quantity = 3 },
                new StockReservationLine { ProductSku = scarceSku, Quantity = 2 },
                new StockReservationLine { ProductSku = unknownSku, Quantity = 1 },
            ],
            CancellationToken.None);

        reservationResult.Succeeded.Should().BeFalse();
        reservationResult.FailureReason.Should().Contain("Insufficient stock");
        reservationResult.UnavailableSkus.Should().BeEquivalentTo(scarceSku, unknownSku);

        var plentifulItem = await GetRequiredStockItemAsync(plentifulSku);
        plentifulItem.QuantityReserved.Should().Be(0);

        var scarceItem = await GetRequiredStockItemAsync(scarceSku);
        scarceItem.QuantityReserved.Should().Be(0);

        var persistedReservation = await _reservationRepository
            .GetByOrderIdAsync(orderId, transaction: null, CancellationToken.None);
        persistedReservation.Should().BeNull();
    }

    [Fact]
    public async Task ReservingTheSameOrderTwiceReturnsTheOriginalReservationWithoutDoubleAllocation()
    {
        var productSku = await SeedStockItemAsync(quantityOnHand: 4);
        var orderId = Guid.NewGuid();

        var firstResult = await ReserveSingleLineAsync(orderId, productSku, quantity: 2);
        var secondResult = await ReserveSingleLineAsync(orderId, productSku, quantity: 2);

        firstResult.Succeeded.Should().BeTrue();
        secondResult.Succeeded.Should().BeTrue();
        secondResult.ReservationId.Should().Be(firstResult.ReservationId);

        var stockItem = await GetRequiredStockItemAsync(productSku);
        stockItem.QuantityReserved.Should().Be(2);
    }

    [Fact]
    public async Task ReleaseRestoresAvailabilityAndMarksTheReservationReleased()
    {
        var productSku = await SeedStockItemAsync(quantityOnHand: 3);
        var orderId = Guid.NewGuid();
        await ReserveSingleLineAsync(orderId, productSku, quantity: 3);

        var released = await _reservationService.ReleaseAsync(orderId, CancellationToken.None);

        released.Should().BeTrue();

        var stockItem = await GetRequiredStockItemAsync(productSku);
        stockItem.QuantityReserved.Should().Be(0);
        stockItem.QuantityAvailable.Should().Be(3);

        var persistedReservation = await _reservationRepository
            .GetByOrderIdAsync(orderId, transaction: null, CancellationToken.None);
        persistedReservation.Should().NotBeNull();
        persistedReservation!.Status.Should().Be(StockReservationStatus.Released);
        persistedReservation.ReleasedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ReleasingTwiceIsANoOpThatChangesNothing()
    {
        var productSku = await SeedStockItemAsync(quantityOnHand: 2);
        var orderId = Guid.NewGuid();
        await ReserveSingleLineAsync(orderId, productSku, quantity: 2);
        await _reservationService.ReleaseAsync(orderId, CancellationToken.None);

        var secondRelease = await _reservationService.ReleaseAsync(orderId, CancellationToken.None);

        secondRelease.Should().BeFalse();

        var stockItem = await GetRequiredStockItemAsync(productSku);
        stockItem.QuantityReserved.Should().Be(0);
        stockItem.QuantityOnHand.Should().Be(2);
    }

    [Fact]
    public async Task ConcurrentReservationsOfTheLastUnitAllowExactlyOneWinner()
    {
        var productSku = await SeedStockItemAsync(quantityOnHand: 1);

        var firstContenderFactory = new SqlConnectionFactory(_databaseFixture.ConnectionString);
        await using (firstContenderFactory)
        {
            var secondContenderFactory = new SqlConnectionFactory(_databaseFixture.ConnectionString);
            await using (secondContenderFactory)
            {
                var contendingResults = await Task.WhenAll(
                    ReserveLastUnitAsync(firstContenderFactory, productSku),
                    ReserveLastUnitAsync(secondContenderFactory, productSku));

                contendingResults.Count(result => result.Succeeded).Should().Be(1);

                var losingResult = contendingResults.Single(result => !result.Succeeded);
                losingResult.UnavailableSkus.Should().ContainSingle().Which.Should().Be(productSku);
            }
        }

        var stockItem = await GetRequiredStockItemAsync(productSku);
        stockItem.QuantityReserved.Should().Be(1);
    }

    private static StockReservationService CreateReservationService(SqlConnectionFactory connectionFactory) =>
        new(
            connectionFactory,
            new StockRepository(connectionFactory),
            new ReservationRepository(connectionFactory),
            TimeProvider.System,
            NullLogger<StockReservationService>.Instance);

    private static Task<StockReservationResult> ReserveLastUnitAsync(
        SqlConnectionFactory contenderConnectionFactory,
        string productSku)
    {
        var contenderService = CreateReservationService(contenderConnectionFactory);

        return contenderService.ReserveAsync(
            Guid.NewGuid(),
            [new StockReservationLine { ProductSku = productSku, Quantity = 1 }],
            CancellationToken.None);
    }

    private Task<StockReservationResult> ReserveSingleLineAsync(Guid orderId, string productSku, int quantity) =>
        _reservationService.ReserveAsync(
            orderId,
            [new StockReservationLine { ProductSku = productSku, Quantity = quantity }],
            CancellationToken.None);

    private async Task<string> SeedStockItemAsync(int quantityOnHand)
    {
        var productSku = UniqueSkuGenerator.CreateProductSku();
        var seededItem = new StockItem
        {
            ProductSku = productSku,
            ProductName = SeededProductName,
            QuantityOnHand = quantityOnHand,
            UpdatedOnUtc = TimeProvider.System.GetUtcNow(),
        };

        await _stockRepository.UpsertAsync(seededItem, transaction: null, CancellationToken.None).ConfigureAwait(false);

        return productSku;
    }

    private async Task<StockItem> GetRequiredStockItemAsync(string productSku)
    {
        var stockItem = await _stockRepository
            .GetBySkuAsync(productSku, transaction: null, CancellationToken.None)
            .ConfigureAwait(false);

        stockItem.Should().NotBeNull();

        return stockItem!;
    }
}
