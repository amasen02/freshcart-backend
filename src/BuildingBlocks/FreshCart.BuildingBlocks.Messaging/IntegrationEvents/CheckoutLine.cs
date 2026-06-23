namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record CheckoutLine(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string PrimaryCategory,
    decimal UnitPrice,
    int Quantity,
    bool IsDigital);
