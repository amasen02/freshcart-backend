using FluentAssertions;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Drivers;
using FreshCart.Delivery.Infrastructure.Persistence.Repositories;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Tests.Infrastructure;

[Collection(MongoIntegrationCollection.Name)]
public sealed class MongoDeliveryRepositoryTests(MongoIntegrationFixture fixture)
{
    private readonly MongoDeliveryRepository deliveryRepository = new(fixture.Context);
    private readonly MongoDriverRepository driverRepository = new(fixture.Context);

    [Fact]
    public async Task RoundTripsADeliveryWithItsAddressStatusAndTimestamps()
    {
        var delivery = ScheduledDelivery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await deliveryRepository.AddAsync(delivery, CancellationToken.None);

        var reloaded = await deliveryRepository.FindByOrderIdAsync(delivery.OrderId, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(delivery.Id);
        reloaded.CustomerId.Should().Be(delivery.CustomerId);
        reloaded.Status.Should().Be(DeliveryStatus.Scheduled);
        reloaded.SlotStartUtc.Should().Be(delivery.SlotStartUtc);
        reloaded.Address.PostalCode.Should().Be(delivery.Address.PostalCode);
    }

    [Fact]
    public async Task PersistsAStatusTransitionThroughUpdate()
    {
        var delivery = ScheduledDelivery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await deliveryRepository.AddAsync(delivery, CancellationToken.None);

        delivery.StartOutForDelivery();
        await deliveryRepository.UpdateAsync(delivery, CancellationToken.None);

        var reloaded = await deliveryRepository.FindByIdAsync(delivery.Id, CancellationToken.None);
        reloaded!.Status.Should().Be(DeliveryStatus.OutForDelivery);
    }

    [Fact]
    public async Task RanksANeverAssignedActiveDriverAheadOfOneWithARecentAssignment()
    {
        var busyDriver = Driver.Create("Busy Driver");
        var idleDriver = Driver.Create("Idle Driver");
        await driverRepository.AddAsync(busyDriver, CancellationToken.None);
        await driverRepository.AddAsync(idleDriver, CancellationToken.None);

        var assignedDelivery = ScheduledDelivery(Guid.NewGuid(), Guid.NewGuid(), busyDriver.Id);
        await deliveryRepository.AddAsync(assignedDelivery, CancellationToken.None);

        var rotation = await driverRepository.GetActiveDriverRotationAsync(CancellationToken.None);

        var busyAssignment = rotation.Single(assignment => assignment.DriverId == busyDriver.Id);
        var idleAssignment = rotation.Single(assignment => assignment.DriverId == idleDriver.Id);
        busyAssignment.LastAssignedOnUtc.Should().NotBeNull();
        idleAssignment.LastAssignedOnUtc.Should().BeNull();
    }

    private static DeliveryAggregate ScheduledDelivery(Guid orderId, Guid customerId, Guid driverId) => DeliveryAggregate.Schedule(
        orderId,
        customerId,
        new DeliveryAddress("5 Baker Street", "Suite 1", "London", "NW1 6XE", "GB"),
        new DateTimeOffset(2026, 7, 5, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero),
        driverId,
        new DateTimeOffset(2026, 7, 4, 18, 0, 0, TimeSpan.Zero));
}
