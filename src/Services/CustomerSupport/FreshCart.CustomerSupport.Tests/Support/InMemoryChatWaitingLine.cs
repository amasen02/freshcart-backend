using FreshCart.CustomerSupport.Api.Assignment;

namespace FreshCart.CustomerSupport.Tests.Support;

/// <summary>
/// In-memory FIFO line for coordinator tests, mirroring the Redis list semantics so a test can drive
/// the queue-drain path without a container. The Redis implementation itself is covered separately.
/// </summary>
public sealed class InMemoryChatWaitingLine : IChatWaitingLine
{
    private readonly LinkedList<Guid> _waiting = [];

    public Task EnqueueAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        _waiting.AddLast(sessionId);
        return Task.CompletedTask;
    }

    public Task RequeueAtFrontAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        _waiting.AddFirst(sessionId);
        return Task.CompletedTask;
    }

    public Task<Guid?> DequeueAsync(CancellationToken cancellationToken)
    {
        if (_waiting.First is null)
        {
            return Task.FromResult<Guid?>(null);
        }

        var head = _waiting.First.Value;
        _waiting.RemoveFirst();
        return Task.FromResult<Guid?>(head);
    }

    public Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        _waiting.Remove(sessionId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> SnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(_waiting.ToList());
}
