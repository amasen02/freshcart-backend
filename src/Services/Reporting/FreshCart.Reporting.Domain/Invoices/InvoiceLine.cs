namespace FreshCart.Reporting.Domain.Invoices;

/// <summary>
/// Single line of an issued invoice; amounts are frozen at issue time.
/// </summary>
public sealed record InvoiceLine(
    int LineNumber,
    string ProductSku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LineTotal);
