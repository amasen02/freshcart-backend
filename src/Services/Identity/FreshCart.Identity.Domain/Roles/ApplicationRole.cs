using Microsoft.AspNetCore.Identity;

namespace FreshCart.Identity.Domain.Roles;

/// <summary>
/// FreshCart role. Three canonical roles ship with the system:
/// <list type="bullet">
/// <item><description><c>Customer</c>: default role granted on sign-up.</description></item>
/// <item><description><c>SupportAgent</c>: staff working live chat and order ops.</description></item>
/// <item><description><c>Administrator</c>: back-office access to catalog, inventory, reporting.</description></item>
/// </list>
/// Additional roles can be added at runtime via the administrative UI.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName)
        : base(roleName)
    {
        NormalizedName = roleName.ToUpperInvariant();
    }

    public string? Description { get; set; }

    public DateTimeOffset CreatedOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
