namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record CheckoutAddress(string Line1, string? Line2, string City, string PostalCode, string CountryCode);
