using FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;
using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Api.Endpoints.Invoices;

/// <summary>
/// Wire shape for the invoice-generation endpoint; mapped one-to-one onto
/// <see cref="GenerateInvoiceCommand"/>.
/// </summary>
public sealed record GenerateInvoiceRequest(
    Guid OrderId,
    InvoiceKind Kind,
    string CustomerEmail,
    string CustomerDisplayName,
    Guid CustomerId,
    InvoiceAddress BillingAddress,
    InvoiceAddress ShippingAddress,
    IReadOnlyList<InvoiceLineRequest> Lines,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    string? CurrencyCode,
    string? OriginalInvoiceNumber,
    string? Notes);
