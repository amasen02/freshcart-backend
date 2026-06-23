using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Infrastructure.Documents.Excel;

/// <summary>
/// ClosedXML-backed Excel exporter. Produces a styled .xlsx with frozen header row, auto-width
/// columns and a properties block (title, author). ClosedXML is a managed-only library that works
/// in chiselled Linux containers, no Office install required.
/// </summary>
public sealed class ClosedXmlExcelExporter : IExcelExporter
{
    public Task<RenderedDocument> ExportTabularAsync<TRow>(
        string sheetName,
        IEnumerable<TRow> rows,
        ExcelExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        ArgumentNullException.ThrowIfNull(rows);
        cancellationToken.ThrowIfCancellationRequested();

        var materialisedRows = rows.ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(Sanitise(sheetName));

        var columns = ResolveColumns<TRow>(options?.Columns);

        ApplyHeader(worksheet, columns);
        ApplyDataRows(worksheet, columns, materialisedRows);
        ApplyWorkbookProperties(workbook, options);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        var fileName = $"{Sanitise(options?.Title ?? sheetName)}-{DateTimeOffset.UtcNow:yyyyMMddHHmm}.xlsx";
        return Task.FromResult(new RenderedDocument(
            FileName: fileName,
            ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Content: memoryStream.ToArray()));
    }

    private static IReadOnlyList<ResolvedColumn> ResolveColumns<TRow>(IReadOnlyList<ExcelColumn>? declared)
    {
        var typeProperties = typeof(TRow).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        if (declared is { Count: > 0 })
        {
            var declaredColumns = new List<ResolvedColumn>(declared.Count);
            foreach (var column in declared)
            {
                var matchingProperty = Array.Find(typeProperties, property => string.Equals(property.Name, column.PropertyName, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException(
                        $"Property \"{column.PropertyName}\" not found on \"{typeof(TRow).FullName}\".");
                declaredColumns.Add(new ResolvedColumn(matchingProperty, column.Header, column.Format));
            }
            return declaredColumns;
        }

        return Array.ConvertAll(typeProperties, property => new ResolvedColumn(property, property.Name, Format: null));
    }

    private static void ApplyHeader(IXLWorksheet worksheet, IReadOnlyList<ResolvedColumn> columns)
    {
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var headerCell = worksheet.Cell(1, columnIndex + 1);
            headerCell.Value = columns[columnIndex].Header;
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
    }

    private static void ApplyDataRows<TRow>(
        IXLWorksheet worksheet,
        IReadOnlyList<ResolvedColumn> columns,
        List<TRow> rows)
    {
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var sourceRow = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var column = columns[columnIndex];
                var cell = worksheet.Cell(rowIndex + 2, columnIndex + 1);
                var value = column.Property.GetValue(sourceRow);
                AssignCellValue(cell, value);
                if (!string.IsNullOrEmpty(column.Format))
                {
                    cell.Style.NumberFormat.Format = column.Format;
                }
            }
        }
    }

    private static void AssignCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Clear();
                return;
            case string text:
                cell.Value = text;
                return;
            case bool flag:
                cell.Value = flag;
                return;
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                return;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.UtcDateTime;
                cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                return;
            case decimal money:
                cell.Value = money;
                cell.Style.NumberFormat.Format = "#,##0.00";
                return;
            case double or float:
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return;
            case int or long or short:
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return;
            default:
                cell.Value = value.ToString();
                return;
        }
    }

    private static void ApplyWorkbookProperties(XLWorkbook workbook, ExcelExportOptions? options)
    {
        workbook.Properties.Author = options?.Author ?? "FreshCart";
        workbook.Properties.Title = options?.Title ?? "FreshCart export";
        workbook.Properties.Created = DateTime.UtcNow;
    }

    private static string Sanitise(string candidate)
        => string.IsNullOrWhiteSpace(candidate)
            ? "export"
            : new string(candidate.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').ToArray());

    private sealed record ResolvedColumn(PropertyInfo Property, string Header, string? Format);
}
