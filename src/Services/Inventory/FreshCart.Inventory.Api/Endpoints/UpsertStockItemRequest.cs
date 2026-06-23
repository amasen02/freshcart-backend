namespace FreshCart.Inventory.Api.Endpoints;

public sealed record UpsertStockItemRequest(string ProductName, int QuantityOnHand);
