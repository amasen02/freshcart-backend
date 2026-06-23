using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Converts an in-memory <see cref="Invoice"/> into a downloadable byte stream. Multiple
/// renderers can coexist (PDF, HTML, JSON). The PDF renderer in production uses QuestPDF; the
/// HTML renderer is useful for in-browser preview and accessibility.
/// </summary>
public interface IInvoiceRenderer
{
    InvoiceRenderingFormat Format { get; }

    Task<RenderedDocument> RenderAsync(Invoice invoice, CancellationToken cancellationToken);
}
