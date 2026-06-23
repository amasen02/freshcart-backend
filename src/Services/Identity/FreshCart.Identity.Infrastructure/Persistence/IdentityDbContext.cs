using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.RefreshTokens;
using FreshCart.Identity.Domain.Roles;
using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Identity bounded context. Inherits the ASP.NET Identity store with our
/// custom <see cref="ApplicationUser"/> and <see cref="ApplicationRole"/>, adds the FreshCart-specific
/// tables <see cref="RefreshToken"/> and <see cref="AuditEvent"/>, and applies every
/// <c>IEntityTypeConfiguration</c> declared in this assembly.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
