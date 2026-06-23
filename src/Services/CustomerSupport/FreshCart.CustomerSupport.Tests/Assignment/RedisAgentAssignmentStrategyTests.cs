using FluentAssertions;
using FreshCart.CustomerSupport.Api.Assignment;
using FreshCart.CustomerSupport.Tests.Support;
using StackExchange.Redis;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Assignment;

[Collection(RedisFixture.CollectionName)]
public sealed class RedisAgentAssignmentStrategyTests : IAsyncLifetime
{
    private static readonly Guid FirstAgent = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondAgent = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ThirdAgent = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private const string AgentDisplayName = "Agent under test";

    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisAgentAvailabilityRegistry _registry;
    private readonly RedisAgentAssignmentStrategy _strategy;

    public RedisAgentAssignmentStrategyTests(RedisFixture redisFixture)
    {
        ArgumentNullException.ThrowIfNull(redisFixture);

        var configurationOptions = ConfigurationOptions.Parse(redisFixture.ConnectionString);
        configurationOptions.AllowAdmin = true;
        _connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        _registry = new RedisAgentAvailabilityRegistry(_connectionMultiplexer);
        _strategy = new RedisAgentAssignmentStrategy(_connectionMultiplexer);
    }

    public Task InitializeAsync()
    {
        var endpoint = _connectionMultiplexer.GetEndPoints()[0];
        return _connectionMultiplexer.GetServer(endpoint).FlushDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _connectionMultiplexer.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task WithTwoIdleAgentsTheOneThatJoinedFirstIsChosen()
    {
        await _registry.RegisterAsync(FirstAgent, AgentDisplayName, CancellationToken.None);
        await _registry.RegisterAsync(SecondAgent, AgentDisplayName, CancellationToken.None);

        var assignedAgentId = await _strategy.AssignAsync(CancellationToken.None);

        assignedAgentId.Should().Be(FirstAgent);
    }

    [Fact]
    public async Task SecondAssignmentSkipsTheNowBusyAgentAndChoosesTheIdleOne()
    {
        await _registry.RegisterAsync(FirstAgent, AgentDisplayName, CancellationToken.None);
        await _registry.RegisterAsync(SecondAgent, AgentDisplayName, CancellationToken.None);

        var firstAssignment = await _strategy.AssignAsync(CancellationToken.None);
        var secondAssignment = await _strategy.AssignAsync(CancellationToken.None);

        firstAssignment.Should().Be(FirstAgent);
        secondAssignment.Should().Be(SecondAgent, "the first agent now carries one chat and is the heavier-loaded option");
    }

    [Fact]
    public async Task WhenNoAgentIsOnlineAssignmentReturnsNullSoTheCustomerIsQueued()
    {
        var assignedAgentId = await _strategy.AssignAsync(CancellationToken.None);

        assignedAgentId.Should().BeNull();
    }

    [Fact]
    public async Task LoadIsSpreadRoundRobinAcrossThreeEquallyIdleAgents()
    {
        await _registry.RegisterAsync(FirstAgent, AgentDisplayName, CancellationToken.None);
        await _registry.RegisterAsync(SecondAgent, AgentDisplayName, CancellationToken.None);
        await _registry.RegisterAsync(ThirdAgent, AgentDisplayName, CancellationToken.None);

        var assignments = new[]
        {
            await _strategy.AssignAsync(CancellationToken.None),
            await _strategy.AssignAsync(CancellationToken.None),
            await _strategy.AssignAsync(CancellationToken.None),
        };

        assignments.Should().Equal(FirstAgent, SecondAgent, ThirdAgent);
    }

    [Fact]
    public async Task ReleasingFreesTheAgentSoTheNextAssignmentPicksThemAgain()
    {
        await _registry.RegisterAsync(FirstAgent, AgentDisplayName, CancellationToken.None);
        await _registry.RegisterAsync(SecondAgent, AgentDisplayName, CancellationToken.None);
        await _strategy.AssignAsync(CancellationToken.None);

        await _strategy.ReleaseAsync(FirstAgent, CancellationToken.None);
        var assignmentAfterRelease = await _strategy.AssignAsync(CancellationToken.None);

        assignmentAfterRelease.Should().Be(FirstAgent, "the released agent is back to zero load and rejoins the rotation");
    }

    [Fact]
    public async Task ReleasingAnAgentThatIsAlreadyIdleCannotPushTheLoadBelowZero()
    {
        await _registry.RegisterAsync(FirstAgent, AgentDisplayName, CancellationToken.None);

        await _strategy.ReleaseAsync(FirstAgent, CancellationToken.None);
        await _strategy.ReleaseAsync(FirstAgent, CancellationToken.None);
        await _strategy.AssignAsync(CancellationToken.None);
        var loadAfterSingleAssign = await ReadAgentLoadAsync(FirstAgent);

        loadAfterSingleAssign.Should().Be(1, "an over-release must not bank negative credit that later hides real load");
    }

    [Fact]
    public async Task DeregisteringRemovesAnAgentFromTheAssignablePool()
    {
        await _registry.RegisterAsync(FirstAgent, AgentDisplayName, CancellationToken.None);
        await _registry.RegisterAsync(SecondAgent, AgentDisplayName, CancellationToken.None);

        await _registry.DeregisterAsync(FirstAgent, CancellationToken.None);
        var assignedAgentId = await _strategy.AssignAsync(CancellationToken.None);

        assignedAgentId.Should().Be(SecondAgent);
    }

    private async Task<double> ReadAgentLoadAsync(Guid agentId)
    {
        var score = await _connectionMultiplexer
            .GetDatabase()
            .SortedSetScoreAsync(SupportRedisKeys.ActiveAgents, agentId.ToString("N"))
            .ConfigureAwait(false);

        return score ?? 0;
    }
}
