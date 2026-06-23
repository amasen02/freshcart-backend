namespace FreshCart.Identity.Api.Endpoints.Account;

/// <summary>
/// Wire shape for the current-user query used by the Angular shell to hydrate session state.
/// </summary>
public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    bool MultiFactorEnabled);
