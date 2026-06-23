using FluentAssertions;
using FreshCart.Delivery.Application.Shipments;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Infrastructure.Persistence.Repositories;

namespace FreshCart.Delivery.Tests.Infrastructure;

[Collection(MongoIntegrationCollection.Name)]
public sealed class MongoPendingShipmentRepositoryTests(MongoIntegrationFixture fixture)
{
    private readonly MongoPendingShipmentRepository repository = new(fixture.Context);

    [Fact]
    public async Task UpsertIsIdempotentOnTheOrderIdSoRedeliveriesOverwriteRatherThanDuplicate()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var address = new DeliveryAddress("7 Elm Road", null, "London", "SE1 7PB", "GB");

        await repository.UpsertAsync(
            new PendingShipment(orderId, customerId, address, hasPhysicalLines: true),
            CancellationToken.None);
        await repository.UpsertAsync(
            new PendingShipment(orderId, customerId, address, hasPhysicalLines: true),
            CancellationToken.None);

        var stored = await repository.FindByOrderIdAsync(orderId, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.CustomerId.Should().Be(customerId);
        stored.IsDeliverable.Should().BeTrue();
    }

    [Fact]
    public async Task PreservesANullShippingAddressForADigitalOnlyShipment()
    {
        var orderId = Guid.NewGuid();
        await repository.UpsertAsync(
            new PendingShipment(orderId, Guid.NewGuid(), shippingAddress: null, hasPhysicalLines: false),
            CancellationToken.None);

        var stored = await repository.FindByOrderIdAsync(orderId, CancellationToken.None);

        stored!.ShippingAddress.Should().BeNull();
        stored.IsDeliverable.Should().BeFalse();
    }

    [Fact]
    public async Task DeletesThePendingShipmentOnceSchedulingHasConsumedIt()
    {
        var orderId = Guid.NewGuid();
        await repository.UpsertAsync(
            new PendingShipment(orderId, Guid.NewGuid(), shippingAddress: null, hasPhysicalLines: true),
            CancellationToken.None);

        await repository.DeleteByOrderIdAsync(orderId, CancellationToken.None);

        var stored = await repository.FindByOrderIdAsync(orderId, CancellationToken.None);
        stored.Should().BeNull();
    }
}
