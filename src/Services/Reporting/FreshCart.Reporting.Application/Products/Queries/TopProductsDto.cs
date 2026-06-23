using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Products.Queries;

/// <summary>
/// Ranked product rows together with the resolved period and ranking mode.
/// </summary>
public sealed record TopProductsDto(
    ReportingPeriod Period,
    TopProductsMode Mode,
    IReadOnlyList<TopEntityRanking> Rows);
