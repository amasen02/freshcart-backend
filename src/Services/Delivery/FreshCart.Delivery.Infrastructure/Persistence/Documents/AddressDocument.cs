namespace FreshCart.Delivery.Infrastructure.Persistence.Documents;

/// <summary>
/// Embedded address sub-document shared by deliveries and pending shipments.
/// </summary>
internal sealed class AddressDocument
{
    public required string Line1 { get; set; }

    public string? Line2 { get; set; }

    public required string City { get; set; }

    public required string PostalCode { get; set; }

    public required string CountryCode { get; set; }
}
