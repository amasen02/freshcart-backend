namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Output format produced by an <see cref="IInvoiceRenderer"/> implementation.
/// </summary>
public enum InvoiceRenderingFormat
{
    Pdf,
    Html,
}
