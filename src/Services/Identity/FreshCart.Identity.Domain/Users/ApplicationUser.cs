using Microsoft.AspNetCore.Identity;

namespace FreshCart.Identity.Domain.Users;

/// <summary>
/// FreshCart user aggregate. Extends ASP.NET Identity's <see cref="IdentityUser{TKey}"/> with the
/// fields needed by the storefront: display name, marketing-consent flag, audit timestamps and a
/// security-stamp timestamp used to invalidate cookies after sensitive changes.
/// </summary>
/// <remarks>
/// The aggregate intentionally does not reference EF Core or the database layer. Persistence
/// configuration lives in <c>Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs</c>.
/// </remarks>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    public bool MarketingConsent { get; set; }

    public DateTimeOffset CreatedOnUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSignInOnUtc { get; set; }

    public DateTimeOffset SecurityStampUpdatedOnUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Records a successful sign-in. The application service that authenticates the user must call this
    /// inside the same unit of work as session creation so that audit information is consistent.
    /// </summary>
    public void RecordSuccessfulSignIn(DateTimeOffset occurredOnUtc)
    {
        LastSignInOnUtc = occurredOnUtc;
    }

    /// <summary>
    /// Marks every existing session as invalid (used after password change, MFA enable/disable,
    /// or admin-driven forced sign-out).
    /// </summary>
    public void InvalidateExistingSessions(DateTimeOffset occurredOnUtc)
    {
        SecurityStampUpdatedOnUtc = occurredOnUtc;
    }
}
