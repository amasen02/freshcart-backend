using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Application.Abstractions;

public interface IPaymentReadQueries
{
    Task<PaymentReadModel?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<PaginatedResult<PaymentReadModel>> GetPaymentsPageAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken);
}
