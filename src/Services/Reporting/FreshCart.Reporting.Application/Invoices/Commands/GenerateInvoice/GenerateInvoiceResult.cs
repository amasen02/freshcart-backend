namespace FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;

/// <summary>
/// Identity, number and signed download location of the generated (or pre-existing) invoice.
/// </summary>
public sealed record GenerateInvoiceResult(
    Guid InvoiceId,
    string InvoiceNumber,
    Uri DownloadUri,
    decimal GrandTotal,
    string CurrencyCode);
