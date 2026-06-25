using System.Text.Json;
using FreshCart.BuildingBlocks.Messaging.Events;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using FreshCart.Delivery.Infrastructure.Persistence.Mapping;
using MongoDB.Driver;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Infrastructure.Persistence;

/// <summary>
/// MongoDB-backed <see cref="IDeliveryUnitOfWork"/>. Writes the delivery document and the staged outbox
/// message inside a single multi-document transaction, so either both are durable or neither is — there
/// is no window where a delivery is persisted but its integration event is lost. The single-node replica
/// set the service runs against is what makes the transaction available.
/// </summary>
public sealed class MongoDeliveryUnitOfWork(
    IMongoClient mongoClient,
    DeliveryMongoContext context,
    TimeProvider timeProvider) : IDeliveryUnitOfWork
{
    private static readonly JsonSerializerOptions OutboxSerializerOptions = new(JsonSerializerDefaults.Web);

    public Task PersistScheduledDeliveryAsync(
        DeliveryAggregate delivery,
        IntegrationEvent scheduledEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(scheduledEvent);

        var document = DocumentMapper.ToDocument(delivery);
        var outboxMessage = ToOutboxMessage(scheduledEvent);

        return ExecuteInTransactionAsync(
            async session =>
            {
                await context.Deliveries.InsertOneAsync(session, document, options: null, cancellationToken).ConfigureAwait(false);
                await context.Outbox.InsertOneAsync(session, outboxMessage, options: null, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public Task PersistCompletedDeliveryAsync(
        DeliveryAggregate delivery,
        IntegrationEvent completedEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(completedEvent);

        var document = DocumentMapper.ToDocument(delivery);
        var outboxMessage = ToOutboxMessage(completedEvent);

        return ExecuteInTransactionAsync(
            async session =>
            {
                await context.Deliveries
                    .ReplaceOneAsync(
                        session,
                        Builders<DeliveryDocument>.Filter.Eq(stored => stored.Id, delivery.Id),
                        document,
                        new ReplaceOptions { IsUpsert = false },
                        cancellationToken)
                    .ConfigureAwait(false);
                await context.Outbox.InsertOneAsync(session, outboxMessage, options: null, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    private async Task ExecuteInTransactionAsync(Func<IClientSessionHandle, Task> writeAsync, CancellationToken cancellationToken)
    {
        using var session = await mongoClient.StartSessionAsync(options: null, cancellationToken).ConfigureAwait(false);
        session.StartTransaction();
        try
        {
            await writeAsync(session).ConfigureAwait(false);
            await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Abort on a best-effort basis with a non-cancellable token so a cancellation does not leave
            // the transaction dangling; the original exception is what the caller must see.
            await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private OutboxMessage ToOutboxMessage(IntegrationEvent integrationEvent) => new()
    {
        EventType = integrationEvent.EventType,
        ContentJson = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), OutboxSerializerOptions),
        OccurredOnUtc = timeProvider.GetUtcNow(),
    };
}
