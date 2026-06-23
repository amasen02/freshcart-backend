using FreshCart.Identity.Domain.RefreshTokens;

namespace FreshCart.Identity.Application.Common.Abstractions;

/// <summary>
/// Manages the lifecycle of <see cref="RefreshToken"/> records: issue, validate, rotate, revoke.
/// Plaintext token values exist only in transit and are returned by <see cref="IssueAsync"/>;
/// the persistent store always holds a hash.
/// </summary>
public interface IRefreshTokenService
{
    Task<RefreshTokenIssueResult> IssueAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<RefreshTokenRotateResult> RotateAsync(
        string plaintextToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken);
}
