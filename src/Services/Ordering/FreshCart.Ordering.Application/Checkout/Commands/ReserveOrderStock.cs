using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.Ordering.Application.Checkout.Commands;

/// <summary>
/// Saga-internal command (never leaves the Ordering service) instructing a worker to reserve
/// stock with the Inventory service for every line of the order.
/// </summary>
public sealed record ReserveOrderStock(Guid OrderId, IReadOnlyList<CheckoutLine> Lines);
