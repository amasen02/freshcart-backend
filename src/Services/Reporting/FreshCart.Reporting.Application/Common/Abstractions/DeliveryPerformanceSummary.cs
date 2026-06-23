namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Delivery punctuality headline numbers for a reporting period.
/// </summary>
public sealed record DeliveryPerformanceSummary(
    int TotalDeliveries,
    int OnTimeCount,
    int LateCount,
    int FailedCount,
    decimal AverageDurationMinutes,
    decimal OnTimePercentage);
