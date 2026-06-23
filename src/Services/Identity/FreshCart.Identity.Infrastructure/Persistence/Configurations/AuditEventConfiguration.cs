using FreshCart.Identity.Domain.AuditEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshCart.Identity.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AuditEvents");
        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.EventType).IsRequired().HasMaxLength(128);
        builder.Property(auditEvent => auditEvent.Description).IsRequired().HasMaxLength(512);
        builder.Property(auditEvent => auditEvent.IpAddress).HasMaxLength(64);
        builder.Property(auditEvent => auditEvent.UserAgent).HasMaxLength(256);
        builder.Property(auditEvent => auditEvent.CorrelationId).HasMaxLength(64);

        builder.HasIndex(auditEvent => auditEvent.OccurredOnUtc);
        builder.HasIndex(auditEvent => new { auditEvent.UserId, auditEvent.OccurredOnUtc });
    }
}
