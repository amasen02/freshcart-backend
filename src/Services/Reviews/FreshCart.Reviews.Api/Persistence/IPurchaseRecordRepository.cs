using FreshCart.Reviews.Api.Domain;

namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// Persistence port for the local purchase entitlements that back review authorisation and the
/// verified-purchase badge.
/// </summary>
public interface IPurchaseRecordRepository
{
    Task<bool> HasPurchasedAsync(Guid customerId, string productSku, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the entitlement, swallowing the duplicate-key write that a redelivered confirmation
    /// produces so the consumer stays idempotent.
    /// </summary>
    /// <returns><see langword="true"/> when a new row was written; <see langword="false"/> when the
    /// (CustomerId, ProductSku, OrderId) entitlement already existed.</returns>
    Task<bool> TryRecordAsync(PurchaseRecord purchaseRecord, CancellationToken cancellationToken);
}
