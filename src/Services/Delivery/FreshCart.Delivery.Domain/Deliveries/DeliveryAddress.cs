namespace FreshCart.Delivery.Domain.Deliveries;

/// <summary>
/// Postal address a delivery is sent to. Modelled inside the domain so the core never depends on the
/// shared <c>CheckoutAddress</c> integration contract; the application layer translates between them.
/// </summary>
public sealed record DeliveryAddress
{
    public DeliveryAddress(string line1, string? line2, string city, string postalCode, string countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        Line1 = line1;
        Line2 = line2;
        City = city;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    public string Line1 { get; }

    public string? Line2 { get; }

    public string City { get; }

    public string PostalCode { get; }

    public string CountryCode { get; }
}
