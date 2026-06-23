using System.Globalization;

namespace FreshCart.Inventory.Tests.Common;

/// <summary>
/// Every integration test works against its own sku so tests sharing the collection-scoped database
/// can never observe each other's rows.
/// </summary>
internal static class UniqueSkuGenerator
{
    public static string CreateProductSku() =>
        $"SKU-{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).ToUpperInvariant()}";
}
