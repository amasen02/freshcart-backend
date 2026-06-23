using System.Globalization;

namespace FreshCart.Basket.Api.Pricing;

/// <summary>
/// Money crosses the Pricing proto boundary as invariant-culture decimal strings; doubles would
/// silently corrupt cents. These two conversions are the only place that wire format is known.
/// </summary>
public static class GrpcMoney
{
    public static string ToWireFormat(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);

    public static decimal Parse(string wireAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wireAmount);
        return decimal.Parse(wireAmount, NumberStyles.Number, CultureInfo.InvariantCulture);
    }
}
