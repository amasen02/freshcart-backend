using FreshCart.Reporting.Domain.Kpis;

namespace FreshCart.Reporting.Application.Customers.Queries;

/// <summary>
/// Ranked customer rows for the leaderboard widget.
/// </summary>
public sealed record CustomerLeaderboardDto(IReadOnlyList<TopEntityRanking> Rows);
