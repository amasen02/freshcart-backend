using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Application.Inventory.Queries;

/// <summary>
/// Returns catalog-wide stock-level health numbers for the inventory dashboard tile.
/// </summary>
public sealed record GetInventoryHealthQuery : IQuery<InventoryHealthSummary>;
