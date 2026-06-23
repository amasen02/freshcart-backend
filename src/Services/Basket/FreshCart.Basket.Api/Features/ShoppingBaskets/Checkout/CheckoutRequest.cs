using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

public sealed record CheckoutRequest(
    string PaymentMethod,
    CheckoutAddress BillingAddress,
    CheckoutAddress? ShippingAddress);
