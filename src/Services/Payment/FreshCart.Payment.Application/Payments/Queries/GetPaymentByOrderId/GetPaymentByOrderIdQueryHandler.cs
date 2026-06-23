using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Application.Payments.Queries.GetPaymentByOrderId;

public sealed class GetPaymentByOrderIdQueryHandler(IPaymentReadQueries paymentReadQueries)
    : IQueryHandler<GetPaymentByOrderIdQuery, PaymentReadModel>
{
    public async Task<PaymentReadModel> Handle(
        GetPaymentByOrderIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await paymentReadQueries
                .FindByOrderIdAsync(query.OrderId, cancellationToken)
                .ConfigureAwait(false)
            ?? throw new NotFoundException("Payment", query.OrderId);
    }
}
