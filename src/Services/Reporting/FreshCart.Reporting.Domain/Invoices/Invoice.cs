namespace FreshCart.Reporting.Domain.Invoices;

/// <summary>
/// Customer-facing invoice generated for a confirmed order. Numbered with a stable, gap-free
/// sequence so accounting can reconcile the invoice book month-over-month.
/// </summary>
/// <remarks>
/// The invoice is an <em>immutable record of fact</em> once issued. Adjustments are made via a
/// separate credit note (<see cref="InvoiceKind.CreditNote"/>) referencing the original invoice
/// number, never by editing the issued invoice.
/// </remarks>
public sealed class Invoice
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string InvoiceNumber { get; init; }

    public required InvoiceKind Kind { get; init; }

    public required Guid OrderId { get; init; }

    public required Guid CustomerId { get; init; }

    public required string CustomerEmail { get; init; }

    public required string CustomerDisplayName { get; init; }

    public required InvoiceAddress BillingAddress { get; init; }

    public required InvoiceAddress ShippingAddress { get; init; }

    public required IReadOnlyList<InvoiceLine> Lines { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal DiscountTotal { get; init; }

    public required decimal TaxTotal { get; init; }

    public required decimal ShippingTotal { get; init; }

    public required decimal GrandTotal { get; init; }

    public required string CurrencyCode { get; init; } = "USD";

    public required DateTimeOffset IssuedOnUtc { get; init; }

    public DateTimeOffset? DueOnUtc { get; init; }

    public string? OriginalInvoiceNumber { get; init; }

    public string? Notes { get; init; }
}
