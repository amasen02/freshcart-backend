namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Generic Excel export port. Implementations use ClosedXML, a managed-only library that does
/// not need Office to be installed on the host (works in chiselled Linux containers).
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// Writes a tabular sheet from an enumerable of typed rows. The first row is the header,
    /// derived from <typeparamref name="TRow"/>'s public properties unless a column map is
    /// supplied.
    /// </summary>
    Task<RenderedDocument> ExportTabularAsync<TRow>(
        string sheetName,
        IEnumerable<TRow> rows,
        ExcelExportOptions? options = null,
        CancellationToken cancellationToken = default);
}
