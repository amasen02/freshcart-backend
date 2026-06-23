using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Domain.Slots;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using FreshCart.Delivery.Infrastructure.Persistence.Mapping;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB adapter for <see cref="ISlotRepository"/>. The open-capacity predicate is expressed as a
/// field-to-field comparison so the filter runs server-side rather than materialising every slot.
/// </summary>
public sealed class MongoSlotRepository(DeliveryMongoContext context) : ISlotRepository
{
    private static FilterDefinition<SlotDocument> HasFreeCapacityFilter =>
        Builders<SlotDocument>.Filter.Where(slot => slot.BookedCount < slot.Capacity);

    public async Task<IReadOnlyList<DeliverySlot>> ListOpenSlotsForZoneAsync(
        Guid zoneId,
        CancellationToken cancellationToken)
    {
        var filter = Builders<SlotDocument>.Filter.And(
            Builders<SlotDocument>.Filter.Eq(slot => slot.ZoneId, zoneId),
            HasFreeCapacityFilter);

        var documents = await context.Slots
            .Find(filter)
            .SortBy(slot => slot.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.Select(DocumentMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<DeliverySlot>> ListOpenSlotsOnDateAsync(
        DateOnly dateUtc,
        CancellationToken cancellationToken)
    {
        var startOfDay = new DateTimeOffset(dateUtc.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var startOfNextDay = startOfDay.AddDays(1);

        var filter = Builders<SlotDocument>.Filter.And(
            Builders<SlotDocument>.Filter.Gte(slot => slot.StartUtc, startOfDay),
            Builders<SlotDocument>.Filter.Lt(slot => slot.StartUtc, startOfNextDay),
            HasFreeCapacityFilter);

        var documents = await context.Slots
            .Find(filter)
            .SortBy(slot => slot.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.Select(DocumentMapper.ToDomain).ToList();
    }

    public Task UpdateBookingAsync(DeliverySlot slot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var update = Builders<SlotDocument>.Update.Set(stored => stored.BookedCount, slot.BookedCount);

        return context.Slots
            .UpdateOneAsync(
                Builders<SlotDocument>.Filter.Eq(stored => stored.Id, slot.Id),
                update,
                options: null,
                cancellationToken);
    }

    public Task AddAsync(DeliverySlot slot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(slot);

        return context.Slots
            .InsertOneAsync(DocumentMapper.ToDocument(slot), options: null, cancellationToken);
    }

    public async Task<bool> HasAnySlotsAsync(CancellationToken cancellationToken)
    {
        var count = await context.Slots
            .CountDocumentsAsync(
                Builders<SlotDocument>.Filter.Empty,
                new CountOptions { Limit = 1 },
                cancellationToken)
            .ConfigureAwait(false);

        return count > 0;
    }
}
