namespace FreshCart.Reporting.Application.Exports.Commands.ExportSalesTransactions;

/// <summary>
/// Finished workbook bytes plus the metadata the HTTP layer needs to stream a download.
/// </summary>
public sealed record ExportSalesTransactionsResult(
    string FileName,
    string ContentType,
    byte[] Content);
