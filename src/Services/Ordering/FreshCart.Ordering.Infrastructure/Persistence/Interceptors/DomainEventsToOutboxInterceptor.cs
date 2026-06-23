using System.Text.Json;
using FreshCart.BuildingBlocks.Messaging.Events;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Domain.Orders.Events;
using FreshCart.Ordering.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FreshCart.Ordering.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Drains every Order aggregate's domain events into outbox rows just before the same SaveChanges
/// that persists the state change commits. Because the rows are inserted in that transaction, the
/// business write and the intent to publish are atomic: either both land or neither does, which is
/// the guarantee the outbox pattern exists to provide.
/// </summary>
public sealed class DomainEventsToOutboxInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var dbContext = eventData.Context;
        if (dbContext is not null)
        {
            DrainDomainEventsToOutbox(dbContext);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void DrainDomainEventsToOutbox(DbContext dbContext)
    {
        var orders = dbContext.ChangeTracker
            .Entries<Order>()
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var order in orders)
        {
            foreach (var domainEvent in order.DequeueDomainEvents())
            {
                var integrationEvent = MapToIntegrationEvent(domainEvent);
                if (integrationEvent is null)
                {
                    continue;
                }

                dbContext.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    EventType = integrationEvent.EventType,
                    ContentJson = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions),
                    OccurredOnUtc = timeProvider.GetUtcNow(),
                });
            }
        }
    }

    private static IntegrationEvent? MapToIntegrationEvent(IDomainEvent domainEvent) => domainEvent switch
    {
        OrderSubmittedDomainEvent submitted => new OrderPlacedIntegrationEvent
        {
            OrderId = submitted.OrderId,
            CustomerId = submitted.CustomerId,
            CustomerEmail = submitted.CustomerEmail,
            CustomerDisplayName = submitted.CustomerDisplayName,
            GrandTotal = submitted.GrandTotal.Amount,
            CurrencyCode = submitted.GrandTotal.CurrencyCode,
        },
        OrderConfirmedDomainEvent confirmed => new OrderConfirmedIntegrationEvent
        {
            OrderId = confirmed.OrderId,
            CustomerId = confirmed.CustomerId,
            GrandTotal = confirmed.GrandTotal.Amount,
            DiscountTotal = confirmed.DiscountTotal.Amount,
            TaxTotal = confirmed.TaxTotal.Amount,
            ShippingTotal = confirmed.ShippingTotal.Amount,
            CurrencyCode = confirmed.GrandTotal.CurrencyCode,
            PaymentMethod = confirmed.PaymentMethod,
            Lines = [.. confirmed.Lines.Select(line => new OrderConfirmedLine(
                line.ProductSku,
                line.ProductName,
                line.PrimaryCategory,
                line.Quantity,
                line.UnitPrice.Amount))],
        },
        OrderCancelledDomainEvent cancelled => new OrderCancelledIntegrationEvent
        {
            OrderId = cancelled.OrderId,
            CustomerId = cancelled.CustomerId,
            Reason = cancelled.Reason,
        },
        OrderRefundedDomainEvent refunded => new OrderRefundedIntegrationEvent
        {
            OrderId = refunded.OrderId,
            RefundAmount = refunded.RefundAmount.Amount,
            CurrencyCode = refunded.RefundAmount.CurrencyCode,
            Reason = refunded.Reason,
        },
        _ => null,
    };
}
