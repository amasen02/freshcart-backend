using FreshCart.Inventory.Api.Persistence;
using FreshCart.Inventory.Api.Repositories;
using FreshCart.Inventory.Api.Services;
using FreshCart.Inventory.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FreshCart.Inventory.Tests.Services;

[Collection(InventoryDatabaseFixture.CollectionName)]
public sealed class StockLevelServiceTests : IAsyncDisposable
{
    private const string ProductName = "Idempotency test product";

    private readonly SqlConnectionFactory _connectionFactory;
    private readonly StockRepository _stockRepository;
    private readonly StockLevelService _stockLevelService;

    public StockLevelServiceTests(InventoryDatabaseFixture databaseFixture)
    {
        _connectionFactory = new SqlConnectionFactory(databaseFixture.ConnectionString);
        _stockRepository = new StockRepository(_connectionFactory);
        _stockLevelService = new StockLevelService(_stockRepository, TimeProvider.System);
    }

    public ValueTask DisposeAsync() => _connectionFactory.DisposeAsync();

    [Fact]
    public async Task EnsureStockItemCreatesTheRowOnFirstSight()
    {
        var productSku = UniqueSkuGenerator.CreateProductSku();

        var created = await _stockLevelService.EnsureStockItemAsync(productSku, ProductName, 10, CancellationToken.None);

        created.Should().BeTrue();
        var stockItem = await _stockRepository.GetBySkuAsync(productSku, transaction: null, CancellationToken.None);
        stockItem.Should().NotBeNull();
        stockItem!.QuantityOnHand.Should().Be(10);
    }

    [Fact]
    public async Task EnsureStockItemIsIdempotentSoARedeliveredProductCreatedNeverResetsTheOnHandQuantity()
    {
        var productSku = UniqueSkuGenerator.CreateProductSku();
        await _stockLevelService.EnsureStockItemAsync(productSku, ProductName, 10, CancellationToken.None);

        // Stock is drawn down to 2 by real fulfilment, then ProductCreated is redelivered.
        await _stockRepository.AdjustQuantityAsync(
            productSku, quantityOnHandDelta: -8, quantityReservedDelta: 0, DateTimeOffset.UtcNow, transaction: null, CancellationToken.None);

        var createdOnRedelivery = await _stockLevelService.EnsureStockItemAsync(productSku, ProductName, 10, CancellationToken.None);

        createdOnRedelivery.Should().BeFalse("the row already exists, so a redelivery must be a no-op");
        var stockItem = await _stockRepository.GetBySkuAsync(productSku, transaction: null, CancellationToken.None);
        stockItem!.QuantityOnHand.Should().Be(2, "redelivery must not reset the on-hand quantity back to the initial value");
    }
}
