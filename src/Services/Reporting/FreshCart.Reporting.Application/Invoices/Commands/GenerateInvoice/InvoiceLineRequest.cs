namespace FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;

/// <summary>
/// Caller-supplied invoice line; line numbers and totals are computed by the handler.
/// </summary>
public sealed record InvoiceLineRequest(
    string ProductSku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount);
