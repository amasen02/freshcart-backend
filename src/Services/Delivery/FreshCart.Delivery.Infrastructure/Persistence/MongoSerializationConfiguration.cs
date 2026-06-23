using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FreshCart.Delivery.Infrastructure.Persistence;

/// <summary>
/// Registers the BSON serializers the delivery store depends on, exactly once per process. Two choices
/// matter: GUIDs serialize in the standard representation so ids are portable across drivers, and
/// <see cref="DateTimeOffset"/> serializes as a BSON UTC <c>DateTime</c> rather than the default
/// [ticks, offset] array. The array form is not range-queryable, and the slot listing filters by start
/// time; every timestamp in this service is constructed at UTC offset zero, so the conversion is lossless.
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

            isRegistered = true;
        }
    }
}
