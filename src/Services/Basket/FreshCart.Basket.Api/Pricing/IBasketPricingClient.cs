namespace FreshCart.Basket.Api.Pricing;

/// <summary>
/// Port over the Pricing gRPC contract. Handlers depend on this interface, not on generated gRPC
/// stubs, so pricing behavior stays substitutable in tests and the wire format stays an adapter
/// concern.
/// </summary>
public interface IBasketPricingClient
{
    Task<BasketPricingResult> PriceBasketAsync(BasketPricingRequest request, CancellationToken cancellationToken);

    Task<CouponValidationResult> ValidateCouponAsync(
        string couponCode,
        Guid customerId,
        decimal orderSubtotal,
        CancellationToken cancellationToken);
}
