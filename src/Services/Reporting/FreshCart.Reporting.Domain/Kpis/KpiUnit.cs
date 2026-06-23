namespace FreshCart.Reporting.Domain.Kpis;

/// <summary>
/// Unit of measure that tells the dashboard how to format a KPI value.
/// </summary>
public enum KpiUnit
{
    Currency,
    Count,
    Percentage,
    Minutes,
    None,
}
