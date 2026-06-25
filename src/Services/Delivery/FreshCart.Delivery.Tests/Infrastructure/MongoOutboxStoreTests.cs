using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Delivery.Infrastructure.Persistence;
using FreshCart.Delivery.Tests.Support;
using MongoDB.Driver;

namespace FreshCart.Delivery.Tests.Infrastructure;

/// <summary>
/// Proves the multi-instance claim guard (the DLV-002/003 equivalent of BSK-01) for the MongoDB outbox
/// store: two publisher replicas draining concurrently claim disjoint batches, a published message is
/// never handed out again, a transient failure releases the claim until the message dead-letters at the
/// retry ceiling, and a claim stranded by a crashed replica is re-taken once its lease lapses.
/// </summary>
[Collection(MongoIntegrationCollection.Name)]
public sealed class MongoOutboxStoreTests(MongoIntegrationFixture fixture)
{
    private const string ProbeEventType = "FreshCart.Delivery.Tests.OutboxProbeEvent";

    [Fact]
    public async Task ConcurrentDrainersClaimDisjointBatchesSoNoMessageIsPublishedTwice()
    {
        const int MessageCount = 20;
        var context = fixture.CreateIsolatedContext();
        await SeedUnclaimedMessagesAsync(context, MessageCount);

        var storeA = new MongoOutboxStore(context, TimeProvider.System);
        var storeB = new MongoOutboxStore(context, TimeProvider.System);

        var batches = await Task.WhenAll(
            Task.Run(() => storeA.GetUnpublishedAsync(MessageCount, CancellationToken.None)),
            Task.Run(() => storeB.GetUnpublishedAsync(MessageCount, CancellationToken.None)));

        var idsClaimedByA = batches[0].Select(message => message.Id).ToHashSet();
        var idsClaimedByB = batches[1].Select(message => message.Id).ToHashSet();

        idsClaimedByA.Overlaps(idsClaimedByB).Should().BeFalse("two drainers must never claim the same message");
        idsClaimedByA.Union(idsClaimedByB).Should().HaveCount(MessageCount, "every message is claimed exactly once across both drainers");
    }

    [Fact]
    public async Task APublishedMessageIsNotHandedOutAgain()
    {
        var context = fixture.CreateIsolatedContext();
        await SeedUnclaimedMessagesAsync(context, 3);
        var store = new MongoOutboxStore(context, TimeProvider.System);

        var firstClaim = await store.GetUnpublishedAsync(10, CancellationToken.None);
        firstClaim.Should().HaveCount(3);
        await store.MarkAsPublishedAsync(firstClaim, CancellationToken.None);

        var secondClaim = await store.GetUnpublishedAsync(10, CancellationToken.None);
        secondClaim.Should().BeEmpty();
    }

    [Fact]
    public async Task ATransientFailureReleasesTheClaimUntilTheMessageIsDeadLetteredAtMaxAttempts()
    {
        const int MaxRetryAttempts = 3;
        var context = fixture.CreateIsolatedContext();
        await SeedUnclaimedMessagesAsync(context, 1);
        var store = new MongoOutboxStore(context, TimeProvider.System);

        for (var attempt = 1; attempt < MaxRetryAttempts; attempt++)
        {
            var claimed = await store.GetUnpublishedAsync(10, CancellationToken.None);
            claimed.Should().ContainSingle("a released claim makes the message available to the next drain cycle");
            await store.MarkAsFailedAsync(claimed[0], "transient publish failure", MaxRetryAttempts, CancellationToken.None);
            claimed[0].IsDeadLettered.Should().BeFalse();
        }

        var finalAttempt = await store.GetUnpublishedAsync(10, CancellationToken.None);
        finalAttempt.Should().ContainSingle();
        await store.MarkAsFailedAsync(finalAttempt[0], "final failure", MaxRetryAttempts, CancellationToken.None);
        finalAttempt[0].IsDeadLettered.Should().BeTrue();

        var afterDeadLetter = await store.GetUnpublishedAsync(10, CancellationToken.None);
        afterDeadLetter.Should().BeEmpty("a dead-lettered message carries a terminal processed stamp and is no longer polled");
    }

    [Fact]
    public async Task AMessageWhoseClaimLeaseHasLapsedIsReclaimed()
    {
        var nowUtc = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var context = fixture.CreateIsolatedContext();
        var staleMessage = new OutboxMessage
        {
            EventType = ProbeEventType,
            ContentJson = "{}",
            OccurredOnUtc = nowUtc.AddMinutes(-10),
            ClaimId = Guid.NewGuid(),
            ClaimedOnUtc = nowUtc - OutboxMessage.ClaimLeaseTimeout - TimeSpan.FromMinutes(1),
        };
        await context.Outbox.InsertOneAsync(staleMessage);

        var store = new MongoOutboxStore(context, new FixedTimeProvider(nowUtc));
        var reclaimed = await store.GetUnpublishedAsync(10, CancellationToken.None);

        reclaimed.Should().ContainSingle("a claim whose lease has lapsed is taken over so a crashed replica cannot strand the message");
        reclaimed[0].Id.Should().Be(staleMessage.Id);
    }

    private static Task SeedUnclaimedMessagesAsync(DeliveryMongoContext context, int messageCount)
    {
        var firstOccurredOnUtc = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var messages = Enumerable.Range(0, messageCount).Select(index => new OutboxMessage
        {
            EventType = ProbeEventType,
            ContentJson = "{}",
            OccurredOnUtc = firstOccurredOnUtc.AddSeconds(index),
        });

        return context.Outbox.InsertManyAsync(messages);
    }
}
