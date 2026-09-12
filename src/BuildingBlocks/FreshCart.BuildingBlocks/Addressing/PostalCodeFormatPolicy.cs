using System.Text.RegularExpressions;

namespace FreshCart.BuildingBlocks.Addressing;

/// <summary>
/// Checks the structural shape of postal codes for the countries currently offered by the storefront.
/// Unknown country codes remain governed by the caller's generic length and required-value rules until a
/// country-specific format is deliberately added.
/// </summary>
public static partial class PostalCodeFormatPolicy
{
    /// <summary>
    /// Returns whether the postal code has the expected national shape for a known country code.
    /// This checks formatting only; it does not verify that a code exists or belongs to the address.
    /// </summary>
    public static bool IsValid(string countryCode, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(postalCode))
        {
            return false;
        }

        var normalisedCountryCode = countryCode.Trim().ToUpperInvariant();
        var candidate = postalCode.Trim();

        return normalisedCountryCode switch
        {
            "GB" => UnitedKingdomRegex().IsMatch(candidate),
            "US" => UnitedStatesRegex().IsMatch(candidate),
            "IE" => IrelandRegex().IsMatch(candidate),
            "DE" or "FR" => FiveDigitNumericRegex().IsMatch(candidate),
            "AU" => FourDigitNumericRegex().IsMatch(candidate),
            _ => true,
        };
    }

    [GeneratedRegex(@"^(?:(?:[A-Z]{1,2}\d[A-Z\d]?)|GIR)\s{1,2}\d[A-Z]{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UnitedKingdomRegex();

    [GeneratedRegex(@"^\d{5}(?:-\d{4})?$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UnitedStatesRegex();

    [GeneratedRegex(@"^(?:[A-Z]\d{2}|D6W)\s?[A-Z0-9]{4}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex IrelandRegex();

    [GeneratedRegex(@"^\d{5}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex FiveDigitNumericRegex();

    [GeneratedRegex(@"^\d{4}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex FourDigitNumericRegex();
}
