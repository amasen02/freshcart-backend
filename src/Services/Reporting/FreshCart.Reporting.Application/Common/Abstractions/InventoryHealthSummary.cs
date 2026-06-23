namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Stock-level headline numbers across the whole catalog.
/// </summary>
public sealed record InventoryHealthSummary(
    int TotalSkus,
    int OutOfStockCount,
    int LowStockCount,
    int OverstockCount,
    decimal InventoryValueAtCost);
