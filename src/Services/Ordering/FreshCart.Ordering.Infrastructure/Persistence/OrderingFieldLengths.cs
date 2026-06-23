namespace FreshCart.Ordering.Infrastructure.Persistence;

/// <summary>
/// Column length budgets for the Ordering tables. Centralised so the write configurations and the
/// saga map agree on the same limits rather than scattering magic numbers across the model.
/// </summary>
public static class OrderingFieldLengths
{
    public const int CurrencyCode = 3;

    public const int Email = 256;

    public const int DisplayName = 256;

    public const int PaymentMethod = 64;

    public const int Sku = 64;

    public const int ProductName = 256;

    public const int Category = 128;

    public const int AddressLine = 256;

    public const int City = 128;

    public const int PostalCode = 32;

    public const int CountryCode = 2;

    public const int FailureReason = 512;

    public const int SagaState = 64;

    public const int EventType = 512;
}
