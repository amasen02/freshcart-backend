namespace FreshCart.Identity.Application.Common.Models;

/// <summary>
/// Read-model returned by sign-in / current-user endpoints. Intentionally does not expose security
/// stamps or password fields.
/// </summary>
public sealed record AuthenticationProfile(
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    bool MultiFactorEnabled);
