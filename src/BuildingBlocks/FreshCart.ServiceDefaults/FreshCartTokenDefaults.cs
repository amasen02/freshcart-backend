namespace FreshCart.ServiceDefaults;

/// <summary>
/// Shared JWT validation defaults. The clock skew is a cross-service contract: every downstream host
/// must accept a token the gateway minted under the same tolerance, so the value lives here once rather
/// than being copied into each host's <c>TokenValidationParameters</c>.
/// </summary>
public static class FreshCartTokenDefaults
{
    public static readonly TimeSpan JwtClockSkew = TimeSpan.FromSeconds(30);
}
