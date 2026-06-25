using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Delivery.Application.Abstractions;
using Microsoft.Extensions.Logging;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Application.Fulfilment;

/// <summary>
/// Use case driving the back-office fulfilment transitions: marking a delivery out for delivery and
/// completing it. Completion is the moment the rest of the platform cares about: it stages a
/// <see cref="DeliveryCompletedIntegrationEvent"/> in the transactional outbox in the same transaction
/// that persists the completed delivery, so the event cannot be lost on a broker failure. The background
/// publisher delivers it. Going out for delivery raises no integration event, so it is a plain update.
/// </summary>
public sealed partial class CompleteDeliveryService(
    IDeliveryRepository deliveryRepository,
    IDeliveryUnitOfWork deliveryUnitOfWork,
    TimeProvider timeProvider,
    ILogger<CompleteDeliveryService> logger)
{
    public async Task StartOutForDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await LoadDeliveryAsync(deliveryId, cancellationToken).ConfigureAwait(false);

        delivery.StartOutForDelivery();
        await deliveryRepository.UpdateAsync(delivery, cancellationToken).ConfigureAwait(false);

        LogOutForDelivery(delivery.Id, delivery.OrderId);
    }

    public async Task CompleteAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await LoadDeliveryAsync(deliveryId, cancellationToken).ConfigureAwait(false);

        var completedOnUtc = timeProvider.GetUtcNow();
        delivery.Complete(completedOnUtc);

        var completedEvent = new DeliveryCompletedIntegrationEvent
        {
            OrderId = delivery.OrderId,
            DeliveryId = delivery.Id,
            CustomerId = delivery.CustomerId,
            DeliveredOnUtc = completedOnUtc,
        };

        await deliveryUnitOfWork.PersistCompletedDeliveryAsync(delivery, completedEvent, cancellationToken).ConfigureAwait(false);

        LogCompleted(delivery.Id, delivery.OrderId, completedOnUtc);
    }

    private const string DeliveryEntityName = "Delivery";

    private async Task<DeliveryAggregate> LoadDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken)
        => await deliveryRepository.FindByIdAsync(deliveryId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(DeliveryEntityName, deliveryId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Delivery {DeliveryId} for order {OrderId} is now out for delivery")]
    private partial void LogOutForDelivery(Guid deliveryId, Guid orderId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Delivery {DeliveryId} for order {OrderId} completed at {CompletedOnUtc}")]
    private partial void LogCompleted(Guid deliveryId, Guid orderId, DateTimeOffset completedOnUtc);
}
