namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Stock-level headline numbers across the whole catalog.
/// </summary>
public sealed record InventoryHealthSummary(
    long TotalSkus,
    long OutOfStockCount,
    long LowStockCount,
    long OverstockCount,
    decimal InventoryValueAtCost);
