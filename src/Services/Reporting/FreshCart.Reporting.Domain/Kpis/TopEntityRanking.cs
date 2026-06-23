namespace FreshCart.Reporting.Domain.Kpis;

/// <summary>
/// Generic top-N ranking row used by "top products", "top customers", "top categories", etc.
/// </summary>
public sealed record TopEntityRanking(
    int Rank,
    string EntityId,
    string DisplayName,
    decimal MetricValue,
    int SecondaryCount,
    string? Thumbnail = null);
