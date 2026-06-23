using FreshCart.Reviews.Api.Domain;
using MongoDB.Driver;

namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// Single place that knows the reviews database name and its two collection names, so the repositories,
/// the aggregation handler and the index initializer all resolve the same collections.
/// </summary>
public sealed class ReviewsMongoContext
{
    public const string ConnectionStringName = "reviewsdb";
    public const string DefaultDatabaseName = "reviewsdb";
    public const string ReviewsCollectionName = "product_reviews";
    public const string PurchasesCollectionName = "purchase_records";

    static ReviewsMongoContext()
    {
        // Driver 3.x maps a nullable Guid through NullableSerializer, which rejects a per-property
        // [BsonGuidRepresentation] attribute, so the representation is set once globally here before
        // any document type is mapped. This must run before the first GetCollection call.
        ReviewsGuidSerialization.EnsureRegistered();
    }

    public ReviewsMongoContext(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        Reviews = database.GetCollection<ProductReview>(ReviewsCollectionName);
        Purchases = database.GetCollection<PurchaseRecord>(PurchasesCollectionName);
    }

    public IMongoCollection<ProductReview> Reviews { get; }

    public IMongoCollection<PurchaseRecord> Purchases { get; }
}
