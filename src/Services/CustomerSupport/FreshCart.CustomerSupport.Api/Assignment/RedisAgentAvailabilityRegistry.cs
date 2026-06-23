using StackExchange.Redis;

namespace FreshCart.CustomerSupport.Api.Assignment;

/// <summary>
/// Redis-backed roster of online agents. An agent joins the scored set at load zero and is appended
/// to the joined list; deregistration removes them from both. Registration uses ZADD NX so a second
/// connection from the same agent (two browser tabs) never resets their accumulated load to zero.
/// </summary>
public sealed class RedisAgentAvailabilityRegistry : IAgentAvailabilityRegistry
{
    private readonly IDatabase _database;

    public RedisAgentAvailabilityRegistry(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task RegisterAsync(Guid agentId, string agentDisplayName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentDisplayName);
        cancellationToken.ThrowIfCancellationRequested();

        var member = agentId.ToString("N");

        // The name is refreshed on every connect so a changed display name takes effect; the score
        // and join order are touched only on first add so a second tab never resets the agent's load.
        await _database
            .HashSetAsync(SupportRedisKeys.AgentDisplayNames, member, agentDisplayName)
            .ConfigureAwait(false);

        var wasAdded = await _database
            .SortedSetAddAsync(SupportRedisKeys.ActiveAgents, member, score: 0, When.NotExists)
            .ConfigureAwait(false);

        if (wasAdded)
        {
            await _database
                .ListRightPushAsync(SupportRedisKeys.JoinedAgents, member)
                .ConfigureAwait(false);
        }
    }

    public async Task DeregisterAsync(Guid agentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var member = agentId.ToString("N");
        await _database.SortedSetRemoveAsync(SupportRedisKeys.ActiveAgents, member).ConfigureAwait(false);
        await _database.ListRemoveAsync(SupportRedisKeys.JoinedAgents, member).ConfigureAwait(false);
        await _database.HashDeleteAsync(SupportRedisKeys.AgentDisplayNames, member).ConfigureAwait(false);
    }

    public async Task<string?> GetDisplayNameAsync(Guid agentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storedName = await _database
            .HashGetAsync(SupportRedisKeys.AgentDisplayNames, agentId.ToString("N"))
            .ConfigureAwait(false);

        return storedName.IsNullOrEmpty ? null : storedName.ToString();
    }

    public Task<long> CountOnlineAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _database.SortedSetLengthAsync(SupportRedisKeys.ActiveAgents);
    }
}
