namespace FreshCart.Ordering.Infrastructure.Persistence;

/// <summary>
/// SQL decimal precision for every money column. Eighteen total digits with two decimals matches the
/// platform money contract (decimal, two-place rounding at presentation boundaries) while leaving
/// ample headroom for order totals.
/// </summary>
public static class MoneyPrecision
{
    public const int TotalDigits = 18;

    public const int DecimalDigits = 2;
}
