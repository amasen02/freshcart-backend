using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Application.Inventory.Queries;

public sealed class GetInventoryHealthQueryHandler(IProductReadWarehouse productWarehouse)
    : IQueryHandler<GetInventoryHealthQuery, InventoryHealthSummary>
{
    public Task<InventoryHealthSummary> Handle(GetInventoryHealthQuery query, CancellationToken cancellationToken)
        => productWarehouse.GetInventoryHealthAsync(cancellationToken);
}
