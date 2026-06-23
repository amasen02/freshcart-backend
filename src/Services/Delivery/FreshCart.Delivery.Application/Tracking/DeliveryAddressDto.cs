namespace FreshCart.Delivery.Application.Tracking;

/// <summary>
/// Address shape returned to clients. Separate from the domain value object so the wire contract can
/// evolve independently of the domain model.
/// </summary>
public sealed record DeliveryAddressDto(
    string Line1,
    string? Line2,
    string City,
    string PostalCode,
    string CountryCode);
