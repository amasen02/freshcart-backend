namespace FreshCart.Identity.Domain.Roles;

/// <summary>
/// Canonical role-name constants. Use these wherever roles are referenced in code so that a typo cannot
/// silently break authorisation.
/// </summary>
public static class CanonicalRoles
{
    public const string Customer = nameof(Customer);

    public const string SupportAgent = nameof(SupportAgent);

    public const string Administrator = nameof(Administrator);
}
