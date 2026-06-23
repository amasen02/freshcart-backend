using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Domain.Drivers;
using FreshCart.Delivery.Domain.Scheduling;
using FreshCart.Delivery.Infrastructure.Persistence.Documents;
using FreshCart.Delivery.Infrastructure.Persistence.Mapping;
using MongoDB.Driver;

namespace FreshCart.Delivery.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB adapter for <see cref="IDriverRepository"/>. The rotation is the active drivers paired with
/// the timestamp of their most recent delivery assignment, which is exactly the input the scheduling
/// policy needs to pick the least-recently-assigned driver. A driver with no deliveries gets a null
/// timestamp and therefore enters the rotation first.
/// </summary>
public sealed class MongoDriverRepository(DeliveryMongoContext context) : IDriverRepository
{
    public async Task<IReadOnlyList<DriverAssignment>> GetActiveDriverRotationAsync(
        CancellationToken cancellationToken)
    {
        var activeDrivers = await context.Drivers
            .Find(Builders<DriverDocument>.Filter.Eq(driver => driver.IsActive, true))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (activeDrivers.Count == 0)
        {
            return [];
        }

        var lastAssignmentByDriver = await ComputeLastAssignmentByDriverAsync(cancellationToken)
            .ConfigureAwait(false);

        return activeDrivers
            .Select(driver => new DriverAssignment(
                driver.Id,
                lastAssignmentByDriver.TryGetValue(driver.Id, out var lastAssignedOnUtc)
                    ? lastAssignedOnUtc
                    : null))
            .ToList();
    }

    public async Task<IReadOnlyList<Driver>> ListAsync(CancellationToken cancellationToken)
    {
        var documents = await context.Drivers
            .Find(Builders<DriverDocument>.Filter.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.Select(DocumentMapper.ToDomain).ToList();
    }

    public Task AddAsync(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);

        return context.Drivers
            .InsertOneAsync(DocumentMapper.ToDocument(driver), options: null, cancellationToken);
    }

    public async Task<bool> HasAnyDriversAsync(CancellationToken cancellationToken)
    {
        var count = await context.Drivers
            .CountDocumentsAsync(
                Builders<DriverDocument>.Filter.Empty,
                new CountOptions { Limit = 1 },
                cancellationToken)
            .ConfigureAwait(false);

        return count > 0;
    }

    private async Task<Dictionary<Guid, DateTimeOffset>> ComputeLastAssignmentByDriverAsync(
        CancellationToken cancellationToken)
    {
        var assignedDeliveries = await context.Deliveries
            .Find(Builders<DeliveryDocument>.Filter.Ne(delivery => delivery.DriverId, null))
            .Project(delivery => new { delivery.DriverId, delivery.CreatedOnUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var lastAssignmentByDriver = new Dictionary<Guid, DateTimeOffset>();
        foreach (var assignment in assignedDeliveries)
        {
            if (assignment.DriverId is not { } driverId)
            {
                continue;
            }

            if (!lastAssignmentByDriver.TryGetValue(driverId, out var current)
                || assignment.CreatedOnUtc > current)
            {
                lastAssignmentByDriver[driverId] = assignment.CreatedOnUtc;
            }
        }

        return lastAssignmentByDriver;
    }
}
