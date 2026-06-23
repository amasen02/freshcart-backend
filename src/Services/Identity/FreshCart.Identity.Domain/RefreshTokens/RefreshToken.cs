namespace FreshCart.Identity.Domain.RefreshTokens;

/// <summary>
/// Persistent refresh-token record. Refresh tokens are used by service-to-service callers and by
/// the mobile client (which cannot rely on cookies); browser clients use the session cookie and
/// never receive a refresh token.
/// </summary>
/// <remarks>
/// Token value is stored as a SHA-256 hash. The plaintext token is returned to the caller exactly
/// once at issue time. Rotation policy: every successful refresh replaces the token; the previous
/// token is marked revoked and its <see cref="ReplacedByTokenHash"/> points at the new one. This
/// detects token theft (a thief and the legitimate user cannot both successfully refresh).
/// </remarks>
public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid UserId { get; init; }

    public required string TokenHash { get; init; }

    public required DateTimeOffset ExpiresOnUtc { get; init; }

    public DateTimeOffset CreatedOnUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedOnUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? RevocationReason { get; set; }

    public string? CreatedFromIpAddress { get; init; }

    public string? CreatedFromUserAgent { get; init; }

    public bool IsActive => RevokedOnUtc is null && DateTimeOffset.UtcNow < ExpiresOnUtc;

    public void Revoke(string reason, string? replacedByTokenHash, DateTimeOffset occurredOnUtc)
    {
        RevokedOnUtc = occurredOnUtc;
        RevocationReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
