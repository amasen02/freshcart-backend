using System.Data;
using FreshCart.Inventory.Api.Models;

namespace FreshCart.Inventory.Api.Repositories;

public interface IReservationRepository
{
    Task<StockReservation?> GetByOrderIdAsync(Guid orderId, IDbTransaction? transaction, CancellationToken cancellationToken);

    Task InsertAsync(StockReservation reservation, IDbTransaction transaction, CancellationToken cancellationToken);

    Task<bool> MarkReleasedAsync(
        Guid orderId,
        DateTimeOffset releasedOnUtc,
        IDbTransaction transaction,
        CancellationToken cancellationToken);
}
