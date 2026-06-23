namespace FreshCart.Ordering.Domain.Orders;

public sealed record Address(
    string Line1,
    string? Line2,
    string City,
    string PostalCode,
    string CountryCode);
