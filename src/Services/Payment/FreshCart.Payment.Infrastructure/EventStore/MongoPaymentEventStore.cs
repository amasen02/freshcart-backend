using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Domain.Events;
using FreshCart.Payment.Infrastructure.Projections;
using MongoDB.Driver;

namespace FreshCart.Payment.Infrastructure.EventStore;

/// <summary>
/// MongoDB-backed event store. Optimistic concurrency rides on the unique compound index over
/// (PaymentId, Version): a concurrent writer that already appended the same version turns the
/// insert into a duplicate-key error, which surfaces as a <see cref="ConflictException"/>. The
/// one-payment-per-order invariant rides on a partial unique index over the OrderId of the initiating
/// event. Each append also stages a projection marker in the <em>same transaction</em> as the events, so
/// the SQL read model is projected exactly-once by the background projector without a dual write.
/// </summary>
public sealed class MongoPaymentEventStore : IPaymentEventStore
{
    public const string ConnectionStringName = "paymentevents";
    public const string DefaultDatabaseName = "paymentevents";
    public const string CollectionName = "payment_events";

    private const string StreamVersionIndexName = "UX_payment_events_PaymentId_Version";
    private const string OrderInvariantIndexName = "UX_payment_events_OrderId_initiate";
    private const int InitiatingVersion = 1;
    private const int DuplicateKeyErrorCode = 11000;

    private readonly IMongoClient _mongoClient;
    private readonly IMongoCollection<PaymentEventDocument> _eventsCollection;
    private readonly IMongoCollection<PaymentProjectionOutboxDocument> _projectionOutboxCollection;

    public MongoPaymentEventStore(IMongoClient mongoClient, IMongoDatabase eventStoreDatabase)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(eventStoreDatabase);

        _mongoClient = mongoClient;
        _eventsCollection = eventStoreDatabase.GetCollection<PaymentEventDocument>(CollectionName);
        _projectionOutboxCollection = eventStoreDatabase
            .GetCollection<PaymentProjectionOutboxDocument>(PaymentProjectionOutboxDocument.CollectionName);
    }

    public Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var streamVersionIndex = new CreateIndexModel<PaymentEventDocument>(
            Builders<PaymentEventDocument>.IndexKeys
                .Ascending(document => document.PaymentId)
                .Ascending(document => document.Version),
            new CreateIndexOptions { Unique = true, Name = StreamVersionIndexName });

        // One payment per order, enforced at the source of truth: a partial unique index over the OrderId
        // of the version-1 (initiating) event rejects a second initiate for the same order with a
        // duplicate-key error, so two concurrent captures of one order cannot both create a stream.
        var orderInvariantIndex = new CreateIndexModel<PaymentEventDocument>(
            Builders<PaymentEventDocument>.IndexKeys.Ascending(document => document.OrderId),
            new CreateIndexOptions<PaymentEventDocument>
            {
                Unique = true,
                Name = OrderInvariantIndexName,
                PartialFilterExpression = Builders<PaymentEventDocument>.Filter.Eq(document => document.Version, InitiatingVersion),
            });

        return _eventsCollection.Indexes
            .CreateManyAsync([streamVersionIndex, orderInvariantIndex], cancellationToken);
    }

    public async Task AppendAsync(
        Guid orderId,
        Guid paymentId,
        int expectedVersion,
        IReadOnlyList<IPaymentEvent> newEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newEvents);

        if (newEvents.Count == 0)
        {
            throw new ArgumentException("At least one event is required to append.", nameof(newEvents));
        }

        var documents = BuildDocuments(orderId, paymentId, expectedVersion, newEvents);
        var projectionMarker = new PaymentProjectionOutboxDocument
        {
            PaymentId = paymentId,
            OccurredOnUtc = newEvents[^1].OccurredOnUtc,
        };

        using var session = await _mongoClient.StartSessionAsync(options: null, cancellationToken).ConfigureAwait(false);
        session.StartTransaction();
        try
        {
            await _eventsCollection
                .InsertManyAsync(session, documents, new InsertManyOptions { IsOrdered = true }, cancellationToken)
                .ConfigureAwait(false);
            await _projectionOutboxCollection
                .InsertOneAsync(session, projectionMarker, options: null, cancellationToken)
                .ConfigureAwait(false);
            await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (MongoException mongoException) when (IsDuplicateKey(mongoException))
        {
            await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            throw new ConflictException(string.Create(
                CultureInfo.InvariantCulture,
                $"Payment stream {paymentId} (order {orderId}) was modified concurrently; expected version {expectedVersion}."));
        }
        catch
        {
            await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<IReadOnlyList<IPaymentEvent>> LoadStreamAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var documents = await _eventsCollection
            .Find(document => document.PaymentId == paymentId)
            .SortBy(document => document.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents
            .Select(document => PaymentEventSerializer.Deserialize(document.EventType, document.PayloadJson))
            .ToArray();
    }

    public async Task<Guid?> FindStreamIdByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var paymentId = await _eventsCollection
            .Find(document => document.OrderId == orderId)
            .Project(document => document.PaymentId)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return paymentId == Guid.Empty ? null : paymentId;
    }

    private static bool IsDuplicateKey(MongoException exception) => exception switch
    {
        MongoBulkWriteException bulkWrite => bulkWrite.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey),
        MongoWriteException write => write.WriteError?.Category == ServerErrorCategory.DuplicateKey,
        MongoCommandException command => command.Code == DuplicateKeyErrorCode,
        _ => false,
    };

    private static List<PaymentEventDocument> BuildDocuments(
        Guid orderId,
        Guid paymentId,
        int expectedVersion,
        IReadOnlyList<IPaymentEvent> newEvents)
    {
        var documents = new List<PaymentEventDocument>(newEvents.Count);
        var nextVersion = expectedVersion + 1;

        foreach (var paymentEvent in newEvents)
        {
            if (paymentEvent.PaymentId != paymentId || paymentEvent.Version != nextVersion)
            {
                throw new ArgumentException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Event {PaymentEventSerializer.GetEventTypeName(paymentEvent)} (payment {paymentEvent.PaymentId}, version {paymentEvent.Version}) does not continue stream {paymentId} from expected version {expectedVersion}."),
                    nameof(newEvents));
            }

            documents.Add(new PaymentEventDocument
            {
                PaymentId = paymentEvent.PaymentId,
                OrderId = orderId,
                Version = paymentEvent.Version,
                EventType = PaymentEventSerializer.GetEventTypeName(paymentEvent),
                PayloadJson = PaymentEventSerializer.Serialize(paymentEvent),
                OccurredOnUtc = paymentEvent.OccurredOnUtc,
            });

            nextVersion++;
        }

        return documents;
    }
}
