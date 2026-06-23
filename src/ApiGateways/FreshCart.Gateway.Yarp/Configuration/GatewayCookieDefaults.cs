namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Cookie, anti-forgery and data-protection identifiers shared verbatim with the Identity service.
/// They must match on both sides or the gateway cannot decrypt the session ticket nor validate an
/// Identity-issued anti-forgery token.
/// </summary>
public static class GatewayCookieDefaults
{
    public const string SessionCookieName = "FreshCart.Session";

    public const string AntiforgeryCookieName = "FreshCart.Antiforgery";

    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";

    public const string DataProtectionApplicationName = "FreshCart.Identity";

    public const string DataProtectionKeysRedisKey = "freshcart:dataprotection:keys";
}
