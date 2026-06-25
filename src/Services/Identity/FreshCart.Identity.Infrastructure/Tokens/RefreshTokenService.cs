using System.Security.Cryptography;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.RefreshTokens;
using FreshCart.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FreshCart.Identity.Infrastructure.Tokens;

/// <summary>
/// Issues, rotates and revokes refresh tokens. Plaintext leaves this class only on the return
/// paths of <see cref="IssueAsync"/> and <see cref="RotateAsync"/>; the database always stores
/// a SHA-256 hash. Rotation detects reuse and revokes the entire family for that user, so a
/// stolen token surfaces as a forced re-authentication for the victim instead of an undetected
/// takeover.
/// </summary>
public sealed class RefreshTokenService(
    IdentityDbContext identityDbContext,
    IOptions<JwtIssuerOptions> issuerOptions) : IRefreshTokenService
{
    private const int PlaintextTokenLengthInBytes = 64;
    private const int MaximumIpAddressLength = 64;
    private const int MaximumUserAgentLength = 256;
    private const string RotationReason = "Rotated on refresh.";
    private const string ReuseDetectedReason = "Refresh token reuse detected.";

    private readonly JwtIssuerOptions options = issuerOptions.Value
        ?? throw new ArgumentException("JwtIssuerOptions missing from configuration.", nameof(issuerOptions));

    public async Task<RefreshTokenIssueResult> IssueAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var plaintext = GeneratePlaintextToken();
        var expiresOnUtc = DateTimeOffset.UtcNow.Add(options.RefreshTokenLifetime);

        identityDbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(plaintext),
            ExpiresOnUtc = expiresOnUtc,
            CreatedFromIpAddress = Truncate(ipAddress, MaximumIpAddressLength),
            CreatedFromUserAgent = Truncate(userAgent, MaximumUserAgentLength),
        });

        await identityDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new RefreshTokenIssueResult(plaintext, expiresOnUtc);
    }

    public async Task<RefreshTokenRotateResult> RotateAsync(
        string plaintextToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(plaintextToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var presentedHash = HashToken(plaintextToken);
        var existingToken = await identityDbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.TokenHash == presentedHash, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ForbiddenException("Refresh token is invalid.");

        if (!existingToken.IsActive)
        {
            // Replay of a revoked token. Either a thief or a misbehaving client. Either way,
            // revoke the whole family to force a clean re-authentication.
            await RevokeAllForUserAsync(existingToken.UserId, ReuseDetectedReason, cancellationToken).ConfigureAwait(false);
            throw new ForbiddenException("Refresh token is no longer valid.");
        }

        var newPlaintext = GeneratePlaintextToken();
        var newHash = HashToken(newPlaintext);
        var nowUtc = DateTimeOffset.UtcNow;
        var newExpiresOnUtc = nowUtc.Add(options.RefreshTokenLifetime);

        var rotationsClaimed = await ClaimRotationAsync(presentedHash, newHash, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        if (rotationsClaimed == 0)
        {
            await RevokeAllForUserAsync(existingToken.UserId, ReuseDetectedReason, cancellationToken).ConfigureAwait(false);
            throw new ForbiddenException("Refresh token is no longer valid.");
        }

        identityDbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newHash,
            ExpiresOnUtc = newExpiresOnUtc,
            CreatedFromIpAddress = Truncate(ipAddress, MaximumIpAddressLength),
            CreatedFromUserAgent = Truncate(userAgent, MaximumUserAgentLength),
        });

        await identityDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new RefreshTokenRotateResult(existingToken.UserId, newPlaintext, newExpiresOnUtc);
    }

    // Claims the rotation with a single atomic conditional update: it revokes the token only if it is still
    // active, so two requests racing on the same token cannot both rotate it (which would mint two live
    // tokens from one and defeat reuse detection). A zero-row result means another request already rotated
    // it — indistinguishable from a stolen-token replay. A single statement rather than a read-then-write
    // transaction because the DbContext runs a retrying execution strategy, which forbids user transactions.
    private Task<int> ClaimRotationAsync(
        string presentedHash,
        string newHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
        => identityDbContext.RefreshTokens
            .Where(record => record.TokenHash == presentedHash
                && record.RevokedOnUtc == null
                && record.ExpiresOnUtc > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.RevokedOnUtc, nowUtc)
                    .SetProperty(record => record.RevocationReason, RotationReason)
                    .SetProperty(record => record.ReplacedByTokenHash, newHash),
                cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var activeTokens = await identityDbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedOnUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (activeTokens.Count == 0)
        {
            return;
        }

        var revokedAtUtc = DateTimeOffset.UtcNow;
        foreach (var token in activeTokens)
        {
            token.Revoke(reason, replacedByTokenHash: null, occurredOnUtc: revokedAtUtc);
        }

        await identityDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string GeneratePlaintextToken()
    {
        var buffer = RandomNumberGenerator.GetBytes(PlaintextTokenLengthInBytes);
        return Convert.ToBase64String(buffer).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string HashToken(string plaintextToken)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintextToken));
        return Convert.ToHexString(hashBytes);
    }

    private static string? Truncate(string? source, int maximumLength) =>
        string.IsNullOrEmpty(source) || source.Length <= maximumLength
            ? source
            : source[..maximumLength];
}
