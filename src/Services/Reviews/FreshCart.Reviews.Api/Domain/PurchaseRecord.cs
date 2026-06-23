using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace FreshCart.Reviews.Api.Domain;

/// <summary>
/// Local proof that a customer bought a product, recorded from <c>OrderConfirmedIntegrationEvent</c>.
/// Review authorisation and the verified-purchase badge read from these rows, never from a runtime call
/// to Ordering: the purchase fact is carried by the event and owned here (event-carried state transfer).
/// The unique (CustomerId, ProductSku, OrderId) index makes a redelivered confirmation a no-op.
/// </summary>
public sealed class PurchaseRecord
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string ProductSku { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset PurchasedOnUtc { get; set; }

    public static PurchaseRecord Record(
        Guid recordId,
        Guid customerId,
        string productSku,
        Guid orderId,
        DateTimeOffset purchasedOnUtc) => new()
        {
            Id = recordId,
            CustomerId = customerId,
            ProductSku = productSku,
            OrderId = orderId,
            PurchasedOnUtc = purchasedOnUtc,
        };
}
