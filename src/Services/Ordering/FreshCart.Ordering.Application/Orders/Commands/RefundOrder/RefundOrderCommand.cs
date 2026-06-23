using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Ordering.Application.Orders.Commands.RefundOrder;

public sealed record RefundOrderCommand(
    Guid OrderId,
    string Reason) : ICommand;
