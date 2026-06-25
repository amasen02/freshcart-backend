using FreshCart.BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshCart.Ordering.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the transactional outbox table. The publisher polls for unprocessed rows, so the
/// <c>ProcessedOnUtc</c> filtered index keeps that poll from degrading into a scan as the archive of
/// already-published messages grows.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OutboxMessages", OrderingSchema.Name);

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();

        builder.Property(message => message.EventType).HasMaxLength(OrderingFieldLengths.EventType).IsRequired();
        builder.Property(message => message.ContentJson).IsRequired();
        builder.Property(message => message.OccurredOnUtc).IsRequired();
        builder.Property(message => message.ProcessedOnUtc);
        builder.Property(message => message.Error).HasMaxLength(OrderingFieldLengths.FailureReason);
        builder.Property(message => message.RetryAttempt).IsRequired();
        builder.Property(message => message.ClaimId);
        builder.Property(message => message.ClaimedOnUtc);

        builder.HasIndex(message => message.ProcessedOnUtc)
            .HasFilter("[ProcessedOnUtc] IS NULL");
    }
}
