using FluentAssertions;
using FreshCart.Basket.Api.Persistence;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using Marten;
using Xunit;

namespace FreshCart.Basket.Tests.Persistence;

[Collection(OutboxIntegrationCollection.Name)]
public sealed class MartenOutboxStoreTests(OutboxIntegrationFixture fixture)
{
    [Fact]
    public async Task ConcurrentDrainersClaimDisjointBatchesSoNoMessageIsPublishedTwice()
    {
        const int MessageCount = 20;
        await ResetAndSeedAsync(MessageCount);

        await using var sessionA = fixture.DocumentStore.LightweightSession();
        await using var sessionB = fixture.DocumentStore.LightweightSession();
        var storeA = new MartenOutboxStore(sessionA, TimeProvider.System);
        var storeB = new MartenOutboxStore(sessionB, TimeProvider.System);

        var batches = await Task.WhenAll(
            Task.Run(() => storeA.GetUnpublishedAsync(MessageCount, CancellationToken.None)),
            Task.Run(() => storeB.GetUnpublishedAsync(MessageCount, CancellationToken.None)));

        var idsClaimedByA = batches[0].Select(message => message.Id).ToHashSet();
        var idsClaimedByB = batches[1].Select(message => message.Id).ToHashSet();

        idsClaimedByA.Overlaps(idsClaimedByB).Should().BeFalse("two drainers must never claim the same message");
        idsClaimedByA.Union(idsClaimedByB).Should().HaveCount(MessageCount, "every message is claimed exactly once across both drainers");
    }

    private async Task ResetAndSeedAsync(int messageCount)
    {
        await fixture.DocumentStore.Advanced.Clean.DeleteAllDocumentsAsync();

        await using var session = fixture.DocumentStore.LightweightSession();
        for (var index = 0; index < messageCount; index++)
        {
            session.Store(new OutboxMessage
            {
                EventType = "FreshCart.Basket.Tests.OutboxProbeEvent",
                ContentJson = "{}",
            });
        }

        await session.SaveChangesAsync();
    }
}
