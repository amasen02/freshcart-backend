using MongoDB.Bson.Serialization.Attributes;

namespace FreshCart.Delivery.Infrastructure.Persistence.Documents;

/// <summary>
/// Persistence shape of a fleet driver.
/// </summary>
internal sealed class DriverDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public required string DisplayName { get; set; }

    public bool IsActive { get; set; }
}
