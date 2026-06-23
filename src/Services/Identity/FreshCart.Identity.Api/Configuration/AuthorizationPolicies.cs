namespace FreshCart.Identity.Api.Configuration;

/// <summary>
/// Policy names registered by <see cref="AuthenticationConfiguration"/>. Each policy requires an
/// authenticated user holding the role of the same name.
/// </summary>
public static class AuthorizationPolicies
{
    public const string Customer = "Customer";

    public const string SupportAgent = "SupportAgent";

    public const string Administrator = "Administrator";
}
