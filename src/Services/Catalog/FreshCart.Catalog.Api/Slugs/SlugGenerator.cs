using System.Text;

namespace FreshCart.Catalog.Api.Slugs;

/// <summary>
/// Turns display names into lowercase-hyphen URL slugs. Only ASCII letters and digits survive;
/// every other character run collapses into a single hyphen so the output is always route-safe.
/// </summary>
public static class SlugGenerator
{
    private const int MaxSlugLength = 120;
    private const char SegmentSeparator = '-';

    public static string Generate(string sourceText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceText);

        var slugBuilder = new StringBuilder(sourceText.Length);
        var previousWasSeparator = true;

        foreach (var character in sourceText)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slugBuilder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                slugBuilder.Append(SegmentSeparator);
                previousWasSeparator = true;
            }
        }

        if (slugBuilder.Length > MaxSlugLength)
        {
            slugBuilder.Length = MaxSlugLength;
        }

        TrimTrailingSeparator(slugBuilder);

        if (slugBuilder.Length == 0)
        {
            throw new ArgumentException("The text contains no characters usable in a slug.", nameof(sourceText));
        }

        return slugBuilder.ToString();
    }

    private static void TrimTrailingSeparator(StringBuilder slugBuilder)
    {
        while (slugBuilder.Length > 0 && slugBuilder[^1] == SegmentSeparator)
        {
            slugBuilder.Length--;
        }
    }
}
