using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Domain.Events;
using MongoDB.Driver;

namespace FreshCart.Payment.Infrastructure.EventStore;

/// <summary>
/// MongoDB-backed event store. Optimistic concurrency rides on the unique compound index over
/// (PaymentId, Version): a concurrent writer that already appended the same version turns the
/// insert into a duplicate-key error, which surfaces as a <see cref="ConflictException"/>.
/// </summary>
public sealed class MongoPaymentEventStore : IPaymentEventStore
{
    public const string ConnectionStringName = "paymentevents";
    public const string DefaultDatabaseName = "paymentevents";
    public const string CollectionName = "payment_events";

    private const string StreamVersionIndexName = "UX_payment_events_PaymentId_Version";

    private readonly IMongoCollection<PaymentEventDocument> _eventsCollection;

    public MongoPaymentEventStore(IMongoDatabase eventStoreDatabase)
    {
        ArgumentNullException.ThrowIfNull(eventStoreDatabase);

        _eventsCollection = eventStoreDatabase.GetCollection<PaymentEventDocument>(CollectionName);
    }

    public Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var streamVersionIndex = new CreateIndexModel<PaymentEventDocument>(
            Builders<PaymentEventDocument>.IndexKeys
                .Ascending(document => document.PaymentId)
                .Ascending(document => document.Version),
            new CreateIndexOptions { Unique = true, Name = StreamVersionIndexName });

        return _eventsCollection.Indexes.CreateOneAsync(streamVersionIndex, cancellationToken: cancellationToken);
    }

    public async Task AppendAsync(
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

        var documents = BuildDocuments(paymentId, expectedVersion, newEvents);

        try
        {
            await _eventsCollection
                .InsertManyAsync(documents, new InsertManyOptions { IsOrdered = true }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoBulkWriteException bulkWriteException) when (
            bulkWriteException.WriteErrors.Any(writeError => writeError.Category == ServerErrorCategory.DuplicateKey))
        {
            throw new ConflictException(string.Create(
                CultureInfo.InvariantCulture,
                $"Payment stream {paymentId} was modified concurrently; expected version {expectedVersion}."));
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

    private static List<PaymentEventDocument> BuildDocuments(
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
