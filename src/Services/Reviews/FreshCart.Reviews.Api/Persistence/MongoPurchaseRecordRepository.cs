using FreshCart.Reviews.Api.Domain;
using MongoDB.Driver;

namespace FreshCart.Reviews.Api.Persistence;

/// <summary>
/// MongoDB-backed purchase-entitlement store. The unique (CustomerId, ProductSku, OrderId) index is the
/// idempotency key: a redelivered confirmation raises a duplicate-key write error which
/// <see cref="TryRecordAsync"/> swallows deliberately rather than failing the consumer.
/// </summary>
public sealed class MongoPurchaseRecordRepository : IPurchaseRecordRepository
{
    private readonly IMongoCollection<PurchaseRecord> _purchases;

    public MongoPurchaseRecordRepository(ReviewsMongoContext mongoContext)
    {
        ArgumentNullException.ThrowIfNull(mongoContext);

        _purchases = mongoContext.Purchases;
    }

    public Task<bool> HasPurchasedAsync(Guid customerId, string productSku, CancellationToken cancellationToken)
    {
        var filterBuilder = Builders<PurchaseRecord>.Filter;
        var entitlement = filterBuilder.And(
            filterBuilder.Eq(record => record.CustomerId, customerId),
            filterBuilder.Eq(record => record.ProductSku, productSku));

        return _purchases.Find(entitlement).AnyAsync(cancellationToken);
    }

    public async Task<bool> TryRecordAsync(PurchaseRecord purchaseRecord, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(purchaseRecord);

        try
        {
            await _purchases.InsertOneAsync(purchaseRecord, options: null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException writeException)
            when (writeException.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
