using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure.Persistence;

/// <summary>
/// Creates the indexes the delivery store relies on, once, at startup. The 2dsphere index on the zone
/// boundary is what makes the <c>$geoIntersects</c> zone match a server-side indexed query; the order
/// id indexes back the one-delivery-per-order idempotency lookup; the outbox index backs the publisher's
/// unpublished-message poll so it does not degrade to a collection scan as processed rows accumulate.
/// <c>CreateOne</c> is idempotent, so running this on every boot is safe.
/// </summary>
public sealed partial class DeliveryMongoIndexInitializer(
    DeliveryMongoContext context,
    ILogger<DeliveryMongoIndexInitializer> logger)
    : IHostedService
{
    private const string ZoneBoundaryIndexName = "zone-boundary-2dsphere";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var zoneBoundaryIndex = new CreateIndexModel<ZoneDocument>(
            Builders<ZoneDocument>.IndexKeys.Geo2DSphere(zone => zone.Boundary),
            new CreateIndexOptions { Name = ZoneBoundaryIndexName });

        await context.Zones.Indexes
            .CreateOneAsync(zoneBoundaryIndex, options: null, cancellationToken)
            .ConfigureAwait(false);

        var deliveryOrderIndex = new CreateIndexModel<DeliveryDocument>(
            Builders<DeliveryDocument>.IndexKeys.Ascending(delivery => delivery.OrderId),
            new CreateIndexOptions { Name = "delivery-order-id", Unique = true });

        await context.Deliveries.Indexes
            .CreateOneAsync(deliveryOrderIndex, options: null, cancellationToken)
            .ConfigureAwait(false);

        var slotZoneIndex = new CreateIndexModel<SlotDocument>(
            Builders<SlotDocument>.IndexKeys
                .Ascending(slot => slot.ZoneId)
                .Ascending(slot => slot.StartUtc),
            new CreateIndexOptions { Name = "slot-zone-start" });

        await context.Slots.Indexes
            .CreateOneAsync(slotZoneIndex, options: null, cancellationToken)
            .ConfigureAwait(false);

        var outboxPollIndex = new CreateIndexModel<OutboxMessage>(
            Builders<OutboxMessage>.IndexKeys
                .Ascending(message => message.ProcessedOnUtc)
                .Ascending(message => message.OccurredOnUtc),
            new CreateIndexOptions { Name = "outbox-unpublished-poll" });

        await context.Outbox.Indexes
            .CreateOneAsync(outboxPollIndex, options: null, cancellationToken)
            .ConfigureAwait(false);

        LogIndexesEnsured();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Delivery MongoDB indexes ensured")]
    private partial void LogIndexesEnsured();
}
