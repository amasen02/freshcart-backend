using FreshCart.CustomerSupport.Api.Assignment;

namespace FreshCart.CustomerSupport.Tests.Support;

/// <summary>
/// Minimal availability registry for coordinator tests: it only has to answer the agent's display
/// name when a session is assigned. The Redis-backed registration and load tracking are covered in
/// the assignment-strategy tests.
/// </summary>
public sealed class StubAgentAvailabilityRegistry : IAgentAvailabilityRegistry
{
    private readonly Dictionary<Guid, string> _displayNames = [];

    public void SetDisplayName(Guid agentId, string displayName) => _displayNames[agentId] = displayName;

    public Task RegisterAsync(Guid agentId, string agentDisplayName, CancellationToken cancellationToken)
    {
        _displayNames[agentId] = agentDisplayName;
        return Task.CompletedTask;
    }

    public Task DeregisterAsync(Guid agentId, CancellationToken cancellationToken)
    {
        _displayNames.Remove(agentId);
        return Task.CompletedTask;
    }

    public Task<string?> GetDisplayNameAsync(Guid agentId, CancellationToken cancellationToken) =>
        Task.FromResult(_displayNames.GetValueOrDefault(agentId));

    public Task<long> CountOnlineAsync(CancellationToken cancellationToken) =>
        Task.FromResult<long>(_displayNames.Count);
}
