using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Ordering.Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(
    Guid OrderId,
    Guid RequestingCustomerId,
    string Reason) : ICommand;
