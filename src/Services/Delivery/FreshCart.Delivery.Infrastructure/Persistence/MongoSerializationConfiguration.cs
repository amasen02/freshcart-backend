using FreshCart.BuildingBlocks.Messaging.Outbox;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FreshCart.Delivery.Infrastructure.Persistence;

/// <summary>
/// Registers the BSON serializers and class maps the delivery store depends on, exactly once per
/// process. Two serializer choices matter: GUIDs serialize in the standard representation so ids are
/// portable across drivers, and <see cref="DateTimeOffset"/> serializes as a BSON UTC <c>DateTime</c>
/// rather than the default [ticks, offset] array. The array form is not range-queryable, and the slot
/// listing filters by start time; every timestamp in this service is constructed at UTC offset zero, so
/// the conversion is lossless.
/// </summary>
public static class MongoSerializationConfiguration
{
    private static readonly Lock RegistrationGate = new();
    private static bool isRegistered;

    public static void EnsureRegistered()
    {
        if (isRegistered)
        {
            return;
        }

        lock (RegistrationGate)
        {
            if (isRegistered)
            {
                return;
            }

            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            BsonSerializer.TryRegisterSerializer(new DateTimeOffsetSerializer(BsonType.DateTime));
            RegisterOutboxMessageClassMap();

            isRegistered = true;
        }
    }

    // The shared OutboxMessage entity carries no BSON attributes (it must stay storage-agnostic for the
    // Marten and EF stores too), so its mapping is declared here: Id becomes the document _id and the
    // computed IsDeadLettered is unmapped because it has no setter to deserialize back into.
    private static void RegisterOutboxMessageClassMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(OutboxMessage)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<OutboxMessage>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapIdMember(outboxMessage => outboxMessage.Id);
            classMap.UnmapMember(outboxMessage => outboxMessage.IsDeadLettered);
            classMap.SetIgnoreExtraElements(true);
        });
    }
}
