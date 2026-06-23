namespace FreshCart.Basket.Api.Domain;

/// <summary>
/// One product line inside a customer's basket. The unit price is a snapshot captured from Catalog
/// at add-time so the UI can render instantly; every total shown to the customer is recomputed live
/// by the Pricing service, never from this stored value.
/// </summary>
public sealed class BasketItem
{
    public required Guid ProductId { get; init; }

    public required string ProductSku { get; init; }

    public required string ProductName { get; init; }

    public required string PrimaryCategory { get; init; }

    public required decimal UnitPrice { get; set; }

    public required int Quantity { get; set; }

    public string? ImageUrl { get; init; }

    public required bool IsDigital { get; init; }
}
