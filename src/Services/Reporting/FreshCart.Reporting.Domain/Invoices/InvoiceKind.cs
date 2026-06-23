namespace FreshCart.Reporting.Domain.Invoices;

/// <summary>
/// Distinguishes a sale, a credit note (refund or adjustment) and a pro-forma (quote-style)
/// invoice issued before payment is captured.
/// </summary>
public enum InvoiceKind
{
    Sale,
    CreditNote,
    ProForma,
}
