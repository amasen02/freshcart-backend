using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Common.Models;

namespace FreshCart.Reporting.Application.Delivery.Queries;

/// <summary>
/// Returns delivery punctuality numbers for the selected period.
/// </summary>
public sealed record GetDeliveryPerformanceQuery(PeriodSelector Period) : IQuery<DeliveryPerformanceSummary>;
