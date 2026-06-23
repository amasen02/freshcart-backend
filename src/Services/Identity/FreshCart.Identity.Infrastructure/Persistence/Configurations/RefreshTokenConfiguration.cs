using FreshCart.Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshCart.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RefreshTokens");
        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();
        builder.HasIndex(refreshToken => refreshToken.UserId);

        builder.Property(refreshToken => refreshToken.CreatedFromIpAddress).HasMaxLength(64);
        builder.Property(refreshToken => refreshToken.CreatedFromUserAgent).HasMaxLength(256);
        builder.Property(refreshToken => refreshToken.RevocationReason).HasMaxLength(256);
        builder.Property(refreshToken => refreshToken.ReplacedByTokenHash).HasMaxLength(128);
    }
}
