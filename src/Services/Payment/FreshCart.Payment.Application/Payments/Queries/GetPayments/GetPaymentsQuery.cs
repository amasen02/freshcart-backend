using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Application.Payments.Queries.GetPayments;

public sealed record GetPaymentsQuery(PaginationRequest Pagination)
    : IQuery<PaginatedResult<PaymentReadModel>>;
