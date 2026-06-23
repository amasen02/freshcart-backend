namespace FreshCart.Ordering.Application.Orders.Dtos;

public sealed record OrderAddressDto(
    string Line1,
    string? Line2,
    string City,
    string PostalCode,
    string CountryCode);
