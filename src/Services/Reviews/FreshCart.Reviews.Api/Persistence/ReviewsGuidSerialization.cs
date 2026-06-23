using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// Registers the Standard UUID representation for every Guid the review documents store. Driver 3.x
/// refuses to serialize a Guid until a representation is chosen, and a per-property attribute cannot
/// reach a nullable Guid (it is wrapped in a NullableSerializer), so the choice is made once here.
/// </summary>
public static class ReviewsGuidSerialization
{
    private static int registered;

    public static void EnsureRegistered()
    {
        // Interlocked guards against two collections being mapped concurrently and racing to register
        // the same serializer, which the driver would reject as a duplicate.
        if (Interlocked.Exchange(ref registered, 1) == 1)
        {
            return;
        }

        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }
}
