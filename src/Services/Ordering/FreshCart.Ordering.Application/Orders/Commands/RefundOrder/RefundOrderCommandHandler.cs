using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Domain.Orders;
using MediatR;

namespace FreshCart.Ordering.Application.Orders.Commands.RefundOrder;

public sealed class RefundOrderCommandHandler(
    IOrderRepository orderRepository,
    IPaymentClient paymentClient,
    TimeProvider timeProvider) : ICommandHandler<RefundOrderCommand>
{
    public async Task<Unit> Handle(RefundOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository
            .GetByIdAsync(command.OrderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Order", command.OrderId);

        // Validate the domain transition before touching the payment provider so an illegal
        // refund never reaches the provider at all.
        order.Refund(command.Reason, timeProvider.GetUtcNow());

        var paymentId = order.PaymentId
            ?? throw new InternalServerException($"Confirmed order {order.Id} has no payment identifier.");

        var refundResult = await paymentClient
            .RefundPaymentAsync(new PaymentRefundRequest(paymentId, order.GrandTotal.Amount, command.Reason), cancellationToken)
            .ConfigureAwait(false);

        if (!refundResult.Succeeded)
        {
            throw new ConflictException(
                $"The payment provider declined the refund for order {order.Id}: {refundResult.FailureReason}");
        }

        try
        {
            await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConflictException)
        {
            // The RowVersion race was lost. If a concurrent refund of the same order already wrote the
            // single OrderRefunded event, the order is refunded either way and this is an idempotent
            // success. Any other persisted state is a genuine conflict that must surface.
            var persistedStatus = await orderRepository
                .GetPersistedStatusAsync(command.OrderId, cancellationToken)
                .ConfigureAwait(false);

            if (persistedStatus is not OrderStatus.Refunded)
            {
                throw;
            }
        }

        return Unit.Value;
    }
}
