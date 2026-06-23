namespace FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

public sealed record OrderConfirmedLine(
    string ProductSku,
    string ProductName,
    string PrimaryCategory,
    int Quantity,
    decimal UnitPrice);
