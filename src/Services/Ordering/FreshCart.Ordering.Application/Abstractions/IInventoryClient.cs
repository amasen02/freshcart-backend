namespace FreshCart.Ordering.Application.Abstractions;

/// <summary>
/// Port over the Inventory gRPC service. Reservation failures are a business outcome and come
/// back as a result; transport faults propagate as exceptions so the bus retry policy applies.
/// </summary>
public interface IInventoryClient
{
    Task<StockReservationResult> ReserveStockAsync(
        Guid orderId,
        IReadOnlyList<StockReservationLine> lines,
        CancellationToken cancellationToken);

    Task ReleaseReservationAsync(Guid orderId, CancellationToken cancellationToken);
}
