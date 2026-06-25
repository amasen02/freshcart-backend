using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Application.Shipments;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Scheduling;
using Microsoft.Extensions.Logging;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Application.Scheduling;

/// <summary>
/// Use case that turns a confirmed, physically-deliverable order into a scheduled delivery. It is the
/// orchestrator at the centre of the hexagon: it sequences the ports (pending-shipment lookup, geocode,
/// zone match, slot listing, booking, persistence) and applies the pure
/// <see cref="DeliverySchedulingPolicy"/> to pick the slot and driver. No transport or persistence
/// concept leaks in here; every dependency is a port.
/// </summary>
public sealed partial class ScheduleDeliveryService(
    IPendingShipmentRepository pendingShipmentRepository,
    IDeliveryRepository deliveryRepository,
    IDeliveryUnitOfWork deliveryUnitOfWork,
    ISlotRepository slotRepository,
    IZoneRepository zoneRepository,
    IDriverRepository driverRepository,
    IGeocodingService geocodingService,
    TimeProvider timeProvider,
    ILogger<ScheduleDeliveryService> logger)
{
    public async Task<ScheduleDeliveryOutcome> ScheduleForConfirmedOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var existingDelivery = await deliveryRepository
            .FindByOrderIdAsync(orderId, cancellationToken)
            .ConfigureAwait(false);

        if (existingDelivery is not null)
        {
            LogDeliveryAlreadyScheduled(orderId, existingDelivery.Id);
            return ScheduleDeliveryOutcome.AlreadyScheduled(existingDelivery);
        }

        var pendingShipment = await pendingShipmentRepository
            .FindByOrderIdAsync(orderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new PendingShipmentNotYetAvailableException(orderId);

        if (!pendingShipment.IsDeliverable)
        {
            LogSkippingNonDeliverableOrder(orderId);
            await pendingShipmentRepository.DeleteByOrderIdAsync(orderId, cancellationToken).ConfigureAwait(false);
            return ScheduleDeliveryOutcome.SkippedNotDeliverable;
        }

        var shippingAddress = pendingShipment.ShippingAddress!;
        var delivery = await CreateScheduledDeliveryAsync(orderId, pendingShipment.CustomerId, shippingAddress, cancellationToken)
            .ConfigureAwait(false);

        await pendingShipmentRepository.DeleteByOrderIdAsync(orderId, cancellationToken).ConfigureAwait(false);

        LogDeliveryScheduled(orderId, delivery.Id, delivery.SlotStartUtc, delivery.SlotEndUtc);
        return ScheduleDeliveryOutcome.Scheduled(delivery);
    }

    private async Task<DeliveryAggregate> CreateScheduledDeliveryAsync(
        Guid orderId,
        Guid customerId,
        DeliveryAddress shippingAddress,
        CancellationToken cancellationToken)
    {
        var coordinate = geocodingService.Locate(shippingAddress);

        var zone = await zoneRepository
            .FindZoneContainingAsync(coordinate, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"No delivery zone serves the address for order {orderId} at ({coordinate.Latitude}, {coordinate.Longitude})."));

        var openSlots = await slotRepository
            .ListOpenSlotsForZoneAsync(zone.Id, cancellationToken)
            .ConfigureAwait(false);

        var driverRotation = await driverRepository
            .GetActiveDriverRotationAsync(cancellationToken)
            .ConfigureAwait(false);

        var proposal = DeliverySchedulingPolicy.Propose(openSlots, driverRotation)
            ?? throw new DomainException(string.Create(
                CultureInfo.InvariantCulture,
                $"No delivery slot with free capacity and an active driver is available in zone {zone.Name} for order {orderId}."));

        proposal.Slot.Book();

        // The atomic conditional update — not the in-memory Book() above — is what holds the capacity
        // invariant under concurrency; a false return means a racing scheduler took the last unit, so fail
        // before creating the delivery and let the retry policy re-schedule against the remaining slots.
        if (!await slotRepository.TryBookSlotAsync(proposal.Slot, cancellationToken).ConfigureAwait(false))
        {
            throw new SlotNoLongerAvailableException(orderId, proposal.Slot.Id);
        }

        var delivery = DeliveryAggregate.Schedule(
            orderId,
            customerId,
            shippingAddress,
            proposal.Slot.StartUtc,
            proposal.Slot.EndUtc,
            proposal.DriverId,
            timeProvider.GetUtcNow());

        var scheduledEvent = new DeliveryScheduledIntegrationEvent
        {
            OrderId = delivery.OrderId,
            DeliveryId = delivery.Id,
            CustomerId = delivery.CustomerId,
            SlotStartUtc = delivery.SlotStartUtc,
            SlotEndUtc = delivery.SlotEndUtc,
        };

        // The delivery and its DeliveryScheduled event commit together; the background outbox publisher
        // delivers the event, so a broker outage can no longer drop it.
        await deliveryUnitOfWork.PersistScheduledDeliveryAsync(delivery, scheduledEvent, cancellationToken).ConfigureAwait(false);
        return delivery;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Delivery {DeliveryId} already scheduled for order {OrderId}; skipping duplicate")]
    private partial void LogDeliveryAlreadyScheduled(Guid orderId, Guid deliveryId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Order {OrderId} is digital-only or has no shipping address; no delivery scheduled")]
    private partial void LogSkippingNonDeliverableOrder(Guid orderId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Scheduled delivery {DeliveryId} for order {OrderId} in slot {SlotStartUtc}..{SlotEndUtc}")]
    private partial void LogDeliveryScheduled(Guid orderId, Guid deliveryId, DateTimeOffset slotStartUtc, DateTimeOffset slotEndUtc);
}
