using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Reporting.Application.Invoices.Queries.DownloadInvoice;

/// <summary>
/// Resolves an invoice number to a short-lived signed download link.
/// </summary>
public sealed record DownloadInvoiceQuery(string InvoiceNumber) : IQuery<DownloadInvoiceResult>;
