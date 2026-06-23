using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Reporting.Application.Customers.Queries;

/// <summary>
/// Returns the highest lifetime-value customers for the back-office leaderboard.
/// </summary>
public sealed record GetCustomerLeaderboardQuery(int Take = 20) : IQuery<CustomerLeaderboardDto>;
