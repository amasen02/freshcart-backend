using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Notification.Api.Hubs;
using FreshCart.Notification.Api.Notifications;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace FreshCart.Notification.Api.Consumers;

/// <summary>
/// Notifies the customer that their order is confirmed and emits the live sales tick to the
/// <c>backoffice</c> group so every connected dashboard refreshes its KPIs. The tick fires only on a
/// genuine first delivery, so a redelivered event never double-counts on the dashboards.
/// </summary>
public sealed class OrderConfirmedConsumer(
    NotificationRecorder notificationRecorder,
    IHubContext<NotificationHub> hubContext)
    : IConsumer<OrderConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderConfirmedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var integrationEvent = context.Message;
        var content = NotificationContentFactory.ForOrderConfirmed(integrationEvent);

        var outcome = await notificationRecorder
            .RecordAndDispatchAsync(
                integrationEvent.CustomerId,
                integrationEvent.EventId,
                content,
                integrationEvent.OrderId,
                context.CancellationToken)
            .ConfigureAwait(false);

        if (outcome == AddNotificationOutcome.Stored)
        {
            await hubContext.Clients
                .Group(NotificationGroups.BackOffice)
                .SendAsync(NotificationHubMethods.SalesDashboardUpdated, context.CancellationToken)
                .ConfigureAwait(false);
        }
    }
}
