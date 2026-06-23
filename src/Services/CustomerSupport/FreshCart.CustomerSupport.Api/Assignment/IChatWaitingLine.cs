namespace FreshCart.CustomerSupport.Api.Assignment;

/// <summary>
/// FIFO holding area for customers who arrive while every agent is busy or offline. A Redis list so
/// the order survives across replicas and a restart of any single hub instance.
/// </summary>
public interface IChatWaitingLine
{
    /// <summary>Appends a session to the back of the waiting line.</summary>
    Task EnqueueAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a session to the FRONT of the line so a dequeue that could not be completed (no agent
    /// free, or the persistence of the assignment failed) does not cost the customer their place. This
    /// preserves the strict arrival-order guarantee that <see cref="EnqueueAsync"/> would break.
    /// </summary>
    Task RequeueAtFrontAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>Removes and returns the session at the front of the line, or null when empty.</summary>
    Task<Guid?> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>Removes a specific session from the line regardless of position (e.g. it ended while waiting).</summary>
    Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Ordered snapshot of the waiting sessions, front first. Used to recompute every waiter's
    /// position after the front of the line drains.
    /// </summary>
    Task<IReadOnlyList<Guid>> SnapshotAsync(CancellationToken cancellationToken);
}
