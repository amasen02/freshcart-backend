namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Revenue contribution of one product category within a reporting period.
/// </summary>
public sealed record RevenueByCategoryRow(string CategoryName, int OrderCount, decimal NetRevenue);
