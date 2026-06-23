namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. The gateway re-signs a short-lived bearer token
/// using the same symmetric key, issuer and audience the Identity service uses, so downstream
/// services validate the token with the configuration block they already share.
/// </summary>
public sealed class GatewayJwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public TimeSpan DownstreamTokenLifetime { get; init; } = TimeSpan.FromMinutes(5);
}
