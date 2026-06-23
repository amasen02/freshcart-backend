namespace FreshCart.Identity.Application.Common.Abstractions;

/// <summary>
/// Result envelope for <see cref="IAccessTokenIssuer.Issue"/>.
/// </summary>
public sealed record AccessTokenIssueResult(string AccessToken, DateTimeOffset ExpiresOnUtc);
