using FreshCart.Inventory.Api.Models;

namespace FreshCart.Inventory.Api.Services;

public interface IStockReservationService
{
    Task<StockReservationResult> ReserveAsync(
        Guid orderId,
        IReadOnlyCollection<StockReservationLine> requestedLines,
        CancellationToken cancellationToken);

    Task<bool> ReleaseAsync(Guid orderId, CancellationToken cancellationToken);
}
