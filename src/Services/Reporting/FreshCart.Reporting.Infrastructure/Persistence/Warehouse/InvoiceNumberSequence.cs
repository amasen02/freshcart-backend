using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// EF Core row shape for the per-year, per-kind gap-free numbering sequence.
/// </summary>
public sealed class InvoiceNumberSequence
{
    public int Year { get; set; }
    public InvoiceKind Kind { get; set; }
    public long LastSequence { get; set; }
}
