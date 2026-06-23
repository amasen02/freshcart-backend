using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.Ordering.Application.Checkout.Commands;

/// <summary>
/// Saga-internal command (never leaves the Ordering service) instructing a worker to persist the
/// Order aggregate from the checkout payload. The original event is carried whole so the worker
/// sees exactly what the saga correlated on.
/// </summary>
public sealed record SubmitOrderFromCheckout(BasketCheckoutStartedIntegrationEvent Checkout);
