using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Ordering.Tests.Persistence;

[Collection(OutboxIntegrationCollection.Name)]
public sealed class EntityFrameworkOutboxStoreTests(OutboxIntegrationFixture fixture)
{
    [Fact]
    public async Task ConcurrentDrainersClaimDisjointBatchesSoNoMessageIsPublishedTwice()
    {
        const int MessageCount = 20;
        await ResetAndSeedAsync(MessageCount);

        var storeA = new EntityFrameworkOutboxStore(fixture.CreateDbContext(), TimeProvider.System);
        var storeB = new EntityFrameworkOutboxStore(fixture.CreateDbContext(), TimeProvider.System);

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
        var dbContext = fixture.CreateDbContext();
        await using (dbContext.ConfigureAwait(false))
        {
            await dbContext.Database
                .ExecuteSqlRawAsync($"DELETE FROM [{OrderingSchema.Name}].[OutboxMessages]")
                .ConfigureAwait(false);

            for (var index = 0; index < messageCount; index++)
            {
                dbContext.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = "FreshCart.Ordering.Tests.OutboxProbeEvent",
                    ContentJson = "{}",
                });
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
