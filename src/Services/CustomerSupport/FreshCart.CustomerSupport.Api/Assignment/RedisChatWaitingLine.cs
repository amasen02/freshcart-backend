using StackExchange.Redis;

namespace FreshCart.CustomerSupport.Api.Assignment;

/// <summary>
/// FIFO waiting line as a Redis list: new waiters are pushed on the right, the next to be served is
/// popped from the left. A list rather than a sorted set because position is purely arrival order,
/// and the list survives a hub restart so a queued customer is not silently dropped.
/// </summary>
public sealed class RedisChatWaitingLine : IChatWaitingLine
{
    private readonly IDatabase _database;

    public RedisChatWaitingLine(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        _database = connectionMultiplexer.GetDatabase();
    }

    public Task EnqueueAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _database.ListRightPushAsync(SupportRedisKeys.WaitingQueue, sessionId.ToString("N"));
    }

    public Task RequeueAtFrontAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _database.ListLeftPushAsync(SupportRedisKeys.WaitingQueue, sessionId.ToString("N"));
    }

    public async Task<Guid?> DequeueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var head = await _database
            .ListLeftPopAsync(SupportRedisKeys.WaitingQueue)
            .ConfigureAwait(false);

        if (head.IsNull)
        {
            return null;
        }

        var headValue = (string?)head;
        return string.IsNullOrEmpty(headValue)
            ? null
            : Guid.ParseExact(headValue, "N");
    }

    public Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _database.ListRemoveAsync(SupportRedisKeys.WaitingQueue, sessionId.ToString("N"));
    }

    public async Task<IReadOnlyList<Guid>> SnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = await _database
            .ListRangeAsync(SupportRedisKeys.WaitingQueue)
            .ConfigureAwait(false);

        var waitingSessions = new List<Guid>(entries.Length);
        foreach (var entry in entries)
        {
            var entryValue = (string?)entry;
            if (!string.IsNullOrEmpty(entryValue))
            {
                waitingSessions.Add(Guid.ParseExact(entryValue, "N"));
            }
        }

        return waitingSessions;
    }
}
