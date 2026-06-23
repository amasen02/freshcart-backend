namespace FreshCart.Identity.Infrastructure.Tokens;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. In production the signing key is loaded from
/// Azure Key Vault via the configuration provider chain; locally it ships in user-secrets.
/// </summary>
public sealed class JwtIssuerOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);
}
