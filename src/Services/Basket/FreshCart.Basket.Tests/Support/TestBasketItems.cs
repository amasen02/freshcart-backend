using FreshCart.Basket.Api.Domain;

namespace FreshCart.Basket.Tests.Support;

/// <summary>Factory for basket line fixtures so tests state only the values they assert on.</summary>
public static class TestBasketItems
{
    public static BasketItem Create(
        Guid? productId = null,
        string productSku = "SKU-0001",
        string productName = "Organic bananas 1kg",
        string primaryCategory = "Fruit",
        decimal unitPrice = 2.50m,
        int quantity = 1,
        bool isDigital = false) => new()
    {
        ProductId = productId ?? Guid.NewGuid(),
        ProductSku = productSku,
        ProductName = productName,
        PrimaryCategory = primaryCategory,
        UnitPrice = unitPrice,
        Quantity = quantity,
        IsDigital = isDigital,
    };
}
