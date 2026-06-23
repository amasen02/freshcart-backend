namespace FreshCart.Identity.Application.Common.Abstractions;

/// <summary>
/// Newly issued refresh token; the plaintext value exists only in this envelope while in transit.
/// </summary>
public sealed record RefreshTokenIssueResult(string PlaintextToken, DateTimeOffset ExpiresOnUtc);
