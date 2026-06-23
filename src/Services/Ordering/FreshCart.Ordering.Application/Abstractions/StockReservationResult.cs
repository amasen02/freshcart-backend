namespace FreshCart.Ordering.Application.Abstractions;

public sealed record StockReservationResult(
    bool Succeeded,
    Guid? ReservationId,
    string? FailureReason,
    IReadOnlyList<string> UnavailableSkus)
{
    public static StockReservationResult Success(Guid reservationId) =>
        new(true, reservationId, null, []);

    public static StockReservationResult Failure(string failureReason, IReadOnlyList<string> unavailableSkus) =>
        new(false, null, failureReason, unavailableSkus);
}
