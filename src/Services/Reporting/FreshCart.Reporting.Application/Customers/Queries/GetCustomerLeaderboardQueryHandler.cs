using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Application.Customers.Queries;

public sealed class GetCustomerLeaderboardQueryHandler(ICustomerReadWarehouse customerWarehouse)
    : IQueryHandler<GetCustomerLeaderboardQuery, CustomerLeaderboardDto>
{
    public async Task<CustomerLeaderboardDto> Handle(
        GetCustomerLeaderboardQuery query,
        CancellationToken cancellationToken)
    {
        var rows = await customerWarehouse
            .GetTopCustomersByLifetimeValueAsync(query.Take, cancellationToken)
            .ConfigureAwait(false);

        return new CustomerLeaderboardDto(rows);
    }
}
