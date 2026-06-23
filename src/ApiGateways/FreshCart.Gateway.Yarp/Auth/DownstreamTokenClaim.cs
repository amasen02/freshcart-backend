namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// Claim type names placed on the downstream bearer token. They mirror what the Identity service
/// writes onto the session cookie so a downstream service reads identical claims whether the original
/// caller used a cookie (browser, via the gateway) or a bearer token (programmatic client).
/// </summary>
public static class DownstreamTokenClaim
{
    public const string Subject = "sub";

    public const string Email = "email";

    public const string DisplayName = "display_name";

    public const string Role = "role";
}
