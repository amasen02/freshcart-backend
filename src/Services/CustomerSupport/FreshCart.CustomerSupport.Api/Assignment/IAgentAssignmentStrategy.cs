namespace FreshCart.CustomerSupport.Api.Assignment;

/// <summary>
/// Round-robin assignment over the available agents. Assignment and release both mutate the same
/// sorted set the registry maintains, so they live behind one abstraction to make the load
/// accounting a single responsibility.
/// </summary>
public interface IAgentAssignmentStrategy
{
    /// <summary>
    /// Picks the least-loaded online agent, increments their load, and returns them; returns
    /// <see langword="null"/> when no agent is online so the caller queues the customer instead.
    /// </summary>
    Task<Guid?> AssignAsync(CancellationToken cancellationToken);

    /// <summary>Decrements an agent's active-chat count, never below zero.</summary>
    Task ReleaseAsync(Guid agentId, CancellationToken cancellationToken);
}
