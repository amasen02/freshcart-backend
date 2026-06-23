using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;

/// <summary>
/// Generates an invoice document for a confirmed order and persists both the metadata record and
/// the rendered PDF. Idempotent: re-issuing for the same order returns the existing invoice.
/// </summary>
public sealed record GenerateInvoiceCommand(
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
    string CurrencyCode = "USD",
    string? OriginalInvoiceNumber = null,
    string? Notes = null) : ICommand<GenerateInvoiceResult>;
