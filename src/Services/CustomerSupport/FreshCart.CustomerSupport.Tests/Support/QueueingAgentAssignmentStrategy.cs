using FreshCart.CustomerSupport.Api.Assignment;

namespace FreshCart.CustomerSupport.Tests.Support;

/// <summary>
/// Test double whose assignment is fully scripted: each agent made available is handed out once, in
/// order, and a released agent is returned to the tail so the queue-drain path can be driven
/// deterministically. The real round-robin Lua selection is exercised against Redis elsewhere.
/// </summary>
public sealed class QueueingAgentAssignmentStrategy : IAgentAssignmentStrategy
{
    private readonly Queue<Guid> _availableAgents = [];
    private readonly List<Guid> _releasedAgents = [];

    public IReadOnlyList<Guid> ReleasedAgents => _releasedAgents;

    public void MakeAvailable(Guid agentId) => _availableAgents.Enqueue(agentId);

    public Task<Guid?> AssignAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_availableAgents.Count == 0 ? (Guid?)null : _availableAgents.Dequeue());

    public Task ReleaseAsync(Guid agentId, CancellationToken cancellationToken)
    {
        _releasedAgents.Add(agentId);
        _availableAgents.Enqueue(agentId);
        return Task.CompletedTask;
    }
}
