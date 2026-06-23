namespace FreshCart.Reporting.Domain.Invoices;

/// <summary>
/// Postal address snapshot frozen onto the invoice at issue time.
/// </summary>
public sealed record InvoiceAddress(
    string FullName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? State,
    string PostalCode,
    string Country);
