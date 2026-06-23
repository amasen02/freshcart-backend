using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Slots;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Application.Tracking;

/// <summary>
/// Read-side use cases: the customer tracking view and the open-slot listing. Ownership is enforced
/// here, not at the edge, so every caller of the tracking query goes through the same access rule.
/// </summary>
public sealed class DeliveryTrackingQueries(
    IDeliveryRepository deliveryRepository,
    ISlotRepository slotRepository)
{
    public async Task<DeliveryTrackingDto> GetTrackingForOrderAsync(
        Guid orderId,
        Guid requestingCustomerId,
        bool requesterIsAdministrator,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryRepository
            .FindByOrderIdAsync(orderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Delivery for order", orderId);

        if (!requesterIsAdministrator && delivery.CustomerId != requestingCustomerId)
        {
            throw new ForbiddenException("The delivery for this order belongs to a different customer.");
        }

        return ToTrackingDto(delivery);
    }

    public async Task<IReadOnlyList<OpenSlotDto>> ListOpenSlotsAsync(
        DateOnly dateUtc,
        CancellationToken cancellationToken)
    {
        var openSlots = await slotRepository
            .ListOpenSlotsOnDateAsync(dateUtc, cancellationToken)
            .ConfigureAwait(false);

        return openSlots
            .Select(ToOpenSlotDto)
            .OrderBy(slot => slot.StartUtc)
            .ThenBy(slot => slot.ZoneId)
            .ToList();
    }

    private static DeliveryTrackingDto ToTrackingDto(DeliveryAggregate delivery) => new(
        delivery.Id,
        delivery.OrderId,
        delivery.CustomerId,
        delivery.Status,
        ToAddressDto(delivery.Address),
        delivery.SlotStartUtc,
        delivery.SlotEndUtc,
        delivery.DriverId,
        delivery.CreatedOnUtc,
        delivery.CompletedOnUtc);

    private static DeliveryAddressDto ToAddressDto(DeliveryAddress address) => new(
        address.Line1,
        address.Line2,
        address.City,
        address.PostalCode,
        address.CountryCode);

    private static OpenSlotDto ToOpenSlotDto(DeliverySlot slot) => new(
        slot.Id,
        slot.ZoneId,
        slot.StartUtc,
        slot.EndUtc,
        slot.Capacity - slot.BookedCount);
}
