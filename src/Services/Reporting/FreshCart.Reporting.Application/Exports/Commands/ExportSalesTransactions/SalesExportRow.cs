namespace FreshCart.Reporting.Application.Exports.Commands.ExportSalesTransactions;

/// <summary>
/// Flat row shape written to the Excel sheet, one row per aggregation day.
/// </summary>
public sealed record SalesExportRow(
    string Day,
    int OrderCount,
    decimal GrossRevenue,
    decimal DiscountTotal,
    decimal RefundTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    decimal NetRevenue,
    decimal AverageOrderValue,
    decimal RefundRatePercent);
