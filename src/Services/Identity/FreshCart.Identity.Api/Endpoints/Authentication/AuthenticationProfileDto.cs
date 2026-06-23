namespace FreshCart.Identity.Api.Endpoints.Authentication;

/// <summary>
/// Profile projection shared by the sign-in and current-user responses.
/// </summary>
public sealed record AuthenticationProfileDto(
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    bool MultiFactorEnabled);
