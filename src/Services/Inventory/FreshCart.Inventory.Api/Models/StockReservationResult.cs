namespace FreshCart.Inventory.Api.Models;

public sealed record StockReservationResult
{
    private StockReservationResult(bool succeeded, Guid reservationId, string? failureReason, IReadOnlyList<string> unavailableSkus)
    {
        Succeeded = succeeded;
        ReservationId = reservationId;
        FailureReason = failureReason;
        UnavailableSkus = unavailableSkus;
    }

    public bool Succeeded { get; }

    public Guid ReservationId { get; }

    public string? FailureReason { get; }

    public IReadOnlyList<string> UnavailableSkus { get; }

    public static StockReservationResult Success(Guid reservationId) =>
        new(succeeded: true, reservationId, failureReason: null, unavailableSkus: []);

    public static StockReservationResult Failure(string failureReason, IReadOnlyList<string> unavailableSkus) =>
        new(succeeded: false, Guid.Empty, failureReason, unavailableSkus);
}
