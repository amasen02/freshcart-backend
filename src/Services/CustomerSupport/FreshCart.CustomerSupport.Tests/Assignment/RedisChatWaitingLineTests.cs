using FluentAssertions;
using FreshCart.CustomerSupport.Api.Assignment;
using FreshCart.CustomerSupport.Tests.Support;
using StackExchange.Redis;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Assignment;

[Collection(RedisFixture.CollectionName)]
public sealed class RedisChatWaitingLineTests : IAsyncLifetime
{
    private static readonly Guid FirstSession = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SecondSession = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid ThirdSession = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisChatWaitingLine _waitingLine;

    public RedisChatWaitingLineTests(RedisFixture redisFixture)
    {
        ArgumentNullException.ThrowIfNull(redisFixture);

        var configurationOptions = ConfigurationOptions.Parse(redisFixture.ConnectionString);
        configurationOptions.AllowAdmin = true;
        _connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        _waitingLine = new RedisChatWaitingLine(_connectionMultiplexer);
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
    public async Task SessionsAreDequeuedInTheOrderTheyWereEnqueued()
    {
        await _waitingLine.EnqueueAsync(FirstSession, CancellationToken.None);
        await _waitingLine.EnqueueAsync(SecondSession, CancellationToken.None);
        await _waitingLine.EnqueueAsync(ThirdSession, CancellationToken.None);

        var firstOut = await _waitingLine.DequeueAsync(CancellationToken.None);
        var secondOut = await _waitingLine.DequeueAsync(CancellationToken.None);
        var thirdOut = await _waitingLine.DequeueAsync(CancellationToken.None);

        firstOut.Should().Be(FirstSession);
        secondOut.Should().Be(SecondSession);
        thirdOut.Should().Be(ThirdSession);
    }

    [Fact]
    public async Task DequeuingAnEmptyLineReturnsNull()
    {
        var head = await _waitingLine.DequeueAsync(CancellationToken.None);

        head.Should().BeNull();
    }

    [Fact]
    public async Task RemovingASessionPullsItOutOfTheMiddleWithoutDisturbingTheRest()
    {
        await _waitingLine.EnqueueAsync(FirstSession, CancellationToken.None);
        await _waitingLine.EnqueueAsync(SecondSession, CancellationToken.None);
        await _waitingLine.EnqueueAsync(ThirdSession, CancellationToken.None);

        await _waitingLine.RemoveAsync(SecondSession, CancellationToken.None);
        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);

        snapshot.Should().Equal(FirstSession, ThirdSession);
    }

    [Fact]
    public async Task RequeuingAtFrontPutsTheSessionAheadOfEveryoneStillWaiting()
    {
        await _waitingLine.EnqueueAsync(SecondSession, CancellationToken.None);
        await _waitingLine.EnqueueAsync(ThirdSession, CancellationToken.None);

        await _waitingLine.RequeueAtFrontAsync(FirstSession, CancellationToken.None);
        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);

        snapshot.Should().Equal(FirstSession, SecondSession, ThirdSession);
    }

    [Fact]
    public async Task SnapshotReturnsTheWaitingSessionsFrontFirstWithoutConsumingThem()
    {
        await _waitingLine.EnqueueAsync(FirstSession, CancellationToken.None);
        await _waitingLine.EnqueueAsync(SecondSession, CancellationToken.None);

        var snapshot = await _waitingLine.SnapshotAsync(CancellationToken.None);
        var stillQueuedHead = await _waitingLine.DequeueAsync(CancellationToken.None);

        snapshot.Should().Equal(FirstSession, SecondSession);
        stillQueuedHead.Should().Be(FirstSession);
    }
}
