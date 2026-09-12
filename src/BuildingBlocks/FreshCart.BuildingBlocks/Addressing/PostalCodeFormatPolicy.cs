using System.Text.RegularExpressions;

namespace FreshCart.BuildingBlocks.Addressing;

/// <summary>
/// Checks the structural shape of postal codes for the countries currently offered by the storefront.
/// Unknown country codes remain governed by the caller's generic length and required-value rules until a
/// country-specific format is deliberately added.
/// The national shape references are the UPU Postal Addressing Systems (Germany and France), GOV.UK
/// postcode input guidance, USPS Web Tools and ZIP Code guidance, the official Eircode Code of Practice,
/// and Australia Post's postcode data guide:
/// https://www.upu.int/en/Postal-Solutions/Programmes-Services/Addressing-Solutions,
/// https://design-system.service.gov.uk/components/text-input/,
/// https://www.usps.com/business/web-tools-apis/address-information-api.htm,
/// https://faq.usps.com/articles/Knowledge/ZIP-Code-The-Basics,
/// https://www.eircode.ie/docs/default-source/common/code-of-practice-version-7b23930b5-1cbd-4b89-bafe-bcd6f72438f8.pdf,
/// https://auspost.com.au/content/dam/auspost_corp/media/documents/online-self-service-products-data-guide.pdf.
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

    [GeneratedRegex(@"\A(?:(?:[A-Z]{1,2}[0-9][A-Z0-9]?)|GIR) ?[0-9][A-Z]{2}\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UnitedKingdomRegex();

    [GeneratedRegex(@"\A[0-9]{5}(?:-[0-9]{4})?\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UnitedStatesRegex();

    [GeneratedRegex(@"\A(?:[A-Z][0-9]{2}|D6W) ?[A-Z0-9]{4}\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex IrelandRegex();

    [GeneratedRegex(@"\A[0-9]{5}\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex FiveDigitNumericRegex();

    [GeneratedRegex(@"\A[0-9]{4}\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex FourDigitNumericRegex();
}
