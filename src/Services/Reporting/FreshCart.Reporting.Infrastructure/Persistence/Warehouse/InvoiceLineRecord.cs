namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// EF Core row shape for the <c>invoice_lines</c> table.
/// </summary>
public sealed class InvoiceLineRecord
{
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public string ProductSku { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
