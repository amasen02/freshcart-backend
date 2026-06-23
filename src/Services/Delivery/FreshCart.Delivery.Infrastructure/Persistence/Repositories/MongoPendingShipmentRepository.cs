using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Application.Shipments;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using FreshCart.Delivery.Infrastructure.Persistence.Mapping;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB adapter for <see cref="IPendingShipmentRepository"/>. The order id is the document key, so
/// the upsert is idempotent against checkout-event redeliveries and the post-scheduling delete is a
/// single keyed removal.
/// </summary>
public sealed class MongoPendingShipmentRepository(DeliveryMongoContext context) : IPendingShipmentRepository
{
    public Task UpsertAsync(PendingShipment pendingShipment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingShipment);

        var document = DocumentMapper.ToDocument(pendingShipment);
        return context.PendingShipments
            .ReplaceOneAsync(
                Builders<PendingShipmentDocument>.Filter.Eq(stored => stored.OrderId, pendingShipment.OrderId),
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
    }

    public async Task<PendingShipment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var document = await context.PendingShipments
            .Find(shipment => shipment.OrderId == orderId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document is null ? null : DocumentMapper.ToDomain(document);
    }

    public Task DeleteByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return context.PendingShipments
            .DeleteOneAsync(
                Builders<PendingShipmentDocument>.Filter.Eq(stored => stored.OrderId, orderId),
                cancellationToken);
    }
}
