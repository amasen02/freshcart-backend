namespace FreshCart.Inventory.Api.Models;

public sealed class StockReservation
{
    public required Guid ReservationId { get; init; }

    public required Guid OrderId { get; init; }

    public required StockReservationStatus Status { get; init; }

    public required DateTimeOffset CreatedOnUtc { get; init; }

    public DateTimeOffset? ReleasedOnUtc { get; init; }

    public required IReadOnlyList<StockReservationLine> Lines { get; init; }
}
