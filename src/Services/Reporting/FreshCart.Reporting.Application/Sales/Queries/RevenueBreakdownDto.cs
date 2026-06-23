using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Sales.Queries;

/// <summary>
/// Category and payment-method revenue breakdowns for the resolved period.
/// </summary>
public sealed record RevenueBreakdownDto(
    ReportingPeriod Period,
    IReadOnlyList<RevenueByCategoryRow> ByCategory,
    IReadOnlyList<RevenueByPaymentMethodRow> ByPaymentMethod);
