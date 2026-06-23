namespace FreshCart.Reporting.Application.Invoices.Queries.DownloadInvoice;

/// <summary>
/// Signed, time-boxed download location plus the invoice headline values.
/// </summary>
public sealed record DownloadInvoiceResult(
    string InvoiceNumber,
    Uri SignedDownloadUri,
    decimal GrandTotal,
    string CurrencyCode,
    DateTimeOffset IssuedOnUtc);
