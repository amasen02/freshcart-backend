using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Reviews.Api.Domain;

/// <summary>
/// A customer's review of a single product. This is the persisted document itself rather than a mapped
/// projection of a separate aggregate: the Reviews service is schemaless vertical-slice, so the document
/// owns the small amount of behaviour it has. Guids use the globally registered Standard UUID
/// representation so the unique (ProductSku, CustomerId) index matches the binary subtype the driver
/// writes; timestamps are stored as ISO strings so an export reads naturally.
/// </summary>
public sealed class ProductReview
{
    [BsonId]
    public Guid Id { get; set; }

    public string ProductSku { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerDisplayName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsVerifiedPurchase { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ReviewStatus Status { get; set; }

    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset CreatedOnUtc { get; set; }

    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public DateTimeOffset? ModeratedOnUtc { get; set; }

    [BsonIgnoreIfNull]
    public Guid? ModeratedBy { get; set; }

    public static ProductReview Submit(
        Guid reviewId,
        string productSku,
        Guid customerId,
        string customerDisplayName,
        int rating,
        string title,
        string body,
        bool isVerifiedPurchase,
        DateTimeOffset createdOnUtc) => new()
        {
            Id = reviewId,
            ProductSku = productSku,
            CustomerId = customerId,
            CustomerDisplayName = customerDisplayName,
            Rating = rating,
            Title = title,
            Body = body,
            IsVerifiedPurchase = isVerifiedPurchase,
            Status = ReviewStatus.Pending,
            CreatedOnUtc = createdOnUtc,
        };

    public void ApplyModeration(ModerationDecision decision, Guid moderatorId, DateTimeOffset moderatedOnUtc)
    {
        Status = decision == ModerationDecision.Approved ? ReviewStatus.Approved : ReviewStatus.Rejected;
        ModeratedBy = moderatorId;
        ModeratedOnUtc = moderatedOnUtc;
    }
}
