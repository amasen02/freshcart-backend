using FreshCart.Delivery.Domain.Drivers;
using FreshCart.Delivery.Domain.Scheduling;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Port over the driver store. The rotation projection is the data the scheduling policy needs to pick
/// the least-recently-assigned active driver, computed from prior delivery assignments.
/// </summary>
public interface IDriverRepository
{
    Task<IReadOnlyList<DriverAssignment>> GetActiveDriverRotationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Driver>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(Driver driver, CancellationToken cancellationToken);

    Task<bool> HasAnyDriversAsync(CancellationToken cancellationToken);
}
