using StackExchange.Redis;

namespace FreshCart.CustomerSupport.Api.Assignment;

/// <summary>
/// Round-robin assignment over the availability sorted set. The selection runs as a single Lua script
/// because choosing the least-loaded agent and charging them the new chat must be atomic across
/// replicas: two hub instances reading then writing separately would hand the same idle agent to two
/// customers at once.
/// </summary>
public sealed class RedisAgentAssignmentStrategy : IAgentAssignmentStrategy
{
    // Picks the lowest-scored agent, breaking ties by who joined earliest (the joined list at KEYS[2])
    // rather than by member ordinal, then charges them one chat. Returns the chosen member or nil.
    private const string AssignLeastLoadedAgentScript =
        """
        local candidates = redis.call('ZRANGE', KEYS[1], 0, 0, 'WITHSCORES')
        if (#candidates == 0) then
            return nil
        end
        local lowestScore = candidates[2]
        local tied = redis.call('ZRANGEBYSCORE', KEYS[1], lowestScore, lowestScore)
        local joinOrder = redis.call('LRANGE', KEYS[2], 0, -1)
        local chosen = nil
        for _, member in ipairs(joinOrder) do
            for _, candidate in ipairs(tied) do
                if (member == candidate) then
                    chosen = member
                    break
                end
            end
            if (chosen ~= nil) then
                break
            end
        end
        if (chosen == nil) then
            chosen = tied[1]
        end
        redis.call('ZINCRBY', KEYS[1], 1, chosen)
        return chosen
        """;

    // Decrements an agent's load but clamps at zero so a double release (disconnect racing EndChat)
    // can never push the score negative and make a busy agent look idle.
    private const string ReleaseAgentScript =
        """
        local current = redis.call('ZSCORE', KEYS[1], ARGV[1])
        if (current == false) then
            return 0
        end
        if (tonumber(current) <= 0) then
            return 0
        end
        return redis.call('ZINCRBY', KEYS[1], -1, ARGV[1])
        """;

    private readonly IDatabase _database;

    public RedisAgentAssignmentStrategy(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<Guid?> AssignAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _database
            .ScriptEvaluateAsync(
                AssignLeastLoadedAgentScript,
                [SupportRedisKeys.ActiveAgents, SupportRedisKeys.JoinedAgents])
            .ConfigureAwait(false);

        if (result.IsNull)
        {
            return null;
        }

        var chosenMember = (string?)result;
        return string.IsNullOrEmpty(chosenMember)
            ? null
            : Guid.ParseExact(chosenMember, "N");
    }

    public Task ReleaseAsync(Guid agentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _database.ScriptEvaluateAsync(
            ReleaseAgentScript,
            [SupportRedisKeys.ActiveAgents],
            [agentId.ToString("N")]);
    }
}
