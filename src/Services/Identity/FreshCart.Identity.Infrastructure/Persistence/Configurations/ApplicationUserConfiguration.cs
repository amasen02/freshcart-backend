using FreshCart.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshCart.Identity.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(user => user.DisplayName)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(user => user.CreatedOnUtc).IsRequired();
        builder.Property(user => user.SecurityStampUpdatedOnUtc).IsRequired();

        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
    }
}
