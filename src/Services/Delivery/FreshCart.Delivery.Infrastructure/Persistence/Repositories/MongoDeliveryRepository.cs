using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using FreshCart.Delivery.Infrastructure.Persistence.Mapping;
using MongoDB.Driver;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB adapter for <see cref="IDeliveryRepository"/>.
/// </summary>
public sealed class MongoDeliveryRepository(DeliveryMongoContext context) : IDeliveryRepository
{
    public async Task<DeliveryAggregate?> FindByIdAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var document = await context.Deliveries
            .Find(delivery => delivery.Id == deliveryId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document is null ? null : DocumentMapper.ToDomain(document);
    }

    public async Task<DeliveryAggregate?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var document = await context.Deliveries
            .Find(delivery => delivery.OrderId == orderId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document is null ? null : DocumentMapper.ToDomain(document);
    }

    public Task AddAsync(DeliveryAggregate delivery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        return context.Deliveries
            .InsertOneAsync(DocumentMapper.ToDocument(delivery), options: null, cancellationToken);
    }

    public Task UpdateAsync(DeliveryAggregate delivery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var document = DocumentMapper.ToDocument(delivery);
        return context.Deliveries
            .ReplaceOneAsync(
                Builders<DeliveryDocument>.Filter.Eq(stored => stored.Id, delivery.Id),
                document,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);
    }
}
