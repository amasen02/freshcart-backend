using System.Globalization;
using Grpc.Core;

namespace FreshCart.Pricing.Grpc.Services;

/// <summary>
/// Converts money between <see cref="decimal"/> and the invariant strings used on the proto
/// boundary. Money crosses the wire as text, never as double, so binary floating-point rounding
/// cannot corrupt a price. Group separators are rejected because our own writer never emits them.
/// </summary>
public static class MoneyWireFormat
{
    private const NumberStyles WireNumberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

    public static string ToWire(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static decimal Parse(string value, string fieldName)
    {
        if (!decimal.TryParse(value, WireNumberStyles, CultureInfo.InvariantCulture, out var parsedValue))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Field '{fieldName}' must be a decimal in invariant format."));
        }

        return parsedValue;
    }
}
