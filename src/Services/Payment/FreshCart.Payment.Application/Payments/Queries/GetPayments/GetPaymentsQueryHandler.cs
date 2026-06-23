using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Application.Payments.Queries.GetPayments;

public sealed class GetPaymentsQueryHandler(IPaymentReadQueries paymentReadQueries)
    : IQueryHandler<GetPaymentsQuery, PaginatedResult<PaymentReadModel>>
{
    public Task<PaginatedResult<PaymentReadModel>> Handle(
        GetPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return paymentReadQueries.GetPaymentsPageAsync(query.Pagination.Normalise(), cancellationToken);
    }
}
