using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

public sealed record CheckoutCommand(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerDisplayName,
    string PaymentMethod,
    CheckoutAddress BillingAddress,
    CheckoutAddress? ShippingAddress) : ICommand<CheckoutResult>;
