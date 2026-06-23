namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Maps a row property to a sheet column with an optional display format.
/// </summary>
public sealed record ExcelColumn(string PropertyName, string Header, string? Format = null);
