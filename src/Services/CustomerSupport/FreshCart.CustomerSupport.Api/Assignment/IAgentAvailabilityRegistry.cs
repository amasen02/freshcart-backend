namespace FreshCart.CustomerSupport.Api.Assignment;

/// <summary>
/// Tracks which agents are online and how loaded each one is. Backed by Redis so the picture is
/// shared across every hub replica rather than living in one process's memory.
/// </summary>
public interface IAgentAvailabilityRegistry
{
    /// <summary>Registers an agent as online with zero active chats; a no-op if already registered.</summary>
    Task RegisterAsync(Guid agentId, string agentDisplayName, CancellationToken cancellationToken);

    /// <summary>Removes an agent from the available pool (on disconnect).</summary>
    Task DeregisterAsync(Guid agentId, CancellationToken cancellationToken);

    /// <summary>The display name captured when the agent connected, or null if they are not online.</summary>
    Task<string?> GetDisplayNameAsync(Guid agentId, CancellationToken cancellationToken);

    /// <summary>Number of agents currently online.</summary>
    Task<long> CountOnlineAsync(CancellationToken cancellationToken);
}
