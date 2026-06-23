using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// EF Core row shape for the <c>invoices</c> table.
/// </summary>
public sealed class InvoiceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = default!;
    public InvoiceKind Kind { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = default!;
    public string CustomerDisplayName { get; set; } = default!;
    public InvoiceAddress BillingAddress { get; set; } = default!;
    public InvoiceAddress ShippingAddress { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTimeOffset IssuedOnUtc { get; set; }
    public DateTimeOffset? DueOnUtc { get; set; }
    public string? OriginalInvoiceNumber { get; set; }
    public string? Notes { get; set; }
    public ICollection<InvoiceLineRecord> Lines { get; set; } = [];
}
