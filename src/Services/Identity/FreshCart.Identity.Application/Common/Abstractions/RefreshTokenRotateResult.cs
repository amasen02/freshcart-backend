namespace FreshCart.Identity.Application.Common.Abstractions;

/// <summary>
/// Outcome of rotating a refresh token: the owning user plus the replacement plaintext token.
/// </summary>
public sealed record RefreshTokenRotateResult(
    Guid UserId,
    string NewPlaintextToken,
    DateTimeOffset NewExpiresOnUtc);
