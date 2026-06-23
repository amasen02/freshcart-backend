namespace FreshCart.CustomerSupport.Api.Assignment;

/// <summary>
/// Redis key names shared by the availability registry and the waiting queue. Naming them in one
/// place keeps the assignment Lua script, the registry and the queue pointing at the same structures.
/// </summary>
public static class SupportRedisKeys
{
    /// <summary>Sorted set of online agents scored by their current active-chat count.</summary>
    public const string ActiveAgents = "support:agents:active";

    /// <summary>Insertion-ordered list of online agents; used only to break score ties fairly.</summary>
    public const string JoinedAgents = "support:agents:joined";

    /// <summary>Hash of agent id to display name, captured from the agent's token on connect.</summary>
    public const string AgentDisplayNames = "support:agents:names";

    /// <summary>FIFO list of session ids waiting for an agent.</summary>
    public const string WaitingQueue = "support:queue";
}
