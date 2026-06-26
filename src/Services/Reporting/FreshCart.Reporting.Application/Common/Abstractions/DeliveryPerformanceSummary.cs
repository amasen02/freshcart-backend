namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Delivery punctuality headline numbers for a reporting period.
/// </summary>
public sealed record DeliveryPerformanceSummary(
    long TotalDeliveries,
    long OnTimeCount,
    long LateCount,
    long FailedCount,
    decimal AverageDurationMinutes,
    decimal OnTimePercentage);
