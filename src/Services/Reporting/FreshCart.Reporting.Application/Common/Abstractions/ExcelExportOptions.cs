namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Optional presentation settings for <see cref="IExcelExporter.ExportTabularAsync{TRow}"/>.
/// </summary>
public sealed record ExcelExportOptions(
    IReadOnlyList<ExcelColumn>? Columns = null,
    string? Title = null,
    string? Author = null);
