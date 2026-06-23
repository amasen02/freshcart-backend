using System.Globalization;
using System.Runtime.InteropServices;

namespace FreshCart.Reporting.Domain.Invoices;

/// <summary>
/// Strongly-typed invoice number with stable gap-free yearly sequencing.
/// Format: <c>INV-YYYY-NNNNNN</c> (sale) or <c>CR-YYYY-NNNNNN</c> (credit note).
/// </summary>
/// <remarks>
/// Gap-free numbering is required by most tax jurisdictions. The sequence is allocated by the
/// repository under a transactional row-lock; this struct only formats and parses.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct InvoiceNumber(InvoiceKind Kind, int Year, long Sequence)
{
    private const int MinimumYear = 2000;

    private const int MaximumYear = 9999;

    public string Value => string.Create(
        CultureInfo.InvariantCulture,
        $"{Prefix(Kind)}-{Year:0000}-{Sequence:000000}");

    public override string ToString() => Value;

    public static InvoiceNumber Allocate(InvoiceKind kind, int year, long nextSequence)
    {
        if (year is < MinimumYear or > MaximumYear)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be a four-digit value.");
        }

        if (nextSequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(nextSequence), "Sequence is 1-based.");
        }

        return new InvoiceNumber(kind, year, nextSequence);
    }

    public static bool TryParse(string candidate, out InvoiceNumber invoiceNumber)
    {
        invoiceNumber = default;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var parts = candidate.Split('-');
        if (parts.Length != 3)
        {
            return false;
        }

        var kind = parts[0] switch
        {
            "INV" => InvoiceKind.Sale,
            "CR"  => InvoiceKind.CreditNote,
            "PF"  => InvoiceKind.ProForma,
            _     => (InvoiceKind?)null,
        };

        if (kind is null
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
            || !long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sequence))
        {
            return false;
        }

        invoiceNumber = new InvoiceNumber(kind.Value, year, sequence);
        return true;
    }

    private static string Prefix(InvoiceKind kind) => kind switch
    {
        InvoiceKind.Sale       => "INV",
        InvoiceKind.CreditNote => "CR",
        InvoiceKind.ProForma   => "PF",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
