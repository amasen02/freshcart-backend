namespace FreshCart.Pricing.Grpc.Services;

/// <summary>
/// Money is rounded exactly once, at the end of each computed figure, with banker's rounding so
/// that midpoint cents do not drift totals upward across millions of baskets.
/// </summary>
public static class MoneyRounding
{
    private const int CurrencyDecimalPlaces = 2;

    public static decimal ToCurrency(decimal value) =>
        Math.Round(value, CurrencyDecimalPlaces, MidpointRounding.ToEven);
}
